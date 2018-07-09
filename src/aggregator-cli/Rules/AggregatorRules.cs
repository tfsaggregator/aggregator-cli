using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Authentication;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;

namespace aggregator.cli
{
    class AggregatorRules
    {
        private readonly IAzure azure;
        private readonly ILogger logger;

        public AggregatorRules(IAzure azure, ILogger logger)
        {
            this.azure = azure;
            this.logger = logger;
        }


        internal async Task<IEnumerable<KuduFunction>> List(InstanceName instance)
        {
            var instances = new AggregatorInstances(azure, logger);
            var kudu = new KuduApi(instance, azure, logger);
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Get, $"api/functions"))
            using (var response = await client.SendAsync(request))
            {
                var stream = await response.Content.ReadAsStreamAsync();

                if (response.IsSuccessStatusCode)
                {
                    using (var sr = new StreamReader(stream))
                    using (var jtr = new JsonTextReader(sr))
                    {
                        var js = new JsonSerializer();
                        var functionList = js.Deserialize<KuduFunction[]>(jtr);
                        return functionList;
                    }
                }
                else
                    return new KuduFunction[0];
            }
        }

        internal static string GetInvocationUrl(InstanceName instance, string rule)
        {
            return $"{instance.FunctionAppUrl}/api/{rule}";
        }

        internal async Task<(string url, string key)> GetInvocationUrlAndKey(InstanceName instance, string rule)
        {
            var instances = new AggregatorInstances(azure, logger);
            var kudu = new KuduApi(instance, azure, logger);

            // see https://github.com/projectkudu/kudu/wiki/Functions-API
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Post, $"api/functions/{rule}/listsecrets"))
            {
                using (var response = await client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var sr = new StreamReader(stream))
                        using (var jtr = new JsonTextReader(sr))
                        {
                            var js = new JsonSerializer();
                            var secret = js.Deserialize<KuduSecret>(jtr);

                            (string url, string key) invocation = (GetInvocationUrl(instance, rule), secret.Key);
                            return invocation;
                        }
                    }
                    else
                        return default;
                }
            }
        }

        internal async Task<bool> AddAsync(InstanceName instance, string name, string filePath)
        {
            var kudu = new KuduApi(instance, azure, logger);

            logger.WriteVerbose($"Layout rule files");
            string baseDirPath = LayoutRuleFiles(name, filePath);
            logger.WriteInfo($"Packaging {filePath} into rule {name} complete.");

            logger.WriteVerbose($"Uploading rule files to {instance.PlainName}");
            bool ok = await UploadRuleFiles(instance, name, baseDirPath);
            if (ok)
            {
                logger.WriteInfo($"{name} files uploaded to {instance.PlainName}.");
            }
            CleanupRuleFiles(baseDirPath);
            return ok;
        }

        private static string LayoutRuleFiles(string name, string filePath)
        {
            // working directory
            var rand = new Random((int)DateTime.UtcNow.Ticks);
            string baseDirPath = Path.Combine(
                Path.GetTempPath(),
                $"aggregator-{rand.Next().ToString()}");
            string tempDirPath = Path.Combine(
                baseDirPath,
                name);
            Directory.CreateDirectory(tempDirPath);

            // copy rule content to fixed file name
            File.Copy(filePath, Path.Combine(tempDirPath, $"{name}.rule"));

            // copy templates
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.function.json"))
                // TODO this can be created by deserializing Kudu...
            using (var writer = File.Create(Path.Combine(tempDirPath, "function.json")))
            {
                reader.CopyTo(writer);
            }
            using (Stream reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.run.csx"))
            using (var writer = File.Create(Path.Combine(tempDirPath, "run.csx")))
            {
                reader.CopyTo(writer);
            }

            return baseDirPath;
        }

        void CleanupRuleFiles(string baseDirPath)
        {
            // clean-up: everything is in memory
            Directory.Delete(baseDirPath, true);
        }

        private async Task<bool> UploadRuleFiles(InstanceName instance, string name, string baseDirPath)
        {
            /*
            PUT /api/vfs/{path}
            Puts a file at path.

            PUT /api/vfs/{path}/
            Creates a directory at path. The path can be nested, e.g. `folder1/folder2`.
            */
            var kudu = new KuduApi(instance, azure, logger);
            string relativeUrl = $"api/vfs/site/wwwroot/{name}/";

            var instances = new AggregatorInstances(azure, logger);
            using (var client = new HttpClient())
            {
                using (var request = await kudu.GetRequestAsync(HttpMethod.Put, relativeUrl))
                {
                    using (var response = await client.SendAsync(request))
                    {
                        bool ok = response.IsSuccessStatusCode;
                        if (!ok)
                        {
                            logger.WriteError($"Upload failed with {response.ReasonPhrase}");
                            return ok;
                        }
                    }
                }
                var files = Directory.EnumerateFiles(baseDirPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    string fileUrl = $"{relativeUrl}{Path.GetFileName(file)}";
                    using (var request = await kudu.GetRequestAsync(HttpMethod.Put, fileUrl))
                    {
                        request.Content = new StringContent(File.ReadAllText(file));
                        using (var response = await client.SendAsync(request))
                        {
                            bool ok = response.IsSuccessStatusCode;
                            if (!ok)
                            {
                                logger.WriteError($"Failed uploading {file} with {response.ReasonPhrase}");
                                return ok;
                            }
                        }
                    }
                }//for
            }
            return true;
        }

        internal async Task<bool> RemoveAsync(InstanceName instance, string name)
        {
            var kudu = new KuduApi(instance, azure, logger);
            var instances = new AggregatorInstances(azure, logger);
            // undocumented but works, see https://github.com/projectkudu/kudu/wiki/Functions-API
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Delete, $"api/functions/{name}"))
            using (var response = await client.SendAsync(request))
            {
                return response.IsSuccessStatusCode;
            }
        }

#pragma warning disable IDE1006
        internal class Binding
        {
            public string type { get; set; }
            public string direction { get; set; }
            public string webHookType { get; set; }
            public string name { get; set; }
            public string queueName { get; set; }
            public string connection { get; set; }
            public string accessRights { get; set; }
            public string schedule { get; set; }
        }

        internal class FunctionSettings
        {
            public List<Binding> bindings { get; set; }
            public bool disabled { get; set; }
        }
#pragma warning restore IDE1006

        internal async Task<bool> EnableAsync(InstanceName instance, string name, bool disable)
        {
            var webFunctionApp = await azure
                .AppServices
                .WebApps
                .GetByResourceGroupAsync(
                    instance.ResourceGroupName,
                    instance.FunctionAppName);
            webFunctionApp
                .Update()
                .WithAppSetting($"AzureWebJobs.{name}.Disabled", disable.ToString().ToLower())
                .Apply();

            return true;
        }
    }
}

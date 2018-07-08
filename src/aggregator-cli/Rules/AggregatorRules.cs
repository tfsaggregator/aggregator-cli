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

#pragma warning disable IDE1006
        public class KuduFunctionKey
        {
            [JsonProperty(PropertyName = "name")]
            public string Name { get; set; }
            [JsonProperty(PropertyName = "value")]
            public string Value { get; set; }
        }

        public class KuduFunctionKeys
        {
            public KuduFunctionKey[] keys { get; set; }
            // links
        }

        public class KuduFunctionBinding
        {
            public string name { get; set; }
            public string type { get; set; }
            public string direction { get; set; }
            public string webHookType { get; set; }
        }

        public class KuduFunctionConfig
        {
            public KuduFunctionBinding[] bindings { get; set; }
            public bool disabled { get; set; }
        }

        public class KuduFunction
        {
            public string url { get; set; }

            public string name { get; set; }

            public KuduFunctionConfig config { get; set; }
        }

        public class KuduSecret
        {
            public string key { get; set; }
            public string trigger_url { get; set; }
        }
#pragma warning restore IDE1006

        internal async Task<IEnumerable<KuduFunction>> List(string instance)
        {
            var instances = new AggregatorInstances(azure, logger);
            using (var client = new HttpClient())
            using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Get, $"api/functions"))
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

        internal static string GetInvocationUrl(string instance, string rule)
        {
            return $"{AggregatorInstances.GetFunctionAppUrl(instance)}/api/{rule}";
        }

        internal async Task<(string url, string key)> GetInvocationUrlAndKey(string instance, string rule)
        {
            var instances = new AggregatorInstances(azure, logger);

            // see https://github.com/projectkudu/kudu/wiki/Functions-API
            using (var client = new HttpClient())
            using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Post, $"api/functions/{rule}/listsecrets"))
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

                            (string url, string key) invocation = (GetInvocationUrl(instance, rule), secret.key);
                            return invocation;
                        }
                    }
                    else
                        return default;
                }
            }
        }

        internal async Task<bool> AddAsync(string instance, string name, string filePath)
        {
            logger.WriteVerbose($"Layout rule files");
            string baseDirPath = LayoutRuleFiles(name, filePath);
            logger.WriteInfo($"Packaging {filePath} into rule {name} complete.");

            logger.WriteVerbose($"Requesting Publish credentials for {instance}");
            var instances = new AggregatorInstances(azure, logger);
            (string username, string password) = await instances.GetPublishCredentials(instance);
            logger.WriteInfo($"Retrieved publish credentials for {instance}.");

            logger.WriteVerbose($"Uploading rule files to {instance}");
            bool ok = await UploadRuleFiles(instance, name, baseDirPath);
            if (ok)
            {
                logger.WriteInfo($"{name} files uploaded to {instance}.");
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

        private async Task<bool> UploadRuleFiles(string instance, string name, string baseDirPath)
        {
            /*
            PUT /api/vfs/{path}
            Puts a file at path.

            PUT /api/vfs/{path}/
            Creates a directory at path. The path can be nested, e.g. `folder1/folder2`.
            */
            string relativeUrl = $"api/vfs/site/wwwroot/{name}/";

            var instances = new AggregatorInstances(azure, logger);
            using (var client = new HttpClient())
            {
                using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Put, relativeUrl))
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
                    using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Put, fileUrl))
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

        internal async Task<bool> RemoveAsync(string instance, string name)
        {
            var instances = new AggregatorInstances(azure, logger);
            // undocumented but works, see https://github.com/projectkudu/kudu/wiki/Functions-API
            using (var client = new HttpClient())
            using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Delete, $"api/functions/{name}"))
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

        internal async Task<bool> EnableAsync(string instance, string name, bool disable)
        {
            var instances = new AggregatorInstances(azure, logger);

            FunctionSettings settings;
            string settingsUrl = $"/api/vfs/site/wwwroot/{name}/function.json";

            using (var client = new HttpClient())
            using (var request1 = await instances.GetKuduRequestAsync(instance, HttpMethod.Get, settingsUrl))
            using (var response1 = await client.SendAsync(request1))
            {
                if (response1.IsSuccessStatusCode)
                {
                    string settingsString = await response1.Content.ReadAsStringAsync();
                    settings = JsonConvert.DeserializeObject<FunctionSettings>(settingsString);
                    settings.disabled = disable;

                    using (var request2 = await instances.GetKuduRequestAsync(instance, HttpMethod.Put, settingsUrl))
                    {
                        request2.Headers.Add("If-Match", "*"); // we are updating a file
                        var jset = new JsonSerializerSettings() {
                            Formatting = Formatting.Indented,
                            NullValueHandling = NullValueHandling.Ignore
                        };
                        settingsString = JsonConvert.SerializeObject(settings, jset);
                        request2.Content = new StringContent(settingsString);
                        using (var response2 = await client.SendAsync(request2))
                        {
                            return response2.IsSuccessStatusCode;
                        }
                    }
                }
                else
                    return false;
            }
        }

        internal async Task<bool> ConfigureAsync(string instance, string name)
        {
            throw new NotImplementedException(nameof(ConfigureAsync));
        }
    }
}

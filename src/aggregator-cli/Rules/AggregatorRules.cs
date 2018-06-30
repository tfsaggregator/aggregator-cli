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

        public AggregatorRules(IAzure azure)
        {
            this.azure = azure;
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
            var instances = new AggregatorInstances(azure);
            using (var client = new HttpClient())
            using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Get, $"/api/functions"))
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

        internal async Task<string> GetInvocationUrl(string instance, string rule)
        {
            var instances = new AggregatorInstances(azure);

            // see https://github.com/projectkudu/kudu/wiki/Functions-API
            using (var client = new HttpClient())
            using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Post, $"/api/functions/{rule}/listsecrets"))
            {
                using (var response = await client.SendAsync(request))
                {
                    var stream = await response.Content.ReadAsStreamAsync();
                    if (response.IsSuccessStatusCode)
                    {
                        using (var sr = new StreamReader(stream))
                        using (var jtr = new JsonTextReader(sr))
                        {
                            var js = new JsonSerializer();
                            var secret = js.Deserialize<KuduSecret>(jtr);
                            return secret.trigger_url;
                        }
                    }
                    else
                        return null;
                }
            }
        }

        internal async Task<bool> AddAsync(string instance, string name, string filePath)
        {
            byte[] zipContent = CreateTemporaryZipForRule(name, filePath);

            var instances = new AggregatorInstances(azure);
            (string username, string password) = await instances.GetPublishCredentials(instance);

            bool ok = await UploadZipWithRule(instance, zipContent, username, password);
            return ok;
        }

        private static byte[] CreateTemporaryZipForRule(string name, string filePath)
        {
            // see https://docs.microsoft.com/en-us/azure/azure-functions/deployment-zip-push

            // working directory
            var rand = new Random((int)DateTime.UtcNow.Ticks);
            string baseDirPath = Path.Combine(
                Path.GetTempPath(),
                $"aggregator-{rand.Next().ToString()}");
            string tempDirPath = Path.Combine(
                baseDirPath,
                name);
            Directory.CreateDirectory(tempDirPath);

            // copy rule content
            string deployedRulePath = Path.GetFileName(filePath) + ".csx";
            File.Copy(
                filePath,
                Path.Combine(
                    tempDirPath,
                    deployedRulePath));
            // copy templates
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.function.json"))
            using (var writer = File.Create(Path.Combine(tempDirPath, "function.json")))
            {
                reader.CopyTo(writer);
            }
            using (Stream reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.run.csx"))
            using (var writer = File.Create(Path.Combine(tempDirPath, "run.csx")))
            {
                var header = Encoding.UTF8.GetBytes($"#load \"{deployedRulePath}\"\r\n\r\n");
                writer.Write(header, 0, header.Length);
                reader.CopyTo(writer);
            }

            // zip
            string tempZipPath = Path.GetTempFileName();
            File.Delete(tempZipPath);
            ZipFile.CreateFromDirectory(baseDirPath, tempZipPath);
            var zipContent = File.ReadAllBytes(tempZipPath);

            // clean-up: everything is in memory
            Directory.Delete(tempDirPath, true);
            File.Delete(tempZipPath);
            return zipContent;
        }

        private async Task<bool> UploadZipWithRule(string instance, byte[] zipContent, string username, string password)
        {
            // POST /api/zipdeploy?isAsync=true
            // Deploy from zip asynchronously. The Location header of the response will contain a link to a pollable deployment status.
            var instances = new AggregatorInstances(azure);
            var body = new ByteArrayContent(zipContent);
            using (var client = new HttpClient())
            using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Post, $"/api/zipdeploy"))
            {
                request.Content = body;
                using (var response = await client.SendAsync(request))
                {
                    return response.IsSuccessStatusCode;
                }
            }
        }

        internal async Task<bool> RemoveAsync(string instance, string name)
        {
            var instances = new AggregatorInstances(azure);
            // undocumented but works, see https://github.com/projectkudu/kudu/wiki/Functions-API
            using (var client = new HttpClient())
            using (var request = await instances.GetKuduRequestAsync(instance, HttpMethod.Delete, $"/api/functions/{name}"))
            using (var response = await client.SendAsync(request))
            {
                return response.IsSuccessStatusCode;
            }
        }
    }
}

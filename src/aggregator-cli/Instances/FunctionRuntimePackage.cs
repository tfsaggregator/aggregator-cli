using Microsoft.Azure.Management.Fluent;
using Octokit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace aggregator.cli
{
    class FunctionRuntimePackage
    {
        private string fileVersion;
        private string infoVersion;

        internal FunctionRuntimePackage()
        {
            // check this assembly version
            var here = Assembly
                .GetExecutingAssembly();
            fileVersion = here
                .GetCustomAttribute<AssemblyFileVersionAttribute>()
                .Version;
            infoVersion = here
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
        }

        internal string RuntimePackageFile => "FunctionRuntime.zip";

        internal async Task<string> FindVersion(string tag = "latest")
        {
            var githubClient = new GitHubClient(new ProductHeaderValue("aggregator-cli", infoVersion));
            var releases = await githubClient.Repository.Release.GetAll("tfsaggregator", "aggregator-cli");
            // latest is default
            var release = releases[0];
            if (string.Compare(tag, "latest", true) != 0)
            {
                release = releases.Where(r => string.Compare(tag, r.TagName, true) == 0).FirstOrDefault();
            }
            if (release == null)
            {
                return "not found";
            }
            var asset = release.Assets.Where(a => a.Name == RuntimePackageFile).FirstOrDefault();
            return asset.BrowserDownloadUrl;
        }

        internal async Task<string> Download(string downloadUrl)
        {
            using (var httpClient = new WebClient())
            {
                await httpClient.DownloadFileTaskAsync(downloadUrl, RuntimePackageFile);
            }
            return RuntimePackageFile;
        }

        internal async Task<bool> UploadRuntimeZip(InstanceName instance, IAzure azure, ILogger logger)
        {
            var zipContent = File.ReadAllBytes(RuntimePackageFile);
            var kudu = new KuduApi(instance, azure, logger);
            // POST /api/zipdeploy?isAsync=true
            // Deploy from zip asynchronously. The Location header of the response will contain a link to a pollable deployment status.
            var body = new ByteArrayContent(zipContent);
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Post, $"api/zipdeploy"))
            {
                request.Content = body;
                using (var response = await client.SendAsync(request))
                {
                    bool ok = response.IsSuccessStatusCode;
                    if (!ok)
                    {
                        logger.WriteError($"Upload failed with {response.ReasonPhrase}");
                    }
                    return ok;
                }
            }
        }
    }
}

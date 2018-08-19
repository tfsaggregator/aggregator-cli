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
        private readonly string fileVersion;
        private readonly string infoVersion;
        private readonly ILogger logger;

        internal FunctionRuntimePackage(ILogger logger)
        {
            this.logger = logger;
            // check this assembly version
            var here = Assembly.GetExecutingAssembly();
            fileVersion = here
                .GetCustomAttribute<AssemblyFileVersionAttribute>()
                .Version;
            infoVersion = here
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
        }

        internal string RuntimePackageFile => "FunctionRuntime.zip";

        internal async Task<bool> UpdateVersion(InstanceName instance, IAzure azure)
        {
            logger.WriteVerbose($"Checking runtime package version");
            (string rel_name, DateTimeOffset? rel_when, string rel_url) = await FindVersion();
            logger.WriteVerbose($"Downloading runtime package {rel_name}");
            await Download(rel_url);
            logger.WriteInfo($"Runtime package downloaded.");

            logger.WriteVerbose($"Uploading runtime package to {instance.DnsHostName}");
            bool ok = await UploadRuntimeZip(instance, azure);
            return ok;
        }

        private async Task<(string name, DateTimeOffset? when, string url)> FindVersion(string tag = "latest")
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
                return default;
            }
            var asset = release.Assets.Where(a => a.Name == RuntimePackageFile).FirstOrDefault();
            return (name: release.Name, when: release.PublishedAt, url: asset.BrowserDownloadUrl);
        }

        private async Task<string> Download(string downloadUrl)
        {
            using (var httpClient = new WebClient())
            {
                await httpClient.DownloadFileTaskAsync(downloadUrl, RuntimePackageFile);
            }
            return RuntimePackageFile;
        }

        private async Task<bool> UploadRuntimeZip(InstanceName instance, IAzure azure)
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

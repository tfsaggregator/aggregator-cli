using Microsoft.Azure.Management.Fluent;
using Octokit;
using Semver;
using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace aggregator.cli
{
    class FunctionRuntimePackage
    {
        private readonly string infoVersion;
        private readonly ILogger logger;

        internal FunctionRuntimePackage(ILogger logger)
        {
            this.logger = logger;
            // check this assembly version
            var here = Assembly.GetExecutingAssembly();
            infoVersion = here
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
        }

        private string RuntimePackageFile => "FunctionRuntime.zip";

        internal async Task<bool> UpdateVersion(string requiredVersion, InstanceName instance, IAzure azure)
        {
            if (string.IsNullOrWhiteSpace(requiredVersion))
            {
                requiredVersion = "latest";
            }
            logger.WriteVerbose($"Checking runtime package versions in GitHub");
            (string rel_name, DateTimeOffset? rel_when, string rel_url) = await FindVersionInGitHub(requiredVersion);
            if (rel_name[0] == 'v') rel_name = rel_name.Substring(1);
            var requiredRuntimeVer = SemVersion.Parse(rel_name);
            logger.WriteInfo($"Latest Runtime package version is {requiredRuntimeVer} (released on {rel_when}).");

            string localPackageVersion = GetLocalPackageVersion(RuntimePackageFile);
            var localRuntimeVer = SemVersion.Parse(localPackageVersion);
            logger.WriteInfo($"Cached Runtime package version is {localRuntimeVer}.");
            if (ShouldUpdate(requiredRuntimeVer, localRuntimeVer))
            {
                logger.WriteVerbose($"Downloading runtime package {rel_name}");
                await Download(rel_url);
                logger.WriteInfo($"Runtime package downloaded.");
            }

            logger.WriteVerbose($"Uploading runtime package to {instance.DnsHostName}");
            bool ok = await UploadRuntimeZip(instance, azure);
            if (ok)
            {
                logger.WriteInfo($"Runtime package uploaded to {instance.PlainName}.");
            }
            else
            {
                logger.WriteError($"Failed uploading Runtime to {instance.DnsHostName}.");
            }
            return ok;
        }

        private string GetLocalPackageVersion(string runtimePackageFile)
        {
            string manifestVersion = "0.0"; // this default allows SemVer to parse and compare
            if (File.Exists(runtimePackageFile))
            {
                var zip = ZipFile.OpenRead(runtimePackageFile);
                var manifestEntry = zip.GetEntry("aggregator-manifest.ini");
                using (var byteStream = manifestEntry.Open())
                using (var reader = new StreamReader(byteStream))
                {
                    while (!reader.EndOfStream)
                    {
                        string line = reader.ReadLine();
                        var parts = line.Split('=');
                        if (parts[0] == "version")
                            manifestVersion = parts[1];
                    }
                }
            }
            return manifestVersion;
        }

        private bool ShouldUpdate(SemVersion lastRuntimeVer, SemVersion currentCliVer)
        {
             return lastRuntimeVer > currentCliVer;
        }

        private async Task<(string name, DateTimeOffset? when, string url)> FindVersionInGitHub(string tag = "latest")
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

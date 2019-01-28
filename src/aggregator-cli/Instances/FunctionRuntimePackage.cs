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
            string tag = string.IsNullOrWhiteSpace(requiredVersion)
                ? "latest"
                : (requiredVersion != "latest"
                    ? (requiredVersion[0] != 'v'
                        ? "v" + requiredVersion
                        : requiredVersion)
                    : requiredVersion);

            logger.WriteVerbose($"Checking runtime package versions in GitHub");
            (string rel_name, DateTimeOffset? rel_when, string rel_url) = await FindVersionInGitHub(tag);
            if (string.IsNullOrEmpty(rel_name))
            {
                logger.WriteError($"Requested runtime {requiredVersion} version does not exists.");
                return false;
            }
            if (rel_name[0] == 'v') rel_name = rel_name.Substring(1);
            var requiredRuntimeVer = SemVersion.Parse(rel_name);
            logger.WriteVerbose($"Latest Runtime package version is {requiredRuntimeVer} (released on {rel_when}).");

            var localRuntimeVer = await GetLocalPackageVersionAsync(RuntimePackageFile);
            logger.WriteVerbose($"Cached Runtime package version is {localRuntimeVer}.");

            // TODO check the uploaded version before overwriting?
            SemVersion uploadedRuntimeVer = await GetDeployedRuntimeVersion(instance, azure);

            if (requiredRuntimeVer > uploadedRuntimeVer || localRuntimeVer > uploadedRuntimeVer)
            {
                if (requiredRuntimeVer > localRuntimeVer)
                {
                    logger.WriteVerbose($"Downloading runtime package {rel_name}");
                    await Download(rel_url);
                    logger.WriteInfo($"Runtime package downloaded.");
                }
                else
                {
                    logger.WriteInfo($"Using cached runtime package {localRuntimeVer}");
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
            else
            {
                logger.WriteInfo($"Runtime package is up to date.");
                return true;
            }
        }

        internal async Task<SemVersion> GetDeployedRuntimeVersion(InstanceName instance, IAzure azure)
        {
            logger.WriteVerbose($"Retrieving functions runtime from {instance.PlainName} app");
            SemVersion uploadedRuntimeVer;
            var kudu = new KuduApi(instance, azure, logger);
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Get, $"api/vfs/site/wwwroot/aggregator-manifest.ini"))
            using (var response = await client.SendAsync(request))
            {
                string manifest = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    uploadedRuntimeVer = ManifestParser.Parse(manifest).Version;
                }
                else
                {
                    logger.WriteWarning($"Cannot read aggregator-manifest.ini: {response.ReasonPhrase}");
                    uploadedRuntimeVer = new SemVersion(0, 0, 0);
                }
            }

            logger.WriteVerbose($"Function Runtime version is {uploadedRuntimeVer}.");
            return uploadedRuntimeVer;
        }

        private async Task<SemVersion> GetLocalPackageVersionAsync(string runtimePackageFile)
        {
            if (File.Exists(runtimePackageFile))
            {
                var zip = ZipFile.OpenRead(runtimePackageFile);
                var manifestEntry = zip.GetEntry("aggregator-manifest.ini");
                using (var byteStream = manifestEntry.Open())
                using (var reader = new StreamReader(byteStream))
                {
                    var content = await reader.ReadToEndAsync();
                    var info = ManifestParser.Parse(content);
                    return info.Version;
                }
            }

            // this default allows SemVer to parse and compare
            return new SemVersion(0, 0);
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
            {
                client.Timeout = TimeSpan.FromMinutes(60);
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
}

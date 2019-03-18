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
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    class FunctionRuntimePackage
    {
        private readonly string cliVersion;
        private readonly ILogger logger;

        internal FunctionRuntimePackage(ILogger logger)
        {
            this.logger = logger;
            // check this assembly version
            var here = Assembly.GetExecutingAssembly();
            cliVersion = here
                .GetCustomAttribute<AssemblyFileVersionAttribute>()
                .Version;
        }

        private string RuntimePackageFile => "FunctionRuntime.zip";

        internal async Task<bool> UpdateVersionAsync(string requiredVersion, string sourceUrl, InstanceName instance, IAzure azure, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(sourceUrl))
            {
                return await UpdateVersionFromGitHubAsync(requiredVersion, instance, azure, cancellationToken);
            }
            else
            {
                return await UpdateVersionFromUrlAsync(sourceUrl, instance, azure, cancellationToken);
            }
        }

        internal async Task<bool> UpdateVersionFromUrlAsync(string sourceUrl, InstanceName instance, IAzure azure, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Refreshing local cache from {sourceUrl}.");
            await RefreshLocalPackageFromUrl(sourceUrl, cancellationToken);

            var localRuntimeVer = await GetLocalPackageVersionAsync(RuntimePackageFile);
            logger.WriteVerbose($"Locally cached Runtime package version is {localRuntimeVer}.");

            // TODO check the uploaded version before overwriting?
            SemVersion uploadedRuntimeVer = await GetDeployedRuntimeVersion(instance, azure, cancellationToken);

            if (localRuntimeVer > uploadedRuntimeVer)
            {
                logger.WriteInfo($"Using local cached runtime package {localRuntimeVer}");

                logger.WriteVerbose($"Uploading runtime package to {instance.DnsHostName}");
                bool ok = await UploadRuntimeZip(instance, azure, cancellationToken);
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

        private async Task RefreshLocalPackageFromUrl(string sourceUrl, CancellationToken cancellationToken)
        {
            DateTimeOffset? cachedLastWrite = GetLastWriteAtCachedRuntime();

            using (var client = new HttpClient())
            // HACK assume that source URL does not require authentication!
            // note: HEAD verb does not work with GitHub, so we use a GET with a 0-bytes range
            using (var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl))
            {
                if (cachedLastWrite.HasValue)
                {
                    request.Headers.IfModifiedSince = cachedLastWrite;
                }
                using (var response = await client.SendAsync(request, cancellationToken))
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                            logger.WriteVerbose($"Downloading runtime package from {sourceUrl}");
                            using (var fileStream = File.Create(RuntimePackageFile))
                            {
                                //TODO cancellationToken
                                await response.Content.CopyToAsync(fileStream);
                            }
                            logger.WriteInfo($"Runtime package downloaded.");
                            break;

                        case HttpStatusCode.NotModified:
                            logger.WriteInfo($"Runtime package at {sourceUrl} matches local cache.");
                            break;

                        default:
                            logger.WriteError($"{sourceUrl} returned {response.ReasonPhrase}.");
                            break;
                    }//switch
                }
            }
        }

        private DateTimeOffset? GetLastWriteAtCachedRuntime()
        {
            DateTimeOffset? cachedLastWrite = null;

            if (File.Exists(RuntimePackageFile))
            {
                cachedLastWrite = File.GetLastWriteTimeUtc(RuntimePackageFile);
            }

            return cachedLastWrite;
        }

        internal async Task<bool> UpdateVersionFromGitHubAsync(string requiredVersion, InstanceName instance, IAzure azure, CancellationToken cancellationToken)
        {
            string tag = string.IsNullOrWhiteSpace(requiredVersion)
                ? "latest"
                : (requiredVersion != "latest"
                    ? (requiredVersion[0] != 'v'
                        ? "v" + requiredVersion
                        : requiredVersion)
                    : requiredVersion);

            logger.WriteVerbose($"Checking runtime package versions in GitHub");
            (string rel_name, DateTimeOffset? rel_when, string rel_url) = await FindVersionInGitHubAsync(tag);
            if (string.IsNullOrEmpty(rel_name))
            {
                logger.WriteError($"Requested runtime {requiredVersion} version does not exists.");
                return false;
            }
            logger.WriteVerbose($"Found {rel_name} on {rel_when} in GitHub .");
            if (rel_name[0] == 'v') rel_name = rel_name.Substring(1);
            var requiredRuntimeVer = SemVersion.Parse(rel_name);
            logger.WriteVerbose($"Latest Runtime package version is {requiredRuntimeVer} (released on {rel_when}).");

            var localRuntimeVer = await GetLocalPackageVersionAsync(RuntimePackageFile);
            logger.WriteVerbose($"Locally cached Runtime package version is {localRuntimeVer}.");

            // TODO check the uploaded version before overwriting?
            SemVersion uploadedRuntimeVer = await GetDeployedRuntimeVersion(instance, azure, cancellationToken);

            if (requiredRuntimeVer > uploadedRuntimeVer || localRuntimeVer > uploadedRuntimeVer)
            {
                if (requiredRuntimeVer > localRuntimeVer)
                {
                    logger.WriteVerbose($"Downloading runtime package {rel_name}");
                    await DownloadAsync(rel_url, cancellationToken);
                    logger.WriteInfo($"Runtime package downloaded.");
                }
                else
                {
                    logger.WriteInfo($"Using local cached runtime package {localRuntimeVer}");
                }

                logger.WriteVerbose($"Uploading runtime package to {instance.DnsHostName}");
                bool ok = await UploadRuntimeZip(instance, azure, cancellationToken);
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

        internal async Task<SemVersion> GetDeployedRuntimeVersion(InstanceName instance, IAzure azure, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Retrieving functions runtime from {instance.PlainName} app");
            SemVersion uploadedRuntimeVer;
            var kudu = new KuduApi(instance, azure, logger);
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Get, $"api/vfs/site/wwwroot/aggregator-manifest.ini", cancellationToken))
            using (var response = await client.SendAsync(request, cancellationToken))
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

        private async Task<(string name, DateTimeOffset? when, string url)> FindVersionInGitHubAsync(string tag = "latest")
        {
            var githubClient = new GitHubClient(new ProductHeaderValue("aggregator-cli", cliVersion));
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

        private async Task<string> DownloadAsync(string downloadUrl, CancellationToken cancellationToken)
        {
            using (var httpClient = new WebClient())
            using (cancellationToken.Register(httpClient.CancelAsync))
            {
                await httpClient.DownloadFileTaskAsync(downloadUrl, RuntimePackageFile);
            }

            return RuntimePackageFile;
        }

        private async Task<bool> UploadRuntimeZip(InstanceName instance, IAzure azure, CancellationToken cancellationToken)
        {
            var zipContent = await File.ReadAllBytesAsync(RuntimePackageFile, cancellationToken);
            var kudu = new KuduApi(instance, azure, logger);
            // POST /api/zipdeploy?isAsync=true
            // Deploy from zip asynchronously. The Location header of the response will contain a link to a pollable deployment status.
            var body = new ByteArrayContent(zipContent);
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(60);
                using (var request = await kudu.GetRequestAsync(HttpMethod.Post, $"api/zipdeploy", cancellationToken))
                {
                    request.Content = body;
                    using (var response = await client.SendAsync(request, cancellationToken))
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

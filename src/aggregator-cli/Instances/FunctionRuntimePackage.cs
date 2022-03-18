using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Octokit;
using Semver;

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
            RuntimePackageFile = "FunctionRuntime.zip";
        }

        public string RuntimePackageFile { get; private set; }

        internal async Task<(bool upgrade, string newversion)> IsCliUpgradable()
        {
            string tag = "latest";
            var gitHubVersion = GitHubVersionResponse.TryReadFromCache();
            if (gitHubVersion != null)
                logger.WriteVerbose("located a cached GitHub version query response");
            if (gitHubVersion == null || !gitHubVersion.CacheIsInDate() || tag != gitHubVersion.Tag)
            {
                logger.WriteVerbose($"Checking runtime package versions in GitHub");
                gitHubVersion = await FindVersionInGitHubAsync(tag);
                if (gitHubVersion != null && gitHubVersion.SaveCache())
                    logger.WriteVerbose($"Saved GitHub response to disk");
            }
            else
            {
                logger.WriteVerbose($"Cached versions are recent enough to not require checking GitHub");
            }

            if (gitHubVersion == null || string.IsNullOrEmpty(gitHubVersion.Name))
            {
                logger.WriteError($"Requested CLI version does not exist in GitHub.");
                return (upgrade: false, newversion: "");
            }
            logger.WriteVerbose($"Found {gitHubVersion.Name} on {gitHubVersion.When} in GitHub.");

            if (!SemVersion.TryParse(gitHubVersion.Name[1..], out var latest))
            {
                logger.WriteError($"Failed to parse GitHub version.");
                return new(false, "unknown");
            }
            var asmVer = Assembly.GetEntryAssembly().GetName().Version;
            var current = new SemVersion(asmVer.Major, asmVer.Minor, asmVer.Build);

            bool upgrade = latest.CompareTo(current) > 0;
            return upgrade
                ? (upgrade, newversion: gitHubVersion.Name)
                : (upgrade: false, newversion: "");
        }

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
            var sourceUri = new Uri(sourceUrl);
            if (sourceUri.Scheme == Uri.UriSchemeHttp
                || sourceUri.Scheme == Uri.UriSchemeHttps)
            {
                await RefreshLocalPackageFromHttpUrl(sourceUrl, cancellationToken);
            }
            else if (sourceUri.Scheme == Uri.UriSchemeFile)
            {
                RefreshLocalPackageFromFileUrl(sourceUri);
            }
            else
            {
                logger.WriteError($"Unsupported URI scheme {sourceUri.Scheme}.");
            }

        }

        private void RefreshLocalPackageFromFileUrl(Uri sourceUri)
        {
            RuntimePackageFile = sourceUri.AbsolutePath;
            logger.WriteInfo($"Runtime package source will be {RuntimePackageFile}");
        }

        private async Task RefreshLocalPackageFromHttpUrl(string sourceUrl, CancellationToken cancellationToken)
        {
            DateTimeOffset? cachedLastWrite = GetLastWriteAtCachedRuntime();

            using var client = new HttpClient();
            // HACK assume that source URL does not require authentication!
            // note: HEAD verb does not work with GitHub, so we use a GET with a 0-bytes range
            using var request = new HttpRequestMessage(HttpMethod.Get, sourceUrl);
            if (cachedLastWrite.HasValue)
            {
                request.Headers.IfModifiedSince = cachedLastWrite;
            }
            using var response = await client.SendAsync(request, cancellationToken);
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                    logger.WriteVerbose($"Downloading runtime package from {sourceUrl}");
                    using (var fileStream = File.Create(RuntimePackageFile))
                    {
                        //TODO cancellationToken
                        await response.Content.CopyToAsync(fileStream, cancellationToken);
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
            (SemVersion requiredRuntimeVer, GitHubVersionResponse gitHubVersion)  = await GetGitHubReleaseMatchingUserVersionAsync(GitHubTagFromUserVersion(requiredVersion), requiredVersion);

            var localRuntimeVer = await GetLocalPackageVersionAsync(RuntimePackageFile);
            logger.WriteVerbose($"Locally cached Runtime package version is {localRuntimeVer}.");

            // TODO check the uploaded version before overwriting?
            SemVersion uploadedRuntimeVer = await GetDeployedRuntimeVersion(instance, azure, cancellationToken);

            if (requiredRuntimeVer > uploadedRuntimeVer || localRuntimeVer > uploadedRuntimeVer)
            {
                if (requiredRuntimeVer > localRuntimeVer)
                {
                    logger.WriteVerbose($"Downloading runtime package {gitHubVersion.Name}");
                    await DownloadAsync(gitHubVersion.Url, cancellationToken);
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

        static string GitHubTagFromUserVersion(string requiredVersion)
        {
            if (string.IsNullOrWhiteSpace(requiredVersion)
                || requiredVersion == "latest")
                return "latest";
            else 
                return requiredVersion[0] != 'v'
                        ? "v" + requiredVersion
                        : requiredVersion;
        }

        async Task<(SemVersion requiredRuntimeVer, GitHubVersionResponse gitHubVersion)> GetGitHubReleaseMatchingUserVersionAsync(string tag, string requiredVersion)
        {
            var gitHubVersion = GitHubVersionResponse.TryReadFromCache();
            if (gitHubVersion != null)
                logger.WriteVerbose("located a cached GitHub version query response");
            if (gitHubVersion == null || !gitHubVersion.CacheIsInDate() || tag != gitHubVersion.Tag)
            {
                logger.WriteVerbose($"Checking runtime package versions in GitHub");
                gitHubVersion = await FindVersionInGitHubAsync(tag);
                if (gitHubVersion != null && gitHubVersion.SaveCache())
                    logger.WriteVerbose($"Saved GitHub response to disk");
            }
            else
            {
                logger.WriteVerbose($"Cached versions are recent enough to not require checking GitHub");
            }

            if (gitHubVersion == null || string.IsNullOrEmpty(gitHubVersion.Name))
            {
                logger.WriteError($"Requested runtime {requiredVersion} version does not exist in GitHub.");
                return (null, null);
            }
            logger.WriteVerbose($"Found {gitHubVersion.Name} on {gitHubVersion.When} in GitHub .");

            if (gitHubVersion.Name[0] == 'v') gitHubVersion.Name = gitHubVersion.Name[1..];
            var requiredRuntimeVer = SemVersion.Parse(gitHubVersion.Name);
            logger.WriteVerbose($"Latest Runtime package version is {requiredRuntimeVer} (released on {gitHubVersion.When}).");

            return (requiredRuntimeVer, gitHubVersion);
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
                string manifest = await response.Content.ReadAsStringAsync(cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    uploadedRuntimeVer = ManifestParser.Parse(manifest).Version;
                }
                else
                {
                    logger.WriteWarning($"Cannot read aggregator-manifest.ini: {response.ReasonPhrase} (disregard this message on new instances)");
                    uploadedRuntimeVer = new SemVersion(0, 0, 0);
                }
            }

            logger.WriteVerbose($"Function Runtime version is {uploadedRuntimeVer}.");
            return uploadedRuntimeVer;
        }

        internal static async Task<Stream> GetDeployedFunctionEntrypoint(InstanceName instance, IAzure azure, ILogger logger, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Retrieving deployed aggregator-function.dll");
            var kudu = new KuduApi(instance, azure, logger);
            using var client = new HttpClient();
            using var request = await kudu.GetRequestAsync(HttpMethod.Get, $"api/vfs/site/wwwroot/bin/aggregator-function.dll", cancellationToken);
            var response = await client.SendAsync(request, cancellationToken);
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                return stream;
            }

            logger.WriteError($"Cannot read aggregator-function.dll: {response.ReasonPhrase}");
            return null;
        }

        private static async Task<SemVersion> GetLocalPackageVersionAsync(string runtimePackageFile)
        {
            if (!File.Exists(runtimePackageFile))
            {
                // this default allows SemVer to parse and compare
                return new(0, 0);
            }

            using var zip = ZipFile.OpenRead(runtimePackageFile);
            var manifestEntry = zip.GetEntry("aggregator-manifest.ini");
#pragma warning disable S5042 // Make sure that decompressing this archive file is safe
            using var byteStream = manifestEntry.Open();
#pragma warning restore S5042 // Make sure that decompressing this archive file is safe
            using var reader = new StreamReader(byteStream);
            var content = await reader.ReadToEndAsync();
            var info = ManifestParser.Parse(content);
            return info.Version;
        }

        private async Task<GitHubVersionResponse> FindVersionInGitHubAsync(string tag)
        {
            var githubClient = new GitHubClient(new ProductHeaderValue("aggregator-cli", cliVersion));
            var releases = await githubClient.Repository.Release.GetAll("tfsaggregator", "aggregator-cli");
            // latest is default
            var release = releases[0];
            if (string.Compare(tag, "latest", true) != 0)
            {
                release = releases.FirstOrDefault(r => string.Compare(tag, r.TagName, true) == 0);
            }
            if (release == null)
            {
                return default;
            }
            var asset = release.Assets.FirstOrDefault(a => a.Name == RuntimePackageFile);
            return new GitHubVersionResponse()
            {
                Tag = tag,
                Name = release.Name,
                When = release.PublishedAt,
                Url = asset.BrowserDownloadUrl,
                ResponseDate = DateTime.Now
            };
        }

        private async Task<string> DownloadAsync(string downloadUrl, CancellationToken cancellationToken)
        {
            using (var httpClient = new HttpClient())
            using (var s = await httpClient.GetStreamAsync(downloadUrl, cancellationToken))
            using (var fs = new FileStream(RuntimePackageFile, System.IO.FileMode.CreateNew))
            await s.CopyToAsync(fs, cancellationToken);

            return RuntimePackageFile;
        }

        private async Task<bool> UploadRuntimeZip(InstanceName instance, IAzure azure, CancellationToken cancellationToken)
        {
            var zipContent = await File.ReadAllBytesAsync(RuntimePackageFile, cancellationToken);
            var kudu = new KuduApi(instance, azure, logger);
            // POST /api/zipdeploy?isAsync=true
            // Deploy from zip asynchronously. The Location header of the response will contain a link to a pollable deployment status.
            var body = new ByteArrayContent(zipContent);
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromMinutes(60);
            using var request = await kudu.GetRequestAsync(HttpMethod.Post, $"api/zipdeploy", cancellationToken);
            request.Content = body;
            using var response = await client.SendAsync(request, cancellationToken);
            bool ok = response.IsSuccessStatusCode;
            if (!ok)
            {
                logger.WriteError($"Upload failed with {response.ReasonPhrase}");
            }
            return ok;
        }
    }
}

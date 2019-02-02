using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace aggregator.cli
{
    internal class AggregatorRules
    {
        private static readonly Random Randomizer = new Random((int)DateTime.UtcNow.Ticks);
        private readonly IAzure _azure;
        private readonly ILogger _logger;

        public AggregatorRules(IAzure azure, ILogger logger)
        {
            _azure = azure;
            _logger = logger;
        }

        internal async Task<IEnumerable<KuduFunction>> ListAsync(InstanceName instance)
        {
            var instances = new AggregatorInstances(_azure, _logger);
            var kudu = new KuduApi(instance, _azure, _logger);
            _logger.WriteInfo($"Retrieving Functions in {instance.PlainName}...");
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

                _logger.WriteError($"{response.ReasonPhrase} {await response.Content.ReadAsStringAsync()}");
                return new KuduFunction[0];
            }
        }

        internal static string GetInvocationUrl(InstanceName instance, string rule)
        {
            return $"{instance.FunctionAppUrl}/api/{rule}";
        }

        internal async Task<(string url, string key)> GetInvocationUrlAndKey(InstanceName instance, string rule)
        {
            var instances = new AggregatorInstances(_azure, _logger);
            var kudu = new KuduApi(instance, _azure, _logger);

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

                    string error = await response.Content.ReadAsStringAsync();
                    _logger.WriteError($"Failed to retrieve function key: {error}");
                    throw new InvalidOperationException("Failed to retrieve function key.");
                }
            }
        }

        internal async Task<bool> AddAsync(InstanceName instance, string name, string filePath)
        {
            var kudu = new KuduApi(instance, _azure, _logger);

            _logger.WriteInfo($"Validate rule file {filePath}");

            var ruleContent = await File.ReadAllLinesAsync(filePath);

            var engineLogger = new EngineWrapperLogger(_logger);
            try
            {
                var ruleEngine = new Engine.RuleEngine(engineLogger, ruleContent, SaveMode.Batch, true);
                (var success, var diagnostics) = ruleEngine.VerifyRule();
                if (success)
                {
                    _logger.WriteInfo($"Rule file is valid");
                }
                else
                {
                    _logger.WriteInfo($"Rule file is invalid");
                    var messages = string.Join('\n', diagnostics.Select(d => d.ToString()));
                    if (!string.IsNullOrEmpty(messages))
                    {
                        _logger.WriteError($"Errors in the rule file {filePath}:\n{messages}");
                    }

                    return false;
                }
            }
            catch
            {
                _logger.WriteInfo($"Rule file is invalid");
                return false;
            }

            _logger.WriteVerbose($"Layout rule files");
            string baseDirPath = await LayoutRuleFilesAsync(name, filePath);
            _logger.WriteInfo($"Packaging {filePath} into rule {name} complete.");

            _logger.WriteVerbose($"Uploading rule files to {instance.PlainName}");
            bool ok = await UploadRuleFiles(instance, name, baseDirPath);
            if (ok)
            {
                _logger.WriteInfo($"All {name} files uploaded to {instance.PlainName}.");
            }

            CleanupRuleFiles(baseDirPath);
            _logger.WriteInfo($"Cleaned local working directory.");
            return ok;
        }

        private static async Task<string> LayoutRuleFilesAsync(string name, string filePath)
        {
            // working directory
            string baseDirPath = Path.Combine(
                Path.GetTempPath(),
                FormattableString.Invariant($"aggregator-{Randomizer.Next()}"));
            string tempDirPath = Path.Combine(
                baseDirPath,
                name);
            Directory.CreateDirectory(tempDirPath);

            // copy rule content to fixed file name
            File.Copy(filePath, Path.Combine(tempDirPath, FormattableString.Invariant($"{name}.rule")));

            // copy templates
            var assembly = Assembly.GetExecutingAssembly();
            using (var reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.function.json"))
            // TODO we can deserialize a KuduFunctionConfig instead of using a fixed file...
            using (var writer = new FileStream(Path.Combine(tempDirPath, "function.json"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                await reader.CopyToAsync(writer);
            }

            using (Stream reader = assembly.GetManifestResourceStream("aggregator.cli.Rules.run.csx"))
            using (var writer = new FileStream(Path.Combine(tempDirPath, "run.csx"), FileMode.OpenOrCreate, FileAccess.Write, FileShare.None))
            {
                await reader.CopyToAsync(writer);
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

            Note: when updating or deleting a file, ETag behavior will apply. You can pass a If-Match: "*" header to disable the ETag check.
            */
            var kudu = new KuduApi(instance, _azure, _logger);
            string relativeUrl = $"api/vfs/site/wwwroot/{name}/";

            var instances = new AggregatorInstances(_azure, _logger);
            using (var client = new HttpClient())
            {
                bool exists = false;

                // check if function already exists
                using (var request = await kudu.GetRequestAsync(HttpMethod.Head, relativeUrl))
                {
                    _logger.WriteVerbose($"Checking if function {name} already exists in {instance.PlainName}...");
                    using (var response = await client.SendAsync(request))
                    {
                        exists = response.IsSuccessStatusCode;
                    }
                }

                if (!exists)
                {
                    _logger.WriteVerbose($"Creating function {name} in {instance.PlainName}...");
                    using (var request = await kudu.GetRequestAsync(HttpMethod.Put, relativeUrl))
                    {
                        using (var response = await client.SendAsync(request))
                        {
                            bool ok = response.IsSuccessStatusCode;
                            if (!ok)
                            {
                                _logger.WriteError($"Upload failed with {response.ReasonPhrase}");
                                return ok;
                            }
                        }
                    }

                    _logger.WriteInfo($"Function {name} created.");
                }

                var files = Directory.EnumerateFiles(baseDirPath, "*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    _logger.WriteVerbose($"Uploading {Path.GetFileName(file)} to {instance.PlainName}...");
                    string fileUrl = $"{relativeUrl}{Path.GetFileName(file)}";
                    using (var request = await kudu.GetRequestAsync(HttpMethod.Put, fileUrl))
                    {
                        //HACK -> request.Headers.IfMatch.Add(new EntityTagHeaderValue("*", false)); <- won't work
                        request.Headers.Add("If-Match", "*");
                        request.Content = new StringContent(File.ReadAllText(file));
                        using (var response = await client.SendAsync(request))
                        {
                            bool ok = response.IsSuccessStatusCode;
                            if (!ok)
                            {
                                _logger.WriteError($"Failed uploading {file} with {response.ReasonPhrase}");
                                return ok;
                            }
                        }
                    }

                    _logger.WriteInfo($"{Path.GetFileName(file)} uploaded to {instance.PlainName}.");
                }//for
            }

            return true;
        }

        internal async Task<bool> RemoveAsync(InstanceName instance, string name)
        {
            var kudu = new KuduApi(instance, _azure, _logger);
            var instances = new AggregatorInstances(_azure, _logger);
            // undocumented but works, see https://github.com/projectkudu/kudu/wiki/Functions-API
            _logger.WriteInfo($"Removing Function {name} in {instance.PlainName}...");
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Delete, $"api/functions/{name}"))
            using (var response = await client.SendAsync(request))
            {
                bool ok = response.IsSuccessStatusCode;
                if (!ok)
                {
                    _logger.WriteError($"Failed removing Function {name} from {instance.PlainName} with {response.ReasonPhrase}");
                }

                return ok;
            }
        }

        internal async Task<bool> EnableAsync(InstanceName instance, string name, bool disable)
        {
            var webFunctionApp = await _azure
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

        internal async Task<bool> UpdateAsync(InstanceName instance, string name, string filePath, string requiredVersion)
        {
            // check runtime package
            var package = new FunctionRuntimePackage(_logger);
            bool ok = await package.UpdateVersion(requiredVersion, instance, _azure);
            if (ok)
            {
                ok = await AddAsync(instance, name, filePath);
            }

            return ok;
        }

        internal async Task<bool> InvokeLocalAsync(string projectName, string @event, int workItemId, string ruleFilePath, bool dryRun, SaveMode saveMode)
        {
            if (!File.Exists(ruleFilePath))
            {
                _logger.WriteError($"Rule code not found at {ruleFilePath}");
                return false;
            }

            var devopsLogonData = DevOpsLogon.Load().connection;

            _logger.WriteVerbose($"Connecting to Azure DevOps using {devopsLogonData.Mode}...");
            var clientCredentials = default(VssCredentials);
            if (devopsLogonData.Mode == DevOpsTokenType.PAT)
            {
                clientCredentials = new VssBasicCredential(devopsLogonData.Mode.ToString(), devopsLogonData.Token);
            }
            else
            {
                _logger.WriteError($"Azure DevOps Token type {devopsLogonData.Mode} not supported!");
                throw new ArgumentOutOfRangeException(nameof(devopsLogonData.Mode));
            }

            string collectionUrl = devopsLogonData.Url;
            using (var devops = new VssConnection(new Uri(collectionUrl), clientCredentials))
            {
                await devops.ConnectAsync();
                _logger.WriteInfo($"Connected to Azure DevOps");

                Guid teamProjectId;
                string teamProjectName;
                using (var projectClient = devops.GetClient<ProjectHttpClient>())
                {
                    _logger.WriteVerbose($"Reading Azure DevOps project data...");
                    var project = await projectClient.GetProject(projectName);
                    _logger.WriteInfo($"Project {projectName} data read.");
                    teamProjectId = project.Id;
                    teamProjectName = project.Name;
                }

                using (var witClient = devops.GetClient<WorkItemTrackingHttpClient>())
                {
                    _logger.WriteVerbose($"Rule code found at {ruleFilePath}");
                    string[] ruleCode = File.ReadAllLines(ruleFilePath);

                    var engineLogger = new EngineWrapperLogger(_logger);
                    var engine = new Engine.RuleEngine(engineLogger, ruleCode, saveMode, dryRun: dryRun);

                    string result = await engine.ExecuteAsync(collectionUrl, teamProjectId, teamProjectName, devopsLogonData.Token, workItemId, witClient);
                    _logger.WriteInfo($"Rule returned '{result}'");

                    return true;
                }
            }
        }

        internal async Task<bool> InvokeRemoteAsync(string account, string project, string @event, int workItemId, InstanceName instance, string ruleName, bool dryRun, SaveMode saveMode)
        {
            var kudu = new KuduApi(instance, _azure, _logger);

            // build the request ...
            _logger.WriteVerbose($"Retrieving {ruleName} Function Key...");
            (string ruleUrl, string ruleKey) = await this.GetInvocationUrlAndKey(instance, ruleName);
            _logger.WriteInfo($"{ruleName} Function Key retrieved.");

            ruleUrl = InvokeOptions.AppendToUrl(ruleUrl, dryRun, saveMode);

            string baseUrl = $"https://dev.azure.com/{account}";
            Guid teamProjectId = Guid.Empty;
            string body = FormattableString.Invariant($@"{{
  ""eventType"": ""{@event}"",
  ""publisherId"": ""tfs"",
  ""resource"": {{
    ""url"": ""{baseUrl}/{project}/_apis/wit/workItems/{workItemId}"",
    ""id"": {workItemId},
    ""workItemId"": {workItemId},
    ""fields"": {{
      ""System.TeamProject"": ""{project}""
    }},
    ""revision"": {{
      ""fields"": {{
        ""System.TeamProject"": ""{project}""
      }}
    }}
  }},
  ""resourceContainers"": {{
    ""collection"": {{
      ""baseUrl"": ""{baseUrl}""
    }},
    ""project"": {{
      ""id"": ""{teamProjectId}""
    }}
  }}
}}");
            _logger.WriteVerbose($"Request to {ruleName} is:");
            _logger.WriteVerbose(body);

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage(HttpMethod.Post, ruleUrl))
                {
                    request.Headers.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
                    request.Headers.Add("x-functions-key", ruleKey);
                    request.Content = new StringContent(body, Encoding.UTF8, "application/json");

                    using (var response = await client.SendAsync(request))
                    {
                        if (response.IsSuccessStatusCode)
                        {
                            string result = await response.Content.ReadAsStringAsync();
                            _logger.WriteInfo($"{result}");
                            return true;
                        }

                        _logger.WriteError($"Failed with {response.ReasonPhrase}");
                        return false;
                    }
                }
            }
        }
    }
}

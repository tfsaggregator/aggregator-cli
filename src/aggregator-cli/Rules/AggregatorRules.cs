using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using aggregator.Engine.Language;

using Microsoft.Azure.Management.Fluent;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;

namespace aggregator.cli
{
    internal class AggregatorRules : AzureBaseClass
    {
        public AggregatorRules(IAzure azure, ILogger logger) : base(azure, logger)
        { }

        internal async Task<IEnumerable<RuleOutputData>> ListAsync(InstanceName instance, CancellationToken cancellationToken)
        {
            var kudu = GetKudu(instance);
            _logger.WriteInfo($"Retrieving Functions in {instance.PlainName}...");

            var webFunctionApp = await GetWebApp(instance, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            var configuration = await AggregatorConfiguration.ReadConfiguration(webFunctionApp);

            using (var client = new HttpClient())
            {
                KuduFunction[] kuduFunctions;
                using (var request = await kudu.GetRequestAsync(HttpMethod.Get, $"api/functions", cancellationToken))
                using (var response = await client.SendAsync(request, cancellationToken))
                {
                    var stream = await response.Content.ReadAsStreamAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.WriteError($"{response.ReasonPhrase} {await response.Content.ReadAsStringAsync()}");
                        return Enumerable.Empty<RuleOutputData>();
                    }

                    using (var sr = new StreamReader(stream))
                    using (var jtr = new JsonTextReader(sr))
                    {
                        var js = new JsonSerializer();
                        kuduFunctions = js.Deserialize<KuduFunction[]>(jtr);
                    }
                }

                List<RuleOutputData> ruleData = new List<RuleOutputData>();
                foreach (var kuduFunction in kuduFunctions)
                {
                    var ruleName = kuduFunction.Name;
                    var ruleFileUrl = $"api/vfs/site/wwwroot/{ruleName}/{ruleName}.rule";
                    _logger.WriteInfo($"Retrieving Function Rule Details {ruleName}...");

                    using (var request = await kudu.GetRequestAsync(HttpMethod.Get, ruleFileUrl, cancellationToken))
                    using (var response = await client.SendAsync(request, cancellationToken))
                    {
                        var stream = await response.Content.ReadAsStreamAsync();

                        if (!response.IsSuccessStatusCode)
                        {
                            _logger.WriteError($"{response.ReasonPhrase} {await response.Content.ReadAsStringAsync()}");
                            continue;
                        }


                        var ruleCode = new List<string>();
                        using (var sr = new StreamReader(stream))
                        {
                            string line;
                            while ((line = sr.ReadLine()) != null)
                            {
                                ruleCode.Add(line);
                            }
                        }

                        var ruleConfiguration = configuration.GetRuleConfiguration(ruleName);
                        var (preprocessedRule, _) = RuleFileParser.Read(ruleCode.ToArray());
                        ruleData.Add(new RuleOutputData(instance, ruleConfiguration, preprocessedRule.LanguageAsString()));
                    }
                }

                return ruleData;
            }
        }

        internal static Uri GetInvocationUrl(InstanceName instance, string rule)
        {
            var url = $"{instance.FunctionAppUrl}/api/{rule}";
            return new Uri(url);
        }

        internal async Task<(Uri url, string key)> GetInvocationUrlAndKey(InstanceName instance, string rule, CancellationToken cancellationToken = default)
        {
            var kudu = GetKudu(instance);

            // see https://github.com/projectkudu/kudu/wiki/Functions-API
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Post, $"api/functions/{rule}/listsecrets", cancellationToken))
            {
                using (var response = await client.SendAsync(request, cancellationToken))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var sr = new StreamReader(stream))
                        using (var jtr = new JsonTextReader(sr))
                        {
                            var js = new JsonSerializer();
                            var secret = js.Deserialize<KuduSecret>(jtr);

                            (Uri url, string key) invocation = (GetInvocationUrl(instance, rule), secret.Key);
                            return invocation;
                        }
                    }

                    string error = await response.Content.ReadAsStringAsync();
                    _logger.WriteError($"Failed to retrieve function key: {error}");
                    throw new InvalidOperationException("Failed to retrieve function key.");
                }
            }
        }

        internal async Task<bool> AddAsync(InstanceName instance, string ruleName, string filePath, CancellationToken cancellationToken)
        {
            _logger.WriteInfo($"Validate rule file {filePath}");
            var preprocessedRule = await LoadAndValidateRule(ruleName, filePath, cancellationToken);
            if (preprocessedRule == null)
            {
                _logger.WriteError("Rule file is invalid");
                return false;
            }
            _logger.WriteInfo("Rule file is valid");

            _logger.WriteVerbose($"Layout rule files");
            var inMemoryFiles = await PackagingFilesAsync(ruleName, preprocessedRule);
            using (var assemblyStream = await FunctionRuntimePackage.GetDeployedFunctionEntrypoint(instance, _azure, _logger, cancellationToken))
            {
                await inMemoryFiles.AddFunctionDefaultFiles(assemblyStream);
            }
            _logger.WriteInfo($"Packaging rule {ruleName} complete.");

            _logger.WriteVerbose($"Uploading rule files to {instance.PlainName}");
            bool ok = await UploadRuleFilesAsync(instance, ruleName, inMemoryFiles, cancellationToken);
            if (ok)
            {
                _logger.WriteInfo($"All {ruleName} files successfully uploaded to {instance.PlainName}.");
            }

            if (preprocessedRule.Impersonate)
            {
                _logger.WriteInfo($"Configure {ruleName} to execute impersonated.");
                ok &= await ConfigureAsync(instance, ruleName, impersonate: true, cancellationToken: cancellationToken);
                if (ok)
                {
                    _logger.WriteInfo($"Updated {ruleName} configuration successfully.");
                }
            }

            return ok;
        }

        private async Task<IPreprocessedRule> LoadAndValidateRule(string ruleName, string filePath, CancellationToken cancellationToken)
        {
            var engineLogger = new EngineWrapperLogger(_logger);
            var (preprocessedRule, _) = await RuleFileParser.ReadFile(filePath, engineLogger, cancellationToken);
            try
            {
                var rule = new Engine.ScriptedRuleWrapper(ruleName, preprocessedRule, engineLogger);
                var (success, diagnostics) = rule.Verify();
                if (!success)
                {
                    var messages = string.Join('\n', diagnostics.Select(d => d.ToString()));
                    if (!string.IsNullOrEmpty(messages))
                    {
                        _logger.WriteError($"Errors in the rule file {filePath}:\n{messages}");
                    }

                    return null;
                }
            }
            catch
            {
                return null;
            }

            // Rule file is valid
            return preprocessedRule;
        }

#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private static async Task<IDictionary<string, string>> PackagingFilesAsync(string ruleName, IPreprocessedRule preprocessedRule)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            var inMemoryFiles = new Dictionary<string, string>
            {
                { $"{ruleName}.rule", string.Join(Environment.NewLine, RuleFileParser.Write(preprocessedRule)) }
            };

            //var assembly = Assembly.GetExecutingAssembly();
            //await inMemoryFiles.AddFunctionDefaultFiles(assembly);

            return inMemoryFiles;
        }

        internal async Task<bool> UploadRuleFilesAsync(InstanceName instance, string ruleName, IDictionary<string, string> uploadFiles, CancellationToken cancellationToken)
        {
            /*
            PUT /api/vfs/{path}
            Puts a file at path.

            PUT /api/vfs/{path}/
            Creates a directory at path. The path can be nested, e.g. `folder1/folder2`.

            Note: when updating or deleting a file, ETag behavior will apply. You can pass a If-Match: "*" header to disable the ETag check.
            */
            var kudu = GetKudu(instance);
            var relativeUrl = $"api/vfs/site/wwwroot/{ruleName}/";

            using (var client = new HttpClient())
            {
                var exists = false;

                // check if function already exists
                using (var request = await kudu.GetRequestAsync(HttpMethod.Head, relativeUrl, cancellationToken))
                {
                    _logger.WriteVerbose($"Checking if function {ruleName} already exists in {instance.PlainName}...");
                    using (var response = await client.SendAsync(request))
                    {
                        exists = response.IsSuccessStatusCode;
                    }
                }

                if (!exists)
                {
                    _logger.WriteVerbose($"Creating function {ruleName} in {instance.PlainName}...");
                    using (var request = await kudu.GetRequestAsync(HttpMethod.Put, relativeUrl, cancellationToken))
                    {
                        using (var response = await client.SendAsync(request, cancellationToken))
                        {
                            bool ok = response.IsSuccessStatusCode;
                            if (!ok)
                            {
                                _logger.WriteError($"Upload failed with {response.ReasonPhrase}");
                                return false;
                            }
                        }
                    }

                    _logger.WriteInfo($"Function {ruleName} created.");
                }

                foreach (var (fileName, fileContent) in uploadFiles)
                {
                    _logger.WriteVerbose($"Uploading {fileName} to {instance.PlainName}...");
                    var fileUrl = $"{relativeUrl}{fileName}";
                    using (var request = await kudu.GetRequestAsync(HttpMethod.Put, fileUrl, cancellationToken))
                    {
                        //HACK -> request.Headers.IfMatch.Add(new EntityTagHeaderValue("*", false)); <- won't work
                        request.Headers.Add("If-Match", "*");
                        request.Content = new StringContent(fileContent);
                        using (var response = await client.SendAsync(request, cancellationToken))
                        {
                            bool ok = response.IsSuccessStatusCode;
                            if (!ok)
                            {
                                _logger.WriteError($"Failed uploading {fileName} with {response.ReasonPhrase}");
                                return false;
                            }
                        }
                    }

                    _logger.WriteInfo($"{fileName} successfully uploaded to {instance.PlainName}.");
                }//for

                return await TriggerSyncing(client, instance, cancellationToken);
            }
        }

        // see https://docs.microsoft.com/en-us/azure/azure-functions/functions-deployment-technologies#trigger-syncing
        private async Task<bool> TriggerSyncing(HttpClient client, InstanceName instance, CancellationToken cancellationToken)
        {
            var webFunctionApp = await azure.AppServices.FunctionApps.GetByResourceGroupAsync(instance.ResourceGroupName, instance.FunctionAppName, cancellationToken);
            string masterKey = await webFunctionApp.GetMasterKeyAsync(cancellationToken);

            using (var content = new StringContent(string.Empty))
            {
                string triggerSyncingUrl = $"{instance.FunctionAppUrl}/admin/host/synctriggers?code={masterKey}";
                using (var response = await client.PostAsync(triggerSyncingUrl, content, cancellationToken))
                {
                    bool ok = response.IsSuccessStatusCode;
                    if (!ok)
                    {
                        _logger.WriteError($"Failed syncing triggers with {response.ReasonPhrase}");
                        return false;
                    }
                }
            }
            return true;
        }

        internal async Task<bool> RemoveAsync(InstanceName instance, string name, CancellationToken cancellationToken)
        {
            var kudu = GetKudu(instance);
            // undocumented but works, see https://github.com/projectkudu/kudu/wiki/Functions-API
            _logger.WriteInfo($"Removing Function {name} in {instance.PlainName}...");
            using (var client = new HttpClient())
            {
                using (var request = await kudu.GetRequestAsync(HttpMethod.Delete, $"api/functions/{name}", cancellationToken))
                using (var response = await client.SendAsync(request, cancellationToken))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.WriteError($"Failed removing Function {name} from {instance.PlainName} with {response.ReasonPhrase}");
                        return false;
                    }
                }
                if (!await TriggerSyncing(client, instance, cancellationToken))
                    return false;
            }

            //TODO BobSilent remove configuration (Enable/Disable or Impersonate)

            return true;
        }

        internal async Task<bool> ConfigureAsync(InstanceName instance, string name, bool? disable = null, bool? impersonate = null, CancellationToken cancellationToken = default)
        {
            var webFunctionApp = await GetWebApp(instance, cancellationToken);
            var configuration = await AggregatorConfiguration.ReadConfiguration(webFunctionApp);
            var ruleConfig = configuration.GetRuleConfiguration(name);

            if (disable.HasValue)
            {
                ruleConfig.IsDisabled = disable.Value;
            }

            if (impersonate.HasValue)
            {
                ruleConfig.Impersonate = impersonate.Value;
            }

            ruleConfig.WriteConfiguration(webFunctionApp);

            return true;
        }


        internal async Task<bool> UpdateAsync(InstanceName instance, string ruleName, string filePath, string requiredVersion, string sourceUrl, CancellationToken cancellationToken)
        {
            // check runtime package
            var package = new FunctionRuntimePackage(_logger);
            bool ok = await package.UpdateVersionAsync(requiredVersion, sourceUrl, instance, _azure, cancellationToken);
            if (ok)
            {
                ok = await AddAsync(instance, ruleName, filePath, cancellationToken);
            }

            return ok;
        }

        internal async Task<bool> UpdateAsync(InstanceName instance, string ruleName, string filePath, CancellationToken cancellationToken)
        {
            bool ok = await AddAsync(instance, ruleName, filePath, cancellationToken);
            // AddAsync
            // - read and parse file
            // - compile and validate content
                // - packaging
                // - upload
            // - Configure App for impersonate if needed
            return ok;
        }

        internal async Task<bool> InvokeLocalAsync(string projectName, string @event, int workItemId, string ruleFilePath, bool dryRun, SaveMode saveMode, bool impersonateExecution, CancellationToken cancellationToken)
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
                await devops.ConnectAsync(cancellationToken);
                _logger.WriteInfo($"Connected to Azure DevOps");

                Guid teamProjectId;
                using (var projectClient = devops.GetClient<ProjectHttpClient>())
                {
                    _logger.WriteVerbose($"Reading Azure DevOps project data...");
                    var project = await projectClient.GetProject(projectName);
                    _logger.WriteInfo($"Project {projectName} data read.");
                    teamProjectId = project.Id;
                }

                using (var clientsContext = new AzureDevOpsClientsContext(devops))
                {
                    _logger.WriteVerbose($"Rule code found at {ruleFilePath}");
                    var (preprocessedRule, _) = await RuleFileParser.ReadFile(ruleFilePath, cancellationToken);
                    var rule = new Engine.ScriptedRuleWrapper(Path.GetFileNameWithoutExtension(ruleFilePath), preprocessedRule)
                               {
                                   ImpersonateExecution = impersonateExecution
                               };

                    var engineLogger = new EngineWrapperLogger(_logger);
                    var engine = new Engine.RuleEngine(engineLogger, saveMode, dryRun: dryRun);

                    var workItem = await clientsContext.WitClient.GetWorkItemAsync(projectName, workItemId, expand: WorkItemExpand.All, cancellationToken: cancellationToken);
                    string result = await engine.RunAsync(rule, teamProjectId, workItem, clientsContext, cancellationToken);
                    _logger.WriteInfo($"Rule returned '{result}'");

                    return true;
                }
            }
        }

        internal async Task<bool> InvokeRemoteAsync(string account, string project, string @event, int workItemId, InstanceName instance, string ruleName, bool dryRun, SaveMode saveMode, bool impersonateExecution, CancellationToken cancellationToken)
        {
            // build the request ...
            _logger.WriteVerbose($"Retrieving {ruleName} Function Key...");
            var (ruleUrl, ruleKey) = await GetInvocationUrlAndKey(instance, ruleName, cancellationToken);
            _logger.WriteInfo($"{ruleName} Function Key retrieved.");

            ruleUrl = ruleUrl.AddToUrl(dryRun, saveMode, impersonateExecution);

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
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
                client.DefaultRequestHeaders.Add("x-functions-key", ruleKey);
                var content = new StringContent(body, Encoding.UTF8, "application/json");

                using (var response = await client.PostAsync(ruleUrl, content, cancellationToken))
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.Extensions.Configuration;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace aggregator
{
    /// <summary>
    /// Contains Aggregator specific code with no reference to Rule triggering
    /// </summary>
    internal class RuleWrapper
    {
        private readonly AggregatorConfiguration configuration;
        private readonly IAggregatorLogger logger;
        private readonly string ruleName;
        private readonly string functionDirectory;

        public RuleWrapper(AggregatorConfiguration configuration, IAggregatorLogger logger, string ruleName, string functionDirectory)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.ruleName = ruleName;
            this.functionDirectory = functionDirectory;
        }

        internal async Task<string> Execute(string aggregatorVersion, dynamic data)
        {
            if (string.IsNullOrEmpty(aggregatorVersion))
            {
                aggregatorVersion = "0.1";
            }

            string collectionUrl = data.resourceContainers.collection.baseUrl;
            string eventType = data.eventType;
            int workItemId = (eventType != "workitem.updated") ? data.resource.id : data.resource.workItemId;

            logger.WriteVerbose($"Connecting to Azure DevOps using {configuration.DevOpsTokenType}...");
            var clientCredentials = default(VssCredentials);
            if (configuration.DevOpsTokenType == DevOpsTokenType.PAT)
            {
                clientCredentials = new VssBasicCredential(configuration.DevOpsTokenType.ToString(), configuration.DevOpsToken);
            } else
            {
                logger.WriteError($"Azure DevOps Token type {configuration.DevOpsTokenType} not supported!");
                throw new ArgumentOutOfRangeException(nameof(configuration.DevOpsTokenType));
            }
            var devops = new VssConnection(new Uri(collectionUrl), clientCredentials);
            await devops.ConnectAsync();
            logger.WriteInfo($"Connected to Azure DevOps");
            var witClient = devops.GetClient<WorkItemTrackingHttpClient>();
            var context = new Engine.EngineContext(witClient, logger);
            var store = new Engine.WorkItemStore(context);
            var self = store.GetWorkItem(workItemId);
            logger.WriteInfo($"Initial WorkItem {workItemId} retrieved from {collectionUrl}");

            string ruleFilePath = Path.Combine(functionDirectory, $"{ruleName}.rule");
            if (!File.Exists(ruleFilePath))
            {
                logger.WriteError($"Rule code not found at {ruleFilePath}");
                return "Rule file not found!";
            }

            logger.WriteVerbose($"Rule code found at {ruleFilePath}");
            string ruleCode = File.ReadAllText(ruleFilePath);

            logger.WriteInfo($"Executing Rule...");
            var globals = new Engine.Globals {
                self = self,
                store = store
            };

            var types = new List<Type>() {
                typeof(object),
                typeof(System.Linq.Enumerable),
                typeof(System.Collections.Generic.CollectionExtensions)
            };
            var references = types.ConvertAll(t => t.Assembly).Distinct();

            var scriptOptions = ScriptOptions.Default
                .WithEmitDebugInformation(true)
                .WithReferences(references)
                // Add namespaces
                .WithImports("System","System.Linq","System.Collections.Generic");
            var roslynScript = CSharpScript.Create<string>(
                code: ruleCode,
                options: scriptOptions,
                globalsType: typeof(Engine.Globals));
            var result = await roslynScript.RunAsync(globals);
            if (result.Exception != null)
            {
                logger.WriteError($"Rule failed with {result.Exception}");
            }
            else if(result.ReturnValue != null)
            {
                logger.WriteInfo($"Rule succeeded with {result.ReturnValue}");
            }
            else
            {
                logger.WriteInfo($"Rule succeeded, no return value");
            }

            logger.WriteVerbose($"Post-execution, save any change...");
            var saveRes = store.SaveChanges();
            if (saveRes.created + saveRes.updated > 0)
            {
                logger.WriteInfo($"Changes saved to Azure DevOps: {saveRes.created} created, {saveRes.updated} updated.");
            }
            else
            {
                logger.WriteInfo($"No changes saved to Azure DevOps.");
            }

            return result.ReturnValue;
        }
    }
}

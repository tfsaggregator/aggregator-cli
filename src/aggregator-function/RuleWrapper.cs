using System;
using System.IO;
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
    public class Globals
    {
        public WorkItem self;
    }

    /// <summary>
    /// Contains Aggregator specific code with no reference to Rule triggering
    /// </summary>
    internal class RuleWrapper
    {
        private readonly AggregatorConfiguration configuration;
        private readonly ILogger logger;
        private readonly string ruleName;
        private readonly string functionDirectory;

        public RuleWrapper(AggregatorConfiguration configuration, ILogger logger, string ruleName, string functionDirectory)
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
            int workItemId = data.resource.id;

            var vsts = Cache.Instance.GetVstsConnection(async () =>
            {
                logger.WriteVerbose($"Connecting to VSTS using {configuration.VstsTokenType}...");
                var clientCredentials = default(VssCredentials);
                if (configuration.VstsTokenType == VstsTokenType.PAT)
                {
                    clientCredentials = new VssBasicCredential(configuration.VstsTokenType.ToString(), configuration.VstsToken);
                }
                else
                {
                    logger.WriteError($"VSTS Token type {configuration.VstsTokenType} not supported!");
                    throw new ArgumentOutOfRangeException(nameof(configuration.VstsTokenType));
                }
                var connection = new VssConnection(new Uri(collectionUrl), clientCredentials);
                await connection.ConnectAsync();
                logger.WriteInfo($"Connected to VSTS");
                return connection;
            });

            var witClient = vsts.GetClient<WorkItemTrackingHttpClient>();
            var self = await witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All);
            logger.WriteInfo($"Self retrieved");

            var script = Cache.Instance.GetScript(() => {
                string ruleFilePath = Path.Combine(functionDirectory, $"{ruleName}.rule");
                if (!File.Exists(ruleFilePath))
                {
                    logger.WriteError($"Rule code not found at {ruleFilePath}");
                    return null;
                }

                logger.WriteVerbose($"Rule code found at {ruleFilePath}");
                string ruleCode = File.ReadAllText(ruleFilePath);

                logger.WriteVerbose($"Compiling rule code");
                var options = ScriptOptions.Default;
                var csscript = CSharpScript.Create<string>(ruleCode, options: options, globalsType: typeof(Globals));
                var errors = csscript.Compile();
                return csscript;
            });
            if (script == null)
            {
                return "Rule file not found!";
            }

            // END TODO
            logger.WriteVerbose($"Executing Rule...");
            var globals = new Globals { self = self };
            var result = await script.RunAsync(globals);
            logger.WriteVerbose($"Rule returned {result.ReturnValue}");
            return result.ReturnValue;
        }
    }
}

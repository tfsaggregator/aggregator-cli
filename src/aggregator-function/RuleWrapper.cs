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

        internal async Task<string> Execute(dynamic data)
        {
            string collectionUrl = data.resourceContainers.collection.baseUrl;
            string eventType = data.eventType;
            int workItemId = (eventType != "workitem.updated") ? data.resource.id : data.resource.workItemId;
            Guid teamProject = data.resourceContainers.project.id;

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

            using (var devops = new VssConnection(new Uri(collectionUrl), clientCredentials))
            {
                await devops.ConnectAsync();
                logger.WriteInfo($"Connected to Azure DevOps");
                using (var witClient = devops.GetClient<WorkItemTrackingHttpClient>())
                {
                    string ruleFilePath = Path.Combine(functionDirectory, $"{ruleName}.rule");
                    if (!File.Exists(ruleFilePath))
                    {
                        logger.WriteError($"Rule code not found at {ruleFilePath}");
                        return "Rule file not found!";
                    }

                    logger.WriteVerbose($"Rule code found at {ruleFilePath}");
                    string[] ruleCode = File.ReadAllLines(ruleFilePath);

                    var engine = new Engine.RuleEngine(logger, ruleCode);

                    return await engine.ExecuteAsync(collectionUrl, teamProject, workItemId, witClient);
                }
            }
        }
    }
}

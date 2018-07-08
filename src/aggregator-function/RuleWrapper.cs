using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
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
        private readonly IConfigurationRoot config;
        private readonly ILogger logger;
        private readonly string functionDirectory;

        public RuleWrapper(IConfigurationRoot config, ILogger logger, string functionDirectory)
        {
            this.config = config;
            this.logger = logger;
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

            string vstsTokenType = config["Aggregator_VstsTokenType"];
            string vstsToken = config["Aggregator_VstsToken"];

            logger.WriteVerbose($"Connecting to VSTS using {vstsTokenType}...");
            var clientCredentials = default(VssCredentials);
            if (string.Compare(vstsTokenType, "PAT", true) == 0)
            {
                clientCredentials = new VssBasicCredential(vstsTokenType, vstsToken);
            } else
            {
                throw new ArgumentOutOfRangeException(nameof(vstsTokenType));
            }
            var vsts = new VssConnection(new Uri(collectionUrl), clientCredentials);
            await vsts.ConnectAsync();
            logger.WriteInfo($"Connected to VSTS");
            var witClient = vsts.GetClient<WorkItemTrackingHttpClient>();
            var self = await witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All);
            logger.WriteInfo($"Self retrieved");

            string ruleFilePath;
            string probedDir = functionDirectory;
            do
            {
                ruleFilePath = Path.Combine(probedDir, "default.rule");
                logger.WriteVerbose($"probing {ruleFilePath}");
                probedDir = Directory.GetParent(probedDir).FullName;
            } while (!File.Exists(ruleFilePath));

            logger.WriteVerbose($"Rule code found at {ruleFilePath}");
            string ruleCode = File.ReadAllText(ruleFilePath);

            logger.WriteVerbose($"Executing Rule...");
            var globals = new Globals { self = self };
            var result = await CSharpScript.EvaluateAsync<string>(ruleCode, globals: globals);
            logger.WriteVerbose($"Rule returned {result}");
            return result;
        }
    }
}

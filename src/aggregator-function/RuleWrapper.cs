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
        private IConfigurationRoot config;

        public RuleWrapper(IConfigurationRoot config)
        {
            this.config = config;
        }

        internal async Task<string> Execute(string aggregatorVersion, dynamic data)
        {
            if (string.IsNullOrEmpty(aggregatorVersion))
            {
                aggregatorVersion = "0.1";
            }

            string collectionUrl = data.resourceContainers.collection.baseUrl;
            int workItemId = data.resource.id;
            string patToken = config["VSTS_PAT"];

            var clientCredentials = new VssBasicCredential("pat", patToken);
            var vsts = new VssConnection(new Uri(collectionUrl), clientCredentials);
            await vsts.ConnectAsync();
            var witClient = vsts.GetClient<WorkItemTrackingHttpClient>();
            var self = await witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All);

            var globals = new Globals { self = self };
            string ruleCode = File.ReadAllText("sample.rule");
            var result = await CSharpScript.EvaluateAsync<string>(ruleCode, globals: globals);

            return result;
        }
    }
}

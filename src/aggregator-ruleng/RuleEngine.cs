using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;

namespace aggregator.Engine
{
    public class RuleEngine
    {
        private readonly IAggregatorLogger logger;
        private readonly Script<string> roslynScript;

        public RuleEngine(IAggregatorLogger logger, string ruleCode)
        {
            this.logger = logger;

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
                .WithImports("System", "System.Linq", "System.Collections.Generic");
            this.roslynScript = CSharpScript.Create<string>(
                code: ruleCode,
                options: scriptOptions,
                globalsType: typeof(Globals));
        }

        public async Task<string> ExecuteAsync(string collectionUrl, int workItemId, WorkItemTrackingHttpClientBase witClient)
        {
            var context = new EngineContext(witClient, logger);
            var store = new WorkItemStore(context);
            var self = store.GetWorkItem(workItemId);
            logger.WriteInfo($"Initial WorkItem {workItemId} retrieved from {collectionUrl}");

            var globals = new Globals
            {
                self = self,
                store = store
            };

            logger.WriteInfo($"Executing Rule...");
            var result = await roslynScript.RunAsync(globals);
            if (result.Exception != null)
            {
                logger.WriteError($"Rule failed with {result.Exception}");
            }
            else if (result.ReturnValue != null)
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

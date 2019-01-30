using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;

namespace aggregator.Engine
{
    public enum EngineState
    {
        Unknown,
        Success,
        Error
    }

    /// <summary>
    /// Entry point to execute rules, indipendent of environment
    /// </summary>
    public class RuleEngine
    {
        private readonly IAggregatorLogger logger;
        private readonly Script<string> roslynScript;
        private readonly SaveMode saveMode;

        public RuleEngine(IAggregatorLogger logger, string[] ruleCode, SaveMode mode, bool dryRun)
        {
            State = EngineState.Unknown;

            this.logger = logger;
            this.saveMode = mode;
            this.DryRun = dryRun;

            var directives = new DirectivesParser(logger, ruleCode);
            if (!directives.Parse())
            {
                State = EngineState.Error;
                return;
            }

            if (directives.Language == DirectivesParser.Languages.Csharp)
            {
                var types = new List<Type>() {
                        typeof(object),
                        typeof(System.Linq.Enumerable),
                        typeof(System.Collections.Generic.CollectionExtensions),
                        typeof(Microsoft.VisualStudio.Services.WebApi.IdentityRef)
                    };
                var references = types.ConvertAll(t => t.Assembly).Distinct();

                var scriptOptions = ScriptOptions.Default
                    .WithEmitDebugInformation(true)
                    .WithReferences(references)
                    // Add namespaces
                    .WithImports(
                        "System",
                        "System.Linq",
                        "System.Collections.Generic",
                        "Microsoft.VisualStudio.Services.WebApi"
                    );

                this.roslynScript = CSharpScript.Create<string>(
                    code: directives.GetRuleCode(),
                    options: scriptOptions,
                    globalsType: typeof(Globals));
            }
            else
            {
                logger.WriteError($"Cannot execute rule: language is not supported.");
                State = EngineState.Error;
            }
        }

        /// <summary>
        /// State is used by unit tests
        /// </summary>
        public EngineState State { get; private set; }
        public bool DryRun { get; }

        public async Task<string> ExecuteAsync(string collectionUrl, Guid projectId, string projectName, string personalAccessToken, int workItemId, WorkItemTrackingHttpClientBase witClient, CancellationToken cancellationToken)
        {
            if (State == EngineState.Error)
            {
                return string.Empty;
            }

            var context = new EngineContext(witClient, projectId, projectName, personalAccessToken, logger);
            var store = new WorkItemStore(context);
            var self = store.GetWorkItem(workItemId);
            logger.WriteInfo($"Initial WorkItem {workItemId} retrieved from {collectionUrl}");

            var globals = new Globals
            {
                self = self,
                store = store
            };

            logger.WriteInfo($"Executing Rule...");
            var result = await roslynScript.RunAsync(globals, cancellationToken);
            if (result.Exception != null)
            {
                logger.WriteError($"Rule failed with {result.Exception}");
                State = EngineState.Error;
            }
            else if (result.ReturnValue != null)
            {
                logger.WriteInfo($"Rule succeeded with {result.ReturnValue}");
            }
            else
            {
                logger.WriteInfo($"Rule succeeded, no return value");
            }
            State = EngineState.Success;

            logger.WriteVerbose($"Post-execution, save any change (mode {saveMode})...");
            var saveRes = await store.SaveChanges(saveMode, !DryRun, cancellationToken);
            if (saveRes.created + saveRes.updated > 0)
            {
                logger.WriteInfo($"Changes saved to Azure DevOps (mode {saveMode}): {saveRes.created} created, {saveRes.updated} updated.");
            }
            else
            {
                logger.WriteInfo($"No changes saved to Azure DevOps.");
            }

            return result.ReturnValue;
        }
    }
}

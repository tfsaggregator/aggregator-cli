using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace aggregator.Engine
{
    public enum EngineState
    {
        Unknown,
        Success,
        Error
    }

    /// <summary>
    /// Entry point to execute rules, independent of environment
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
                var references = LoadReferences(directives);
                var imports = GetImports(directives);

                var scriptOptions = ScriptOptions.Default
                    .WithEmitDebugInformation(true)
                    .WithReferences(references)
                    // Add namespaces
                    .WithImports(imports)
                    ;

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

        private static IEnumerable<Assembly> LoadReferences(DirectivesParser directives)
        {
            var types = new List<Type>() {
                        typeof(object),
                        typeof(System.Linq.Enumerable),
                        typeof(System.Collections.Generic.CollectionExtensions),
                        typeof(Microsoft.VisualStudio.Services.WebApi.IdentityRef),
                        typeof(WorkItemWrapper)
                    };
            var references = types.ConvertAll(t => t.Assembly);
            // user references
            foreach (var reference in directives.References)
            {
                var name = new AssemblyName(reference);
                references.Add(Assembly.Load(name));
            }

            return references.Distinct();
        }

        private static IEnumerable<string> GetImports(DirectivesParser directives)
        {
            var imports = new List<string>
                    {
                        "System",
                        "System.Linq",
                        "System.Collections.Generic",
                        "Microsoft.VisualStudio.Services.WebApi",
                        "aggregator.Engine"
                    };
            imports.AddRange(directives.Imports);
            return imports.Distinct();
        }

        /// <summary>
        /// State is used by unit tests
        /// </summary>
        public EngineState State { get; private set; }
        public bool DryRun { get; }

        public async Task<string> ExecuteAsync(Guid projectId, WorkItemData workItemPayload, WorkItemTrackingHttpClient witClient, CancellationToken cancellationToken)
        {
            if (State == EngineState.Error)
            {
                return string.Empty;
            }

            var workItem = workItemPayload.WorkItem;
            var context = new EngineContext(witClient, projectId, workItem.GetTeamProject(), logger);
            var store = new WorkItemStore(context, workItem);
            var self = store.GetWorkItem(workItem.Id.Value);
            var selfUpdate = new WorkItemUpdateWrapper(workItemPayload.WorkItemUpdate);
            logger.WriteInfo($"Initial WorkItem {self.Id} retrieved from {witClient.BaseAddress}");

            var globals = new Globals
            {
                self = self,
                selfUpdate = selfUpdate,
                store = store,
                logger = logger
            };

            logger.WriteInfo($"Executing Rule...");
            var result = await roslynScript.RunAsync(globals, cancellationToken);
            if (result.Exception != null)
            {
                logger.WriteError($"Rule failed with {result.Exception}");
                State = EngineState.Error;
            }
            else
            {
                logger.WriteInfo($"Rule succeeded with {result.ReturnValue ?? "no return value"}");
                State = EngineState.Success;
            }

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

        public (bool success, ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics) VerifyRule()
        {
            ImmutableArray<Microsoft.CodeAnalysis.Diagnostic> diagnostics = roslynScript.Compile();
            (bool, ImmutableArray<Microsoft.CodeAnalysis.Diagnostic>) result;
            if (diagnostics.Any())
            {
                State = EngineState.Error;
                result = (false, diagnostics);
            }
            else
            {
                State = EngineState.Success;
                result = (true, ImmutableArray.Create<Microsoft.CodeAnalysis.Diagnostic>());
            }

            return result;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using aggregator.Engine.Language;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace aggregator.Engine
{
    internal interface IRuleEngine
    {
        Task<string> RunAsync(IRule rule, Guid projectId, WorkItemData workItemPayload, string eventType, IClientsContext clients, CancellationToken cancellationToken = default);
    }

    public abstract class RuleEngineBase : IRuleEngine
    {
        protected IAggregatorLogger logger;

        protected SaveMode saveMode;

        protected bool dryRun;


        protected RuleEngineBase(IAggregatorLogger logger, SaveMode saveMode, bool dryRun)
        {
            this.logger = logger;
            this.saveMode = saveMode;
            this.dryRun = dryRun;
        }

        public async Task<string> RunAsync(IRule rule, Guid projectId, WorkItemData workItemPayload, string eventType, IClientsContext clients, CancellationToken cancellationToken = default)
        {
            var executionContext = CreateRuleExecutionContext(projectId, workItemPayload, eventType, clients, rule.Settings);

            var result = await ExecuteRuleAsync(rule, executionContext, cancellationToken);

            return result;
        }

        protected abstract Task<string> ExecuteRuleAsync(IRule rule, RuleExecutionContext executionContext, CancellationToken cancellationToken = default);

        protected RuleExecutionContext CreateRuleExecutionContext(Guid projectId, WorkItemData workItemPayload, string eventType, IClientsContext clients, IRuleSettings ruleSettings)
        {
            var workItem = workItemPayload.WorkItem;
            var context = new EngineContext(clients, projectId, workItem.GetTeamProject(), logger, ruleSettings);
            var store = new WorkItemStore(context, workItem);
            var self = store.GetWorkItem(workItem.Id.Value);
            var selfChanges = new WorkItemUpdateWrapper(workItemPayload.WorkItemUpdate);
            logger.WriteInfo($"Initial WorkItem {self.Id} retrieved from {clients.WitClient.BaseAddress}");

            var globals = new RuleExecutionContext
            {
                self = self,
                selfChanges = selfChanges,
                store = store,
                logger = logger,
                eventType = eventType
            };
            return globals;
        }
    }


    public class RuleEngine : RuleEngineBase
    {
        public RuleEngine(IAggregatorLogger logger, SaveMode saveMode, bool dryRun) : base(logger, saveMode, dryRun)
        {
        }

        protected override async Task<string> ExecuteRuleAsync(IRule rule, RuleExecutionContext executionContext, CancellationToken cancellationToken = default)
        {
            var result = await rule.ApplyAsync(executionContext, cancellationToken);

            var store = executionContext.store;
            var (created, updated) = await store.SaveChanges(saveMode, !dryRun, rule.ImpersonateExecution, cancellationToken);
            if (created + updated > 0)
            {
                logger.WriteInfo($"Changes saved to Azure DevOps (mode {saveMode}): {created} created, {updated} updated.");
            }
            else
            {
                logger.WriteInfo($"No changes saved to Azure DevOps.");
            }

            return result.Value;
        }
    }
}

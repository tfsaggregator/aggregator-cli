using System;
using System.Threading;

namespace aggregator.Engine
{
    public class EngineContext
    {
        public EngineContext(IClientsContext clients, Guid projectId, string projectName, IAggregatorLogger logger, IRuleSettings ruleSettings, bool dryRun, CancellationToken cancellationToken)
        {
            Clients = clients;
            Logger = logger;
            Tracker = new Tracker();
            ProjectId = projectId;
            ProjectName = projectName;
            RuleSettings = ruleSettings;
            CancellationToken = cancellationToken;
            DryRun = dryRun;
        }

        public Guid ProjectId { get; internal set; }
        public string ProjectName { get; internal set; }
        internal IClientsContext Clients { get; }
        internal IAggregatorLogger Logger { get; }
        internal Tracker Tracker { get; }
        internal IRuleSettings RuleSettings { get; }
        internal CancellationToken CancellationToken { get; }
        internal bool DryRun { get; }
    }
}

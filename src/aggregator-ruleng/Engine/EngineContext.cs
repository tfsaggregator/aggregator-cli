using System;

namespace aggregator.Engine
{
    public class EngineContext
    {
        public EngineContext(IClientsContext clients, Guid projectId, string projectName, IAggregatorLogger logger, IRuleSettings ruleSettings)
        {
            Clients = clients;
            Logger = logger;
            Tracker = new Tracker();
            ProjectId = projectId;
            ProjectName = projectName;
            RuleSettings = ruleSettings;
        }

        public Guid ProjectId { get; internal set; }
        public string ProjectName { get; internal set; }
        internal IClientsContext Clients { get; }
        internal IAggregatorLogger Logger { get; }
        internal Tracker Tracker { get; }
        internal IRuleSettings RuleSettings { get; }
    }
}

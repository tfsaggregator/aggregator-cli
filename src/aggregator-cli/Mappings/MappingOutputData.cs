namespace aggregator.cli
{
    internal class MappingOutputData : ILogDataObject
    {
#pragma warning disable S4487,IDE0052 // Unread "private" fields should be removed
        private readonly string instanceName;
#pragma warning restore S4487,IDE0052 // Unread "private" fields should be removed
        private readonly string rule;
        private readonly bool executeImpersonated;
        private readonly string project;
        private readonly string @event;
        private readonly string status;

        internal MappingOutputData(InstanceName instance, string rule, bool executeImpersonated, string project, string @event, string status)
        {
            this.instanceName = instance.PlainName;
            this.rule = rule;
            this.executeImpersonated = executeImpersonated;
            this.project = project;
            this.@event = @event;
            this.status = status;
        }

        public string AsHumanReadable()
        {
            return $"Project {project} invokes rule {rule}{(executeImpersonated ? " (impersonated)" : string.Empty)} for {@event} (status {status})";
        }
    }
}

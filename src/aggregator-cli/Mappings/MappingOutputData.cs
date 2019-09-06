namespace aggregator.cli
{
    internal class MappingOutputData : ILogDataObject
    {
        private string instanceName;
        private string rule;
        private bool executeImpersonated;
        private string project;
        private string @event;
        private string status;

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

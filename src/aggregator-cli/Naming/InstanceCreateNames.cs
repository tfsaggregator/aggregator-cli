namespace aggregator.cli
{
    internal class InstanceCreateNames : InstanceName
    {
        protected InstanceCreateNames(string name, string resourceGroup, bool isCustom, string functionAppName)
            : base(name, resourceGroup, isCustom, functionAppName)
        {
        }

        public string HostingPlanName { get; protected set; }
        public string AppInsightName { get; protected set; }
        public string StorageAccountName { get; protected set; }
    }
}

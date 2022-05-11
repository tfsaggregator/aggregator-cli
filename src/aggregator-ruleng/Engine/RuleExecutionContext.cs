namespace aggregator.Engine
{
    ///<summary>
    /// The set of objects accessible from a rule.
    ///</summary>
    public class RuleExecutionContext
    {
#pragma warning disable S1104 // Fields should not have public accessibility
        public string ruleName;
        public WorkItemWrapper self;
        public WorkItemUpdateWrapper selfChanges;
        public WorkItemStore store;
        public IAggregatorLogger logger;
        public string eventType;
#pragma warning restore S1104 // Fields should not have public accessibility
    }
}

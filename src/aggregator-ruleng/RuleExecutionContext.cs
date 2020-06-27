using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    ///<summary>
    /// The set of objects accessible from a rule.
    ///</summary>
    public class RuleExecutionContext
    {
        public WorkItemWrapper self;
        public WorkItemUpdateWrapper selfChanges;
        public WorkItemStore store;
        public IAggregatorLogger logger;
    }
}

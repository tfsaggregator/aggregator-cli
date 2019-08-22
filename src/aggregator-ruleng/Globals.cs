using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    public class Globals
    {
        public WorkItemWrapper self;
        public WorkItemUpdateWrapper selfChanges;
        public WorkItemStore store;
        public IAggregatorLogger logger;
    }
}

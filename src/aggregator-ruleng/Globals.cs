using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    public class Globals
    {
        public WorkItemWrapper self;
        public WorkItemUpdateWrapper selfUpdate;
        public WorkItemStore store;
        public IAggregatorLogger logger;
    }
}

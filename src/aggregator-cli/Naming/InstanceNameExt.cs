using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    internal class InstanceNameExt : InstanceName
    {
        protected InstanceNameExt(string name, string resourceGroup, bool isCustom, string functionAppName)
            :base(name, resourceGroup, isCustom, functionAppName)
        {
        }

        public string HostingPlanName { get; protected set; }
        public string AppInsightName { get; protected set; }
        public string StorageAccountName { get; protected set; }
    }
}

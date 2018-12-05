using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    internal class RuleOutputData : ILogDataObject
    {
        string instanceName;
        KuduFunction function;

        internal RuleOutputData(InstanceName instance, KuduFunction function)
        {
            this.instanceName = instance.PlainName;
            this.function = function;
        }

        public string AsHumanReadable()
        {
            return $"Rule {instanceName}/{function.Name} {(function.Config.Disabled ? "(disabled)" : string.Empty)}";
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    internal class RuleOutputData : ILogDataObject
    {
        private readonly string instanceName;
        private readonly string ruleName;
        private readonly bool isDisabled;
        private readonly bool isImpersonated;

        internal RuleOutputData(InstanceName instance, KuduFunction function, bool isImpersonated)
        {
            this.instanceName = instance.PlainName;
            this.ruleName = function.Name;
            this.isDisabled = function.Config.Disabled;
            this.isImpersonated = isImpersonated;
        }

        public string AsHumanReadable()
        {
            return $"Rule {instanceName}/{ruleName} {(isImpersonated ? "*execute impersonated*" : string.Empty)} {(isDisabled ? "(disabled)" : string.Empty)}";
        }
    }
}

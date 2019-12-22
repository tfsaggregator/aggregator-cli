using System;
using System.Collections.Generic;
using System.Text;


namespace aggregator.cli
{
    internal class RuleOutputData : ILogDataObject
    {
        private readonly string instanceName;
        private readonly string ruleName;
        private readonly string ruleLanguage;
        private readonly bool isDisabled;

        internal RuleOutputData(InstanceName instance, IRuleConfiguration ruleConfiguration, string ruleLanguage)
        {
            this.instanceName = instance.PlainName;
            this.ruleName = ruleConfiguration.RuleName;
            this.isDisabled = ruleConfiguration.IsDisabled;
            this.ruleLanguage = ruleLanguage;
        }

        public string AsHumanReadable()
        {
            return $"Rule {instanceName}/{ruleName} {(isDisabled ? "(disabled)" : string.Empty)}";
        }
    }
}

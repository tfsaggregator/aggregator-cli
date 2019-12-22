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
        private readonly bool isImpersonated;

        internal RuleOutputData(InstanceName instance, IRuleConfiguration ruleConfiguration, string ruleLanguage)
        {
            this.instanceName = instance.PlainName;
            this.ruleName = ruleConfiguration.RuleName;
            this.isDisabled = ruleConfiguration.IsDisabled;
            this.isImpersonated = ruleConfiguration.Impersonate;
            this.ruleLanguage = ruleLanguage;
        }

        public string AsHumanReadable()
        {
            return $"Rule {instanceName}/{ruleName} {(isImpersonated ? "*execute impersonated*" : string.Empty)} {(isDisabled ? "(disabled)" : string.Empty)}";
        }
    }
}

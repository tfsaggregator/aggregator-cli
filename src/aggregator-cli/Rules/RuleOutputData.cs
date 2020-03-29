using System;
using System.Collections.Generic;
using System.Text;


namespace aggregator.cli
{
    internal class RuleOutputData : ILogDataObject
    {
        public string InstanceName { get; }
        public string RuleName { get; }
        public string RuleLanguage { get; }
        public bool IsDisabled { get; }
        public bool IsImpersonated { get; }

        internal RuleOutputData(InstanceName instance, IRuleConfiguration ruleConfiguration, string ruleLanguage)
        {
            this.InstanceName = instance.PlainName;
            this.RuleName = ruleConfiguration.RuleName;
            this.IsDisabled = ruleConfiguration.IsDisabled;
            this.IsImpersonated = ruleConfiguration.Impersonate;
            this.RuleLanguage = ruleLanguage;
        }

        public string AsHumanReadable()
        {
            return $"Rule {InstanceName}/{RuleName} {(IsImpersonated ? "*execute impersonated*" : string.Empty)} {(IsDisabled ? "(disabled)" : string.Empty)}";
        }
    }
}

using System.Diagnostics;


namespace aggregator.Model
{
    [DebuggerDisplay("{RuleName,nq}, Disabled={IsDisabled}, Impersonate={Impersonate}")]
    internal class RuleConfiguration : IRuleConfiguration
    {
        public RuleConfiguration(string ruleName)
        {
            RuleName = ruleName;
        }
        public string RuleName { get; }
        public bool IsDisabled { get; set; }
    }
}
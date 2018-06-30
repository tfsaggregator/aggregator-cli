using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("configure.rule", HelpText = "Change a rule configuration.")]
    class ConfigureRuleCommand : CommandBase
    {
        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        [Option('d', "disable", SetName = "disable", HelpText = "Disable the rule.")]
        public bool Disable { get; set; }
        [Option('e', "enable", SetName = "enable", HelpText = "Enable the rule.")]
        public bool Enable { get; set; }

        internal override async Task<int> RunAsync()
        {
            var azure = await AzureLogon.Load()?.LogonAsync();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
                return 2;
            }

            var vsts = await VstsLogon.Load()?.LogonAsync();
            if (vsts == null)
            {
                WriteError($"Must logon.vsts first.");
                return 2;
            }

            var rules = new AggregatorRules(azure);
            bool ok = false;
            if (Disable || Enable)
            {
                ok = await rules.EnableAsync(Instance, Name, Disable);
            }
            return ok ? 0 : 1;
        }
    }
}

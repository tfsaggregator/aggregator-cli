using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("remove.rule", HelpText = "Remove a rule from existing Aggregator instance in Azure.")]
    class RemoveRuleCommand : CommandBase
    {
        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

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

            var mappings = new AggregatorMappings(vsts, azure, this);
            bool ok = await mappings.RemoveRuleAsync(Instance, Name);

            var rules = new AggregatorRules(azure, this);
            //rules.Progress += Instances_Progress;
            ok = ok && await rules.RemoveAsync(Instance, Name);
            return ok ? 0 : 1;
        }
    }
}

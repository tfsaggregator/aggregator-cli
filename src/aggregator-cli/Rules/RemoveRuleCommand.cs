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
            var logon = await Logon<AzureLogon, VstsLogon>();
            var instance = new InstanceName(Instance);
            var mappings = new AggregatorMappings(logon.vsts, logon.azure, this);
            bool ok = await mappings.RemoveRuleAsync(instance, Name);

            var rules = new AggregatorRules(logon.azure, this);
            //rules.Progress += Instances_Progress;
            ok = ok && await rules.RemoveAsync(instance, Name);
            return ok ? 0 : 1;
        }
    }
}

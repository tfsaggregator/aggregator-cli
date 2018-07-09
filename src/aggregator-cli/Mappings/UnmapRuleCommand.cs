using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("unmap.rule", HelpText = "Unmaps an Aggregator Rule from a VSTS Project.")]
    class UnmapRuleCommand : CommandBase
    {
        [Option('e', "event", Required = true, HelpText = "VSTS event.")]
        public string Event { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('r', "rule", Required = true, HelpText = "Aggregator rule name.")]
        public string Rule { get; set; }

        internal override async Task<int> RunAsync()
        {
            var vsts = await VstsLogon.Load()?.LogonAsync();
            if (vsts == null)
            {
                WriteError($"Must logon.vsts first.");
                return 2;
            }

            var azure = await AzureLogon.Load()?.LogonAsync();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
                return 2;
            }

            var instance = new InstanceName(Instance);
            var mappings = new AggregatorMappings(vsts, azure, this);
            bool ok = await mappings.RemoveRuleEventAsync(Event, instance, Rule);
            return ok ? 0 : 1;
        }
    }
}

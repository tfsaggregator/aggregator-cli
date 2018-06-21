using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("map.rule", HelpText = "Maps an Aggregator Rule to existing VSTS Projects.")]
    class MapRuleCommand : CommandBase
    {
        [Option('p', "project", Required = true, HelpText = "VSTS project name.")]
        public string Project { get; set; }

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

            var azure = AzureLogon.Load()?.Logon();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
                return 2;
            }

            var mappings = new AggregatorMappings(vsts, azure);
            bool ok = await mappings.Add(Project, Event, Instance, Rule);
            return ok ? 0 : 1;
        }
    }
}

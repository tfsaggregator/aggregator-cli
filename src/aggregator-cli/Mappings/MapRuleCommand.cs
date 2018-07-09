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
            var logon = await Logon<AzureLogon, VstsLogon>();
            var mappings = new AggregatorMappings(logon.vsts, logon.azure, this);
            bool ok = mappings.ValidateEvent(Event);
            if (!ok)
            {
                WriteError($"Invalid event type.");
                return 2;
            }
            var instance = new InstanceName(Instance);
            var id = await mappings.Add(Project, Event, instance, Rule);
            return id.Equals(Guid.Empty) ? 1 : 0;
        }
    }
}

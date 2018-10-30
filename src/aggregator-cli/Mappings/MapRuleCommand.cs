using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("map.rule", HelpText = "Maps an Aggregator Rule to existing Azure DevOps Projects.")]
    class MapRuleCommand : CommandBase
    {
        [Option('p', "project", Required = true, HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [Option('e', "event", Required = true, HelpText = "Azure DevOps event.")]
        public string Event { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('r', "rule", Required = true, HelpText = "Aggregator rule name.")]
        public string Rule { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .Build();
            var mappings = new AggregatorMappings(context.Devops, context.Azure, context.Logger);
            bool ok = DevOpsEvents.IsValidEvent(Event);
            if (!ok)
            {
                context.Logger.WriteError($"Invalid event type.");
                return 2;
            }
            var instance = new InstanceName(Instance, ResourceGroup);
            var id = await mappings.Add(Project, Event, instance, Rule);
            return id.Equals(Guid.Empty) ? 1 : 0;
        }
    }
}

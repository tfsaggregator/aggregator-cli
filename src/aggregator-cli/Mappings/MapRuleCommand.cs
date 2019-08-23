using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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

        // event filters: but cannot make AreaPath & Tag work
        //[Option("filterAreaPath", Required = false, HelpText = "Filter Azure DevOps event to include only Work Items under the specified Area Path.")]
        public string FilterAreaPath { get; set; }
        [Option("filterType", Required = false, HelpText = "Filter Azure DevOps event to include only Work Items of the specified Work Item Type.")]
        public string FilterType { get; set; }
        //[Option("filterTag", Required = false, HelpText = "Filter Azure DevOps event to include only Work Items containing the specified Tag.")]
        public string FilterTag { get; set; }
        [Option("filterFields", Required = false, HelpText = "Filter Azure DevOps event to include only work items with the specified Field(s) changed.")]
        public IEnumerable<string> FilterFields { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            var mappings = new AggregatorMappings(context.Devops, context.Azure, context.Logger);
            bool ok = DevOpsEvents.IsValidEvent(Event);
            if (!ok)
            {
                context.Logger.WriteError($"Invalid event type.");
                return 2;
            }

            var filters = new AggregatorMappings.EventFilters
            {
                AreaPath = FilterAreaPath,
                Type = FilterType,
                Tag = FilterTag,
                Fields = FilterFields
            };

            var instance = new InstanceName(Instance, ResourceGroup);
            var id = await mappings.AddAsync(Project, Event, filters, instance, Rule, cancellationToken);
            return id.Equals(Guid.Empty) ? 1 : 0;
        }
    }
}

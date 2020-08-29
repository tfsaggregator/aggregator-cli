using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{
    [Verb("map.rule", HelpText = "Maps an Aggregator Rule to existing Azure DevOps Projects.")]
    class MapRuleCommand : CommandBase
    {
        [Option('p', "project", Required = true, HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [ShowInTelemetry]
        [Option('e', "event", Required = true, HelpText = "Azure DevOps event.")]
        public string Event { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [ShowInTelemetry(TelemetryDisplayMode.Presence)]
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [ShowInTelemetry]
        [Option('r', "rule", Required = true, HelpText = "Aggregator rule name.")]
        public string Rule { get; set; }

        [ShowInTelemetry]
        [Option("impersonate", Required = false, HelpText = "Do rule changes on behalf of the person triggered the rule execution. See wiki for details, requires special account privileges.")]
        public bool ImpersonateExecution { get; set; }

        // event filters: but cannot make AreaPath & Tag work
        //[Option("filterAreaPath", Required = false, HelpText = "Filter Azure DevOps event to include only Work Items under the specified Area Path.")]
        public string FilterAreaPath { get; set; }
        [ShowInTelemetry]
        [Option("filterType", Required = false, HelpText = "Filter Azure DevOps event to include only Work Items of the specified Work Item Type.")]
        public string FilterType { get; set; }
        //[Option("filterTag", Required = false, HelpText = "Filter Azure DevOps event to include only Work Items containing the specified Tag.")]
        public string FilterTag { get; set; }
        [ShowInTelemetry]
        [Option("filterFields", Required = false, HelpText = "Filter Azure DevOps event to include only work items with the specified Field(s) changed.")]
        public IEnumerable<string> FilterFields { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            var mappings = new AggregatorMappings(context.Devops, context.Azure, context.Logger, context.Naming);
            bool ok = DevOpsEvents.IsValidEvent(Event);
            if (!ok)
            {
                context.Logger.WriteError($"Invalid event type.");
                return ExitCodes.InvalidArguments;
            }

            var filters = new EventFilters
            {
                AreaPath = FilterAreaPath,
                Type = FilterType,
                Tag = FilterTag,
                Fields = FilterFields
            };

            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var id = await mappings.AddAsync(Project, Event, filters, instance, Rule, ImpersonateExecution, cancellationToken);
            return id.Equals(Guid.Empty) ? ExitCodes.Failure : ExitCodes.Success;
        }
    }
}

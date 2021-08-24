using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{
    [Verb("map.local.rule", HelpText = "Maps an Aggregator Rule to existing Azure DevOps Server Projects.")]
    class MapLocalRuleCommand : CommandBase
    {
        [Option('p', "project", Required = true, HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [ShowInTelemetry]
        [Option('e', "event", Required = true, HelpText = "Azure DevOps event.")]
        public string Event { get; set; }

        [Option('t', "targetUrl", Required = true, HelpText = "Aggregator instance URL.")]
        public string TargetUrl { get; set; }

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
        [ShowInTelemetry]
        [Option("filterOnlyLinks", Required = false, HelpText = "Filter Azure DevOps event to include only work items with links added or removed.")]
        public bool FilterOnlyLinks { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
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
                Fields = FilterFields,
                OnlyLinks = FilterOnlyLinks,
            };

            var targetUrl = new Uri(TargetUrl);
            var id = await mappings.AddFromUrlAsync(Project, Event, filters, targetUrl, Rule, ImpersonateExecution, cancellationToken);
            return id.Equals(Guid.Empty) ? ExitCodes.Failure : ExitCodes.Success;
        }
    }
}

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("unmap.rule", HelpText = "Unmaps an Aggregator Rule from a Azure DevOps Project.")]
    class UnmapRuleCommand : CommandBase
    {
        [Option('e', "event", Required = true, HelpText = "Azure DevOps event.")]
        public string Event { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('p', "project", Required = false, Default = "*", HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [Option('r', "rule", Required = true, HelpText = "Aggregator rule name.")]
        public string Rule { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            bool ok = DevOpsEvents.IsValidEvent(Event) || Event == "*";
            if (!ok)
            {
                context.Logger.WriteError($"Invalid event type.");
                return ExitCodes.InvalidArguments;
            }
            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var mappings = new AggregatorMappings(context.Devops, context.Azure, context.Logger, context.Naming);
            var outcome = await mappings.RemoveRuleEventAsync(Event, instance, Project, Rule);
            switch (outcome)
            {
                case RemoveOutcome.Succeeded:
                    return ExitCodes.Success;
                case RemoveOutcome.NotFound:
                    context.Logger.WriteWarning($"No mapping(s) found for rule(s) {instance.PlainName}/{Rule}");
                    return ExitCodes.NotFound;
                case RemoveOutcome.Failed:
                    return ExitCodes.Failure;
                default:
                    return ExitCodes.Unexpected;
            }
        }
    }
}

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
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

        [Option('r', "rule", Required = true, HelpText = "Aggregator rule name.")]
        public string Rule { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .WithVstsLogon()
                .Build();
            var instance = new InstanceName(Instance);
            var mappings = new AggregatorMappings(context.Vsts, context.Azure, context.Logger);
            bool ok = await mappings.RemoveRuleEventAsync(Event, instance, Rule);
            return ok ? 0 : 1;
        }
    }
}

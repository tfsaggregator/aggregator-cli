using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("list.instances", HelpText = "Lists Aggregator instances.")]
    class ListInstancesCommand : CommandBase
    {
        [Option('l', "location", Required = false, HelpText = "Aggregator instance location (Azure region).")]
        public string Location { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .Build();
            var instances = new AggregatorInstances(context.Azure, context.Logger);
            if (!string.IsNullOrEmpty(Location))
            {
                context.Logger.WriteVerbose($"Searching aggregator instances in {Location}...");
                return await ListByLocationAsync(context, instances);

            }
            else if (!string.IsNullOrEmpty(ResourceGroup))
            {
                context.Logger.WriteVerbose($"Searching aggregator instances in {ResourceGroup}...");
                return await ListInResourceGroupAsync(context, instances);
            }
            else
            {
                context.Logger.WriteVerbose($"Searching aggregator instances in subscription...");
                return await ListAllAsync(context, instances);
            }
        }

        private async Task<int> ListByLocationAsync(CommandContext context, AggregatorInstances instances)
        {
            var found = await instances.ListByLocationAsync(Location);
            bool any = false;
            foreach (var dataObject in found)
            {
                context.Logger.WriteOutput(dataObject);
                any = true;
            }
            if (!any)
            {
                context.Logger.WriteInfo($"No aggregator instances found in {Location}.");
            }
            return 0;
        }

        private async Task<int> ListInResourceGroupAsync(CommandContext context, AggregatorInstances instances)
        {
            var found = await instances.ListInResourceGroupAsync(ResourceGroup);
            bool any = false;
            foreach (var dataObject in found)
            {
                context.Logger.WriteOutput(dataObject);
                any = true;
            }
            if (!any)
            {
                context.Logger.WriteInfo("No aggregator instances found.");
            }
            return 0;
        }

        private static async Task<int> ListAllAsync(CommandContext context, AggregatorInstances instances)
        {
            var found = await instances.ListAllAsync();
            bool any = false;
            foreach (var dataObject in found)
            {
                context.Logger.WriteOutput(dataObject);
                any = true;
            }
            if (!any)
            {
                context.Logger.WriteInfo("No aggregator instances found.");
            }
            return 0;
        }
    }
}

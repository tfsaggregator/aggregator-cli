using CommandLine;
using System.Threading;
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

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .BuildAsync(cancellationToken);
            var instances = new AggregatorInstances(context.Azure, context.Logger, context.Naming);
            if (!string.IsNullOrEmpty(Location))
            {
                context.Logger.WriteVerbose($"Searching aggregator instances in {Location} Region...");
                return await ListByLocationAsync(context, instances, cancellationToken);

            }
            else if (!string.IsNullOrEmpty(ResourceGroup))
            {
                context.Logger.WriteVerbose($"Searching aggregator instances in {ResourceGroup} Resource Group...");
                return await ListInResourceGroupAsync(context, instances, cancellationToken);
            }
            else
            {
                context.Logger.WriteVerbose($"Searching aggregator instances in whole subscription...");
                return await ListAllAsync(context, instances, cancellationToken);
            }
        }

        private async Task<int> ListByLocationAsync(CommandContext context, AggregatorInstances instances, CancellationToken cancellationToken)
        {
            var found = await instances.ListByLocationAsync(Location, cancellationToken);
            bool any = false;
            foreach (var dataObject in found)
            {
                context.Logger.WriteOutput(dataObject);
                any = true;
            }
            if (!any)
            {
                context.Logger.WriteInfo($"No aggregator instances found in {Location} Region.");
            }
            return 0;
        }

        private async Task<int> ListInResourceGroupAsync(CommandContext context, AggregatorInstances instances, CancellationToken cancellationToken)
        {
            var found = await instances.ListInResourceGroupAsync(ResourceGroup, cancellationToken);
            bool any = false;
            foreach (var dataObject in found)
            {
                context.Logger.WriteOutput(dataObject);
                any = true;
            }

            if (!any)
            {
                context.Logger.WriteInfo($"No aggregator instances found in {ResourceGroup} Resource Group.");
            }

            return 0;
        }

        private static async Task<int> ListAllAsync(CommandContext context, AggregatorInstances instances, CancellationToken cancellationToken)
        {
            var found = await instances.ListAllAsync(cancellationToken);
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

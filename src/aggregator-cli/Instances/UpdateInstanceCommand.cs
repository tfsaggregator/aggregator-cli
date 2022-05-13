using System.Threading;
using System.Threading.Tasks;

using CommandLine;


namespace aggregator.cli.Instances
{
    [Verb("update.instance", HelpText = "Updates an existing Aggregator instance in Azure, with latest runtime binaries.")]
    class UpdateInstanceCommand : CommandBase
    {
        [ShowInTelemetry(TelemetryDisplayMode.Presence)]
        [Option('g', "resourceGroup", Required = true, HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }
        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [ShowInTelemetry]
        [Option("requiredVersion", SetName = "nourl", Required = false, HelpText = "Version of Aggregator Runtime required.")]
        public string RequiredVersion { get; set; }
        [ShowInTelemetry(TelemetryDisplayMode.MaskOthersUrl)]
        [Option("sourceUrl", SetName = "url", Required = false, HelpText = "URL of Aggregator Runtime.")]
        public string SourceUrl { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                                .WithAzureLogon()
                                .WithAzureManagement()
                                .BuildAsync(cancellationToken);

            var instances = new AggregatorInstances(context.Azure, context.AzureManagement, context.Logger, context.Naming);
            var instance = context.Naming.GetInstanceCreateNames(Instance, ResourceGroup);

            bool ok = await instances.UpdateAsync(instance, RequiredVersion, SourceUrl, cancellationToken);
            return ok ? ExitCodes.Success : ExitCodes.Failure;
        }
    }
}

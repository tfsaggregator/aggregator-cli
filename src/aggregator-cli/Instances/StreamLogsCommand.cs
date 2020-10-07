using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{
    [Verb("stream.logs", HelpText = "Streams logs from an Aggregator instance.")]
    class StreamLogsCommand : CommandBase
    {
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .BuildAsync(cancellationToken);
            context.ResourceGroupDeprecationCheck(this.ResourceGroup);
            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var instances = new AggregatorInstances(context.Azure, context.Logger, context.Naming);
            bool ok = await instances.StreamLogsAsync(instance, cancellationToken);
            return ok ? ExitCodes.Success : ExitCodes.Failure;
        }
    }
}

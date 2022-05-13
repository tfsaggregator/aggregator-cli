using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{

    [Verb("update.mappings", HelpText = "Updates existing mappings from old to new Aggregator Instance.")]
    class UpdateMappingsCommand : CommandBase
    {
        [Option('s', "sourceInstance", Required = true, HelpText = "Source Aggregator instance name.")]
        public string SourceInstance { get; set; }

        [Option('d', "destInstance", Required = true, HelpText = "Destination Aggregator instance name.")]
        public string DestInstance { get; set; }

        [Option('g', "resourceGroup", Required = true, HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('p', "project", Required = false, Default = "", HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithDevOpsLogon()
                .WithAzureLogon()
                .BuildAsync(cancellationToken);
            if (SourceInstance == DestInstance)
            {
                context.Logger.WriteError("Source must be different from destination.");
                return ExitCodes.InvalidArguments;
            }
            var sourceInstance = context.Naming.Instance(SourceInstance, ResourceGroup);
            var destInstance = context.Naming.Instance(DestInstance, ResourceGroup);
            // HACK we pass null as the next calls do not use the Azure connection
            var mappings = new AggregatorMappings(context.Devops, context.Azure, context.Logger, context.Naming);
            cancellationToken.ThrowIfCancellationRequested();
            var outcome = await mappings.RemapAsync(sourceInstance, destInstance, Project, cancellationToken);
            switch (outcome)
            {
                case UpdateOutcome.Succeeded:
                    return ExitCodes.Success;
                case UpdateOutcome.NotFound:
                    context.Logger.WriteWarning($"No mappings found for instance {sourceInstance.PlainName}");
                    return ExitCodes.NotFound;
                case UpdateOutcome.Failed:
                    return ExitCodes.Failure;
                default:
                    return ExitCodes.Unexpected;
            }
        }
    }
}

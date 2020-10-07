using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{

    [Verb("list.mappings", HelpText = "Lists mappings from existing Azure DevOps Projects to Aggregator Rules.")]
    class ListMappingsCommand : CommandBase
    {
        [Option('i', "instance", Required = false, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('p', "project", Required = false, Default = "", HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            context.ResourceGroupDeprecationCheck(this.ResourceGroup);
            if (string.IsNullOrEmpty(Instance)
                && string.IsNullOrEmpty(Project))
            {
                context.Logger.WriteError("Specify at least one filtering parameter.");
                return ExitCodes.InvalidArguments;
            }
            var instance = string.IsNullOrEmpty(Instance) ? null : context.Naming.Instance(Instance, ResourceGroup);
            // HACK we pass null as the next calls do not use the Azure connection
            var mappings = new AggregatorMappings(context.Devops, null, context.Logger, context.Naming);
            bool any = false;
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var item in await mappings.ListAsync(instance, Project))
            {
                context.Logger.WriteOutput(item);
                any = true;
            }
            if (!any)
            {
                context.Logger.WriteInfo("No rule mappings found.");
                return ExitCodes.NotFound;
            }
            else
            {
                return ExitCodes.Success;
            }
        }
    }
}

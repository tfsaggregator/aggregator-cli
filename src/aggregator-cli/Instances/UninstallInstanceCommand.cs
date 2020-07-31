using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("uninstall.instance", HelpText = "Destroy an Aggregator instance in Azure.")]
    class UninstallInstanceCommand : CommandBase
    {
        [Option('n', "name", Required = true, HelpText = "Aggregator instance name.")]
        public string Name { get; set; }

        [ShowInTelemetry]
        [Option('l', "location", Required = true, HelpText = "Aggregator instance location (Azure region).")]
        public string Location { get; set; }

        [ShowInTelemetry(TelemetryDisplayMode.Presence)]
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [ShowInTelemetry]
        [Option('m', "dont-remove-mappings", Required = false, HelpText = "Do not remove mappings from Azure DevOps (default is to remove them).")]
        public bool Mappings { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);

            var instance = context.Naming.Instance(Name, ResourceGroup);

            if (!Mappings)
            {
                var mappings = new AggregatorMappings(context.Devops, context.Azure, context.Logger, context.Naming);
                _ = await mappings.RemoveInstanceAsync(instance);
            }

            var instances = new AggregatorInstances(context.Azure, context.Logger, context.Naming);
            var ok = await instances.RemoveAsync(instance, Location);
            return ok ? ExitCodes.Success : ExitCodes.Failure;
        }
    }
}

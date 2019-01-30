using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("test", HelpText = "It does something.", Hidden = true)]
    class TestCommand : CommandBase
    {
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .BuildAsync(cancellationToken);
            var instance = new InstanceName(Instance, ResourceGroup);
            var instances = new AggregatorInstances(context.Azure, context.Logger);
            bool ok = await instances.StreamLogsAsync(instance, cancellationToken);
            return ok ? 0 : 1;
        }
    }
}

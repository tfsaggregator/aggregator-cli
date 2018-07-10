using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("uninstall.instance", HelpText = "Destroy an Aggregator instance in Azure.")]
    class UninstallInstanceCommand : CommandBase
    {
        [Option('n', "name", Required = true, HelpText = "Aggregator instance name.")]
        public string Name { get; set; }

        [Option('l', "location", Required = true, HelpText = "Aggregator instance location (Azure region).")]
        public string Location { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .WithVstsLogon()
                .Build();

            var instance = new InstanceName(Name);

            var mappings = new AggregatorMappings(context.Vsts, context.Azure, context.Logger);
            bool ok = await mappings.RemoveInstanceAsync(instance);

            var instances = new AggregatorInstances(context.Azure, context.Logger);
            ok = await instances.Remove(instance, Location);
            return ok ? 0 : 1;
        }
    }
}

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("install.instance", HelpText = "Creates a new Aggregator instance in Azure.")]
    class InstallInstanceCommand : CommandBase
    {
        [Option('n', "name", Required = true, HelpText = "Aggregator instance name.")]
        public string Name { get; set; }

        [Option('l', "location", Required = true, HelpText = "Aggregator instance location (Azure region).")]
        public string Location { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }

        [Option("requiredVersion", Required = false, HelpText = "Version of Aggregator Runtime required.")]
        public string RequiredVersion { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon() // need the token, so we can save it in the app settings
                .Build();
            var instances = new AggregatorInstances(context.Azure, context.Logger);
            var instance = new InstanceName(Name, ResourceGroup);
            bool ok = await instances.Add(instance, Location, RequiredVersion);
            return ok ? 0 : 1;
        }
    }
}

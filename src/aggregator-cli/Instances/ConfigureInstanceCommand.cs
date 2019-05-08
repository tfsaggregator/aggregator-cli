using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("configure.instance", HelpText = "Configures an existing Aggregator instance.")]
    internal class ConfigureInstanceCommand : CommandBase
    {
        [Option('n', "name", Required = true, HelpText = "Aggregator instance name.")]
        public string Name { get; set; }

        [Option('l', "location", Required = true, HelpText = "Aggregator instance location (Azure region).")]
        public string Location { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('a', "authentication", SetName = "auth", Required = true, HelpText = "Refresh authentication data.")]
        public bool Authentication { get; set; }

        [Option('m', "saveMode", SetName = "save", Required = false, HelpText = "Save behaviour.")]
        public SaveMode SaveMode { get; set; }

        // support App Service Deployment Slots
        [Option('s', "slot", SetName = "slot", Required = true, HelpText = "Set slot deployment.")]
        public string Slot { get; set; }
        [Option('w', "swap", SetName = "slot", Required = true, HelpText = "Swap deploy slot.")]
        public bool Swap { get; set; }


        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon() // need the token, so we can save it in the app settings
                .BuildAsync(cancellationToken);
            var instances = new AggregatorInstances(context.Azure, context.Logger);
            var instance = new InstanceName(Name, ResourceGroup);
            bool ok = false;
            if (Authentication)
            {
                ok = await instances.ChangeAppSettingsAsync(instance, Location, SaveMode, cancellationToken);
            }
            else if (Swap)
            {
                ok = await instances.SwapToSlot(instance, Location, Slot, cancellationToken);
                //Swap;
            }
            else
            {
                context.Logger.WriteError($"Unsupported command option(s)");
            }

            return ok ? 0 : 1;
        }
    }
}
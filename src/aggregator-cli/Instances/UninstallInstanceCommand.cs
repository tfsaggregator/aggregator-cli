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
            var azure = await AzureLogon.Load()?.LogonAsync();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
                return 2;
            }

            var vsts = await VstsLogon.Load()?.LogonAsync();
            if (vsts == null)
            {
                WriteError($"Must logon.vsts first.");
                return 2;
            }

            var mappings = new AggregatorMappings(vsts, azure);
            bool ok = await mappings.RemoveInstanceAsync(Name);

            var instances = new AggregatorInstances(azure);
            instances.Progress += Instances_Progress;
            ok = await instances.Remove(Name, Location);
            return ok ? 0 : 1;
        }

        private void Instances_Progress(object sender, AggregatorInstances.ProgressEventArgs e)
        {
            WriteInfo(e.Message);
        }
    }
}

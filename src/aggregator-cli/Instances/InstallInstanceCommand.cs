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
        [Option('n', "name", HelpText = "Aggregator instance name.")]
        public string Name { get; set; }

        [Option('l', "location", HelpText = "Aggregator instance location (Azure region).")]
        public string Location { get; set; }

        internal override Task<int> RunAsync()
        {
            var azure = AzureLogon.Load()?.Logon();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
            }
            var instances = new AggregatorInstances(azure);
            instances.Progress += Instances_Progress;
            instances.Add(Name, Location);
            return Task.Run(() => 0);
        }

        private void Instances_Progress(object sender, AggregatorInstances.ProgressEventArgs e)
        {
            WriteInfo(e.Message);
        }
    }
}

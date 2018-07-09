using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("add.rule", HelpText = "Add a rule to existing Aggregator instance in Azure.")]
    class AddRuleCommand : CommandBase
    {
        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        [Option('f', "file", Required = true, HelpText = "Aggregator rule code.")]
        public string File { get; set; }

        internal override async Task<int> RunAsync()
        {
            var logon = await Logon<AzureLogon, bool>();
            var instance = new InstanceName(Instance);
            var rules = new AggregatorRules(logon.azure, this);
            bool ok = await rules.AddAsync(instance, Name, File);
            return ok ? 0 : 1;
        }
    }
}

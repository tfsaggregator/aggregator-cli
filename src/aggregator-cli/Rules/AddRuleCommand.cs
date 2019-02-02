using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("add.rule", HelpText = "Add a rule to existing Aggregator instance in Azure.")]
    class AddRuleCommand : CommandBase
    {
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        [Option('f', "file", Required = true, HelpText = "Aggregator rule code.")]
        public string File { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .BuildAsync(cancellationToken);
            var instance = new InstanceName(Instance, ResourceGroup);
            var rules = new AggregatorRules(context.Azure, context.Logger);
            bool ok = await rules.AddAsync(instance, Name, File, cancellationToken);
            return ok ? 0 : 1;
        }
    }
}

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("remove.rule", HelpText = "Remove a rule from existing Aggregator instance in Azure.")]
    class RemoveRuleCommand : CommandBase
    {
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var mappings = new AggregatorMappings(context.Devops, context.Azure, context.Logger, context.Naming);
            bool ok = await mappings.RemoveRuleAsync(instance, Name);

            var rules = new AggregatorRules(context.Azure, context.Logger);
            //rules.Progress += Instances_Progress;
            ok = ok && await rules.RemoveAsync(instance, Name, cancellationToken);
            return ok ? 0 : 1;
        }
    }
}

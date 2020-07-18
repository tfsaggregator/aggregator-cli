using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("list.rules", HelpText = "List the rule in existing Aggregator instance in Azure.")]
    class ListRulesCommand : CommandBase
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
            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var rules = new AggregatorRules(context.Azure, context.Logger);
            bool any = false;
            foreach (var ruleInformation in await rules.ListAsync(instance, cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                context.Logger.WriteOutput(ruleInformation);
                any = true;
            }

            if (!any)
            {
                context.Logger.WriteInfo($"No rules found in aggregator instance {instance.PlainName}.");
                return ExitCodes.NotFound;
            }
            else
            {
                return ExitCodes.Success;
            }
        }
    }
}

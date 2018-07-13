using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("list.rules", HelpText = "List the rule in existing Aggregator instance in Azure.")]
    class ListRulesCommand : CommandBase
    {
        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .Build();
            var instance = new InstanceName(Instance);
            var rules = new AggregatorRules(context.Azure, context.Logger);
            bool any = false;
            foreach (var item in await rules.ListAsync(instance))
            {
                context.Logger.WriteOutput(
                    item,
                    (data) => $"Rule {instance.PlainName}/{item.Name} {(item.Config.Disabled ? "(disabled)" : string.Empty)}");
                any = true;
            }
            if (!any)
            {
                context.Logger.WriteInfo($"No rules found in aggregator instance {instance.PlainName}.");
            }
            return 0;
        }
    }
}

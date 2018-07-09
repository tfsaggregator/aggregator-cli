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
            var azure = await AzureLogon.Load()?.LogonAsync();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
                return 2;
            }
            var instance = new InstanceName(Instance);
            var rules = new AggregatorRules(azure, this);
            bool any = false;
            foreach (var item in await rules.List(instance))
            {
                WriteOutput(
                    item,
                    (data) => $"Rule {item.Name} {(item.Config.Disabled ? "(disabled)" : string.Empty)}");
                any = true;
            }
            if (!any)
            {
                WriteInfo($"No rules found in aggregator instance {instance.PlainName}.");
            }
            return 0;
        }
    }
}

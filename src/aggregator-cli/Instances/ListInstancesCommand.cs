using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("list.instances", HelpText = "Lists Aggregator instances.")]
    class ListInstancesCommand : CommandBase
    {
        internal override async Task<int> RunAsync()
        {
            var logon = await Logon<AzureLogon, bool>();
            var instances = new AggregatorInstances(logon.azure, this);
            var found = await instances.ListAsync();
            bool any = false;
            foreach (var item in found)
            {
                WriteOutput(
                    item,
                    (data) => $"Instance {item.name} in {item.region}");
                any = true;
            }
            if (!any)
            {
                WriteInfo("No aggregator instances found.");
            }
            return 0;
        }
    }
}

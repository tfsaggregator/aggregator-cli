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
            var context = await Context
                .WithAzureLogon()
                .Build();
            var instances = new AggregatorInstances(context.Azure, context.Logger);
            var found = await instances.ListAsync();
            bool any = false;
            foreach (var item in found)
            {
                context.Logger.WriteOutput(
                    item,
                    (data) => $"Instance {item.name} in {item.region} region");
                any = true;
            }
            if (!any)
            {
                context.Logger.WriteInfo("No aggregator instances found.");
            }
            return 0;
        }
    }
}

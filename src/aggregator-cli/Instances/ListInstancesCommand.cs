using CommandLine;
using System;
using System.Collections.Generic;

namespace aggregator.cli
{
    [Verb("list.instances", HelpText = "Lists Aggregator instances.")]
    class ListInstancesCommand : CommandBase
    {
        internal override int Run()
        {
            var azure = AzureLogon.Load()?.Logon();
            if (azure == null)
            {
                WriteError($"Must logon.azure first.");
            }
            var instances = new AggregatorInstances(azure);
            bool any = false;
            foreach (var item in instances.List())
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

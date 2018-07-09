﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("install.instance", HelpText = "Creates a new Aggregator instance in Azure.")]
    class InstallInstanceCommand : CommandBase
    {
        [Option('n', "name", Required = true, HelpText = "Aggregator instance name.")]
        public string Name { get; set; }

        [Option('l', "location", Required = true, HelpText = "Aggregator instance location (Azure region).")]
        public string Location { get; set; }

        internal override async Task<int> RunAsync()
        {
            var logon = await Logon<AzureLogon, bool>();
            var instance = new InstanceName(Name);
            var instances = new AggregatorInstances(logon.azure, this);
            bool ok = await instances.Add(instance, Location);
            return ok ? 0 : 1;
        }
    }
}

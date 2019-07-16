﻿using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }

        [Option("requiredVersion", SetName = "nourl", Required = false, HelpText = "Version of Aggregator Runtime required.")]
        public string RequiredVersion { get; set; }

        [Option("sourceUrl", SetName="url", Required = false, HelpText = "URL of Aggregator Runtime.")]
        public string SourceUrl { get; set; }

        /* next two should go together, no way to express this via CommandLine library */

        [Option('k', "hostingPlanSku", SetName = "plan", Required = false, Default = "Y1", HelpText = "Azure AppPlan SKU hosting the Aggregator instances .")]
        public string HostingPlanSku { get; set; }

        [Option('t', "hostingPlanTier", SetName = "plan", Required = false, Default = "Dynamic", HelpText = "Azure AppPlan Service tier hosting the Aggregator instances .")]
        public string HostingPlanTier { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var validHostingPlanSkus = new string[] { "Y1", "F1", "D1", "B1", "S1", "S2", "S3", "P1", "P2", "P3", "P1V2", "P2V2", "P3V2" };
            var validHostingPlanTiers = new string[] { "Dynamic", "Free", "Shared", "Basic", "Standard", "Premium" };
            if (!validHostingPlanSkus.Contains(HostingPlanSku))
            {
                Logger.WriteError($"Invalid value for hostingPlanSku: must be one of {String.Join(",", validHostingPlanSkus)}");
                return 2;
            }
            if (!validHostingPlanTiers.Contains(HostingPlanTier))
            {
                Logger.WriteError($"Invalid value for hostingPlanTier: must be one of {String.Join(",", validHostingPlanTiers)}");
                return 2;
            }

            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon() // need the token, so we can save it in the app settings
                .BuildAsync(cancellationToken);
            var instances = new AggregatorInstances(context.Azure, context.Logger);
            var instance = new InstanceName(Name, ResourceGroup);
            bool ok = await instances.AddAsync(instance, Location, RequiredVersion, SourceUrl, cancellationToken);
            return ok ? 0 : 1;
        }
    }
}

﻿using System;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;

namespace aggregator.cli
{
    internal class FileNamingTemplates : INamingTemplates
    {
        readonly NamingAffixes affixes;

        internal FileNamingTemplates(string jsonData)
        {
            affixes = JsonConvert.DeserializeObject<NamingAffixes>(jsonData);
        }

        private sealed class InstanceCreateNamesImpl : InstanceCreateNames
        {
            internal InstanceCreateNamesImpl(string name, string resourceGroup, bool isCustom, string functionAppName, NamingAffixes affixes)
                : base(name, resourceGroup, isCustom, functionAppName)
            {
                HostingPlanName = $"{affixes.HostingPlanPrefix}{name}{affixes.HostingPlanSuffix}";
                AppInsightName = $"{affixes.AppInsightPrefix}{name}{affixes.AppInsightSuffix}";
                StorageAccountName = $"{affixes.StorageAccountPrefix}{name}{affixes.StorageAccountSuffix}";
            }
        }

        public InstanceName Instance(string name, string resourceGroup)
        {
            // validate
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Instance name cannot be empty");

            return new InstanceCreateNamesImpl(
                name: name,
                resourceGroup: $"{affixes.ResourceGroupPrefix}{resourceGroup}{affixes.ResourceGroupSuffix}",
                isCustom: true,
                functionAppName: $"{affixes.FunctionAppPrefix}{name}{affixes.FunctionAppSuffix}",
                affixes);
        }

        public InstanceCreateNames GetInstanceCreateNames(string name, string resourceGroup)
        {
            return Instance(name, resourceGroup) as InstanceCreateNames;
        }

        // used only in ListInstances
        public bool ResourceGroupMatches(IResourceGroup rg)
            => !string.IsNullOrWhiteSpace(affixes.ResourceGroupPrefix)
               ? rg.Name.StartsWith(affixes.ResourceGroupPrefix)
               : rg.Name.EndsWith(affixes.ResourceGroupSuffix);

        protected static string StripAffixes(string pfx, string stuffedName, string sfx)
        {
            pfx ??= "";
            sfx ??= "";
            string name = stuffedName.Remove(0, pfx.Length);
            name = name.Remove(name.Length - sfx.Length, sfx.Length);
            return name;
        }

        // used only in ListInstances
        public InstanceName FromResourceGroupName(string rgName)
        {
            // validate
            if (string.IsNullOrWhiteSpace(rgName))
                throw new ArgumentException("Resource Group name cannot be empty");

            return Instance(StripAffixes(affixes.ResourceGroupPrefix, rgName, affixes.ResourceGroupSuffix), null);
        }

        // used only in ListInstances
        public InstanceName FromFunctionAppName(string appName, string resourceGroup)
        {
            // validate
            if (string.IsNullOrWhiteSpace(appName))
                throw new ArgumentException("FunctionApp name cannot be empty");

            return Instance(StripAffixes(affixes.FunctionAppPrefix, appName, affixes.FunctionAppSuffix), resourceGroup);
        }

        // used only in mappings.ListAsync
        public InstanceName FromFunctionAppUrl(Uri url)
        {
            string host = url.Host;
            host = host[..host.IndexOf('.')];
            return Instance(StripAffixes(affixes.FunctionAppPrefix, host, affixes.FunctionAppSuffix), null);
        }

        public string GetResourceGroupName(string resourceGroup)
        {
            return $"{affixes.ResourceGroupPrefix}{resourceGroup}{affixes.ResourceGroupSuffix}";
        }
    }
}

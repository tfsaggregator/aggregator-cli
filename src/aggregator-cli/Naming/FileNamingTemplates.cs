using Microsoft.Azure.Management.ResourceManager.Fluent;
using Newtonsoft.Json;
using System;

namespace aggregator.cli
{
    internal class FileNamingTemplates : INamingTemplates
    {
        NamingAffixes affixes;

        internal FileNamingTemplates(string jsonData)
        {
            affixes = JsonConvert.DeserializeObject<NamingAffixes>(jsonData);
        }

        private class InstanceName_ : InstanceCreateNames
        {
            internal InstanceName_(string name, string resourceGroup, bool isCustom, string functionAppName, NamingAffixes affixes)
                : base(name, resourceGroup, isCustom, functionAppName) {
                HostingPlanName = $"{affixes.HostingPlanPrefix}{name}{affixes.HostingPlanSuffix}";
                AppInsightName = $"{affixes.AppInsightPrefix}{name}{affixes.AppInsightSuffix}";
                StorageAccountName = $"{affixes.StorageAccountPrefix}{name}{affixes.StorageAccountSuffix}";
            }
        }

        public InstanceName Instance(string name, string resourceGroup)
        {
            return new InstanceName_(
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
        public bool ResourceGroupMatches(IResourceGroup rg) => rg.Name.StartsWith(affixes.ResourceGroupPrefix);

        // used only in ListInstances
        public InstanceName FromResourceGroupName(string rgName)
        {
            // TODO TEST!
            string name = rgName
                .Remove(0, affixes.ResourceGroupPrefix.Length)
                .Remove(rgName.Length - affixes.ResourceGroupSuffix.Length, affixes.ResourceGroupSuffix.Length);
            return Instance(name, null);
        }

        // used only in ListInstances
        public InstanceName FromFunctionAppName(string appName, string resourceGroup)
        {
            return Instance(appName.Remove(appName.Length - affixes.FunctionAppSuffix.Length), resourceGroup);
        }

        // used only in mappings.ListAsync
        public InstanceName FromFunctionAppUrl(Uri url)
        {
            string host = url.Host;
            host = host.Substring(0, host.IndexOf('.'));
            string name = host
                .Remove(0, affixes.FunctionAppPrefix.Length)
                .Remove(host.Length - affixes.FunctionAppSuffix.Length, affixes.FunctionAppSuffix.Length);
            return Instance(name, null);
        }
    }
}

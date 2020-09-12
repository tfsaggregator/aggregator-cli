using System;
using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace aggregator.cli
{
    internal class BuiltInNamingTemplates : INamingTemplates
    {
        static readonly NamingAffixes affixes = new NamingAffixes
        {
            ResourceGroupPrefix = "aggregator-",
            ResourceGroupSuffix = "",
            FunctionAppPrefix = "",
            FunctionAppSuffix = "aggregator",
        };

        private static string GetRandomString(int size, string allowedChars = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
            var randomGen = new Random((int)DateTime.Now.Ticks);
            return new string(
                Enumerable.Range(0, size)
                .Select(x => allowedChars[randomGen.Next(0, allowedChars.Length)])
                .ToArray()
                );
        }

        private class InstanceCreateNamesImpl : InstanceCreateNames
        {
            // keep unused parameter  for uniformity
#pragma warning disable S1172,IDE0060 // Unused method parameters should be removed
            internal InstanceCreateNamesImpl(string name, string resourceGroup, bool isCustom, string functionAppName, NamingAffixes affixes)
#pragma warning restore S1172,IDE0060 // Unused method parameters should be removed
                : base(name, resourceGroup, isCustom, functionAppName)
            {
                HostingPlanName = $"{functionAppName}-plan";
                AppInsightName = $"{functionAppName}-ai";
                StorageAccountName = $"aggregator{GetRandomString(8)}";
            }
        }

        public InstanceName Instance(string name, string resourceGroup)
        {
            return new InstanceCreateNamesImpl(
                name: name,
                resourceGroup: string.IsNullOrEmpty(resourceGroup)
                                    ? affixes.ResourceGroupPrefix + name
                                    : resourceGroup,
                isCustom: !string.IsNullOrEmpty(resourceGroup),
                functionAppName: name + affixes.FunctionAppSuffix,
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
            return Instance(rgName.Remove(0, affixes.ResourceGroupPrefix.Length), null);
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
            return Instance(host.Remove(host.Length - affixes.FunctionAppSuffix.Length), null);
        }

        public string GetResourceGroupName(string resourceGroup)
        {
            return resourceGroup;
        }
    }
}

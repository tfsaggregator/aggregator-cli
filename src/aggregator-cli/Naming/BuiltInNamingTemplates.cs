using Microsoft.Azure.Management.ResourceManager.Fluent;
using System;
using System.Linq;

namespace aggregator.cli
{
    internal class BuiltInNamingTemplates : INamingTemplates
    {
        static NamingAffixes affixes = new NamingAffixes
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

        private class InstanceName_ : InstanceCreateNames
        {
            internal InstanceName_(string name, string resourceGroup, bool isCustom, string functionAppName, NamingAffixes affixes)
                : base(name, resourceGroup, isCustom, functionAppName)
            {
                HostingPlanName = $"{functionAppName}-plan";
                AppInsightName = $"{functionAppName}-ai";
                StorageAccountName = $"aggregator{GetRandomString(8)}";
            }
        }

        public InstanceName Instance(string name, string resourceGroup)
        {
            return new InstanceName_(
                name:               name,
                resourceGroup:      string.IsNullOrEmpty(resourceGroup)
                                    ? affixes.ResourceGroupPrefix + name
                                    : resourceGroup,
                isCustom:           !string.IsNullOrEmpty(resourceGroup),
                functionAppName:    name + affixes.FunctionAppSuffix,
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

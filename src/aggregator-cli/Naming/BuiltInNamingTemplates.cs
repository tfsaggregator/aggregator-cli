using System;
using System.Linq;
using Microsoft.Azure.Management.ResourceManager.Fluent;

namespace aggregator.cli
{
    internal class BuiltInNamingTemplates : INamingTemplates
    {
        static readonly NamingAffixes affixes = new()
        {
            ResourceGroupPrefix = "aggregator-",
            ResourceGroupSuffix = "",
            FunctionAppPrefix = "",
            FunctionAppSuffix = "aggregator",
        };

        private static int PseudoHash(string s, int limit)
        {
            long total = 0;
            var c = s.ToCharArray();

            // Horner's rule for generating a polynomial 
            // of 11 using ASCII values of the characters
            for (int k = 0; k < c.Length; k++)
                total += 11 * total + (int)c[k];

            total = total % limit;

            if (total < 0)
                total += limit;

            return (int)total;
        }

        private static string GetRandomString(string input, int size, string allowedChars = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
#pragma warning disable S2245 // Make sure that using this pseudorandom number generator is safe here
            var randomGen = new Random(PseudoHash(input, allowedChars.Length));
            return new string(
                Enumerable.Range(0, size)
                .Select(x => allowedChars[randomGen.Next(0, allowedChars.Length)])
                .ToArray()
                );
#pragma warning restore S2245 // Make sure that using this pseudorandom number generator is safe here
        }

        private sealed class InstanceCreateNamesImpl : InstanceCreateNames
        {
            // keep unused parameter  for uniformity
#pragma warning disable S1172,IDE0060 // Unused method parameters should be removed
            internal InstanceCreateNamesImpl(string name, string resourceGroup, bool isCustom, string functionAppName, NamingAffixes affixes)
#pragma warning restore S1172,IDE0060 // Unused method parameters should be removed
                : base(name, resourceGroup, isCustom, functionAppName)
            {
                HostingPlanName = $"{functionAppName}-plan";
                AppInsightName = $"{functionAppName}-ai";
                StorageAccountName = $"aggregator{GetRandomString(functionAppName, 8)}";
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
            host = host[..host.IndexOf('.')];
            return Instance(host.Remove(host.Length - affixes.FunctionAppSuffix.Length), null);
        }

        public string GetResourceGroupName(string resourceGroup)
        {
            return resourceGroup;
        }
    }
}

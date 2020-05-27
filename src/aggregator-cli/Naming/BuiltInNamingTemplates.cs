using System;

namespace aggregator.cli
{
    internal class BuiltInNamingTemplates : INamingTemplates
    {
        const string resourceGroupPrefix = "aggregator-";
        const string functionAppSuffix = "aggregator";

        private class InstanceName_ : InstanceName {
            internal InstanceName_(string name, string resourceGroup)
                : base(name, resourceGroup, resourceGroupPrefix, functionAppSuffix) {}
        }

        // used only in ListInstances
        public string ResourceGroupInstancePrefix => resourceGroupPrefix;

        public InstanceName Instance(string name, string resourceGroup)
        {
            return new InstanceName_(name, resourceGroup);
        }

        // used only in ListInstances
        public InstanceName FromResourceGroupName(string rgName)
        {
            return new InstanceName_(rgName.Remove(0, ResourceGroupInstancePrefix.Length), null);
        }

        // used only in ListInstances
        public InstanceName FromFunctionAppName(string appName, string resourceGroup)
        {
            return new InstanceName_(appName.Remove(appName.Length - functionAppSuffix.Length), resourceGroup);
        }

        // used only in mappings.ListAsync
        public InstanceName FromFunctionAppUrl(Uri url)
        {
            string host = url.Host;
            host = host.Substring(0, host.IndexOf('.'));
            return new InstanceName_(host.Remove(host.Length - functionAppSuffix.Length), null);
        }
    }
}
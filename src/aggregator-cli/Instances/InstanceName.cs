using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    internal class InstanceName
    {
        const string resourceGroupPrefix = "aggregator-";
        const string functionAppSuffix = "aggregator";
        private readonly string name;
        private readonly string resourceGroup;

        // used only in ListInstances
        public static string ResourceGroupInstancePrefix => resourceGroupPrefix;

        public InstanceName(string name, string resourceGroup)
        {
            this.name = name;
            this.resourceGroup = string.IsNullOrEmpty(resourceGroup)
                ? resourceGroupPrefix + name
                : resourceGroup;
        }

        // used only in ListInstances
        public static InstanceName FromResourceGroupName(string rgName)
        {
            return new InstanceName(rgName.Remove(0, ResourceGroupInstancePrefix.Length), null);
        }

        // used only in ListInstances
        public static InstanceName FromFunctionAppName(string appName)
        {
            return new InstanceName(appName.Remove(appName.Length - functionAppSuffix.Length), null);
        }

        public string PlainName => name;

        internal string ResourceGroupName => resourceGroup;

        internal string FunctionAppName=> name + functionAppSuffix;

        internal string DnsHostName => $"{FunctionAppName}.azurewebsites.net";

        internal string FunctionAppUrl => $"https://{DnsHostName}";

        internal string KuduUrl => $"https://{FunctionAppName}.scm.azurewebsites.net";
    }
}

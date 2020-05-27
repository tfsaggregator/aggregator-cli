using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    internal class InstanceName
    {
        private readonly string resourceGroupPrefix;
        private readonly string functionAppSuffix;
        private readonly string name;
        private readonly string resourceGroup;

        protected InstanceName(string name, string resourceGroup, string resourceGroupPrefix, string functionAppSuffix)
        {
            this.resourceGroupPrefix = resourceGroupPrefix;
            this.functionAppSuffix = functionAppSuffix;
            this.name = name;
            this.resourceGroup = string.IsNullOrEmpty(resourceGroup)
                ? resourceGroupPrefix + name
                : resourceGroup;
        }

        // display to user
        public string PlainName => name;

        // name of Azure Resource Group
        internal string ResourceGroupName => resourceGroup;

        internal bool IsCustom => resourceGroup != resourceGroupPrefix + name;

        // name of Azure App Service
        internal string FunctionAppName=> name + functionAppSuffix;

        internal string DnsHostName => $"{FunctionAppName}.azurewebsites.net";

        internal string FunctionAppUrl => $"https://{DnsHostName}";

        internal string KuduUrl => $"https://{FunctionAppName}.scm.azurewebsites.net";
    }
}

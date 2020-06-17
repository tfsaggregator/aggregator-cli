using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    internal class InstanceName
    {
        private readonly string name;
        private readonly string resourceGroup;
        private readonly bool isCustom;
        private readonly string functionAppName;

        protected InstanceName(string name, string resourceGroup, bool isCustom, string functionAppName)
        {
            this.name = name;
            this.resourceGroup = resourceGroup;
            this.isCustom = isCustom;
            this.functionAppName = functionAppName;
        }

        // display to user
        internal string PlainName => name;

        // name of Azure Resource Group
        internal string ResourceGroupName => resourceGroup;

        internal bool IsCustom => isCustom;

        // name of Azure App Service
        internal string FunctionAppName=> functionAppName;

        internal string DnsHostName => $"{FunctionAppName}.azurewebsites.net";

        internal string FunctionAppUrl => $"https://{DnsHostName}";

        internal string KuduUrl => $"https://{FunctionAppName}.scm.azurewebsites.net";
    }
}

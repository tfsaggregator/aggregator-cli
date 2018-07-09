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

        public static string ResourceGroupInstancePrefix => resourceGroupPrefix;

        internal InstanceName(string name)
        {
            this.name = name;
        }

        public string PlainName => name;

        internal string ResourceGroupName => resourceGroupPrefix + name;

        internal string FunctionAppName=> name + functionAppSuffix;

        internal string DnsHostName => $"{FunctionAppName}.azurewebsites.net";

        internal string FunctionAppUrl => $"https://{DnsHostName}";

        internal string KuduUrl => $"https://{FunctionAppName}.scm.azurewebsites.net";
    }
}

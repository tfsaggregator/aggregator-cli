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

        public static InstanceName FromResourceGroupName(string rgName)
        {
            return new InstanceName(rgName.Remove(0, ResourceGroupInstancePrefix.Length));
        }

        public static InstanceName FromFunctionAppUrl(string url)
        {
            string host = new Uri(url).Host;
            host = host.Substring(0, host.IndexOf('.'));
            return new InstanceName(host.Remove(host.Length - functionAppSuffix.Length));
        }

        public string PlainName => name;

        internal string ResourceGroupName => resourceGroupPrefix + name;

        internal string FunctionAppName=> name + functionAppSuffix;

        internal string DnsHostName => $"{FunctionAppName}.azurewebsites.net";

        internal string FunctionAppUrl => $"https://{DnsHostName}";

        internal string KuduUrl => $"https://{FunctionAppName}.scm.azurewebsites.net";
    }
}

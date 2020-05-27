using System;

namespace aggregator.cli
{
    internal interface INamingTemplates
    {
        InstanceName Instance(string name, string resourceGroup);

        // used only in ListInstances
        string ResourceGroupInstancePrefix { get; }

        // used only in ListInstances
        InstanceName FromResourceGroupName(string rgName);

        // used only in ListInstances
        InstanceName FromFunctionAppName(string appName, string resourceGroup);

        // used only in mappings.ListAsync
        InstanceName FromFunctionAppUrl(Uri url);
    }
}
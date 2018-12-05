using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    internal class InstanceOutputData : ILogDataObject
    {
        string name;
        string region;

        internal InstanceOutputData(string name, string region)
        {
            this.name = name;
            this.region = region;
        }

        public string AsHumanReadable()
        {
            return $"Instance {name} on {region}";
        }
    }
}

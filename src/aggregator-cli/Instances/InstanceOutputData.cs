﻿using Semver;

namespace aggregator.cli
{
    internal class InstanceOutputData : ILogDataObject
    {
        string name;
        string region;
        SemVersion version;

        internal InstanceOutputData(string name, string region, SemVersion version)
        {
            this.name = name;
            this.region = region;
            this.version = version;
        }

        public string AsHumanReadable()
        {
            return $"Instance {name} in {region} using runtime v{version}";
        }
    }
}

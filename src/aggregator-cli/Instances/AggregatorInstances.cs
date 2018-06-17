using Microsoft.Azure.Management.Fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aggregator.cli
{
    class AggregatorInstances
    {
        const string InstancePrefix = "aggregator-";
        private readonly IAzure azure;

        public AggregatorInstances(IAzure azure)
        {
            this.azure = azure;
        }

        public IEnumerable<(string name, string region)> List()
        {
            var rgs = azure.ResourceGroups.List().Where(rg => rg.Name.StartsWith(InstancePrefix));
            foreach (var rg in rgs)
            {
                yield return (
                    rg.Name.Remove(0, InstancePrefix.Length),
                    rg.RegionName
                );
            }
        }
    }
}

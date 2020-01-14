using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;


namespace aggregator.Model
{
    /// <summary>
    /// This class tracks the configuration data that CLI writes and Function runtime reads
    /// </summary>
    [DebuggerDisplay("SaveMode={SaveMode,nq}, DryRun={DryRun}")]
    internal class AggregatorConfiguration : IAggregatorConfiguration
    {
        public AggregatorConfiguration()
        {
            RulesConfiguration = new ConcurrentDictionary<string, IRuleConfiguration>(StringComparer.OrdinalIgnoreCase);
        }

        public DevOpsTokenType DevOpsTokenType { get; set; }
        public string DevOpsToken { get; set; }
        public SaveMode SaveMode { get; set; }
        public bool DryRun { get; set; }

        public IDictionary<string, IRuleConfiguration> RulesConfiguration { get; }
    }
}
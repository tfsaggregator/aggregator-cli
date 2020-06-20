using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    ///<summary>
    /// Holds any configuration data picked by the directive parser that may influence a rule behaviour.
    ///</summary>
    internal class RuleSettings : IRuleSettings
    {
        internal RuleSettings()
        {
            EnableRevisionCheck = true; // default to existing behaviour
        }

        public bool EnableRevisionCheck { get; set; }
    }
}

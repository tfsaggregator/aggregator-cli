using System.Collections.Generic;
using System.Collections.ObjectModel;


namespace aggregator.Engine.Language {
    internal class RuleDirectives : IRuleDirectives
    {
        public RuleDirectives()
        {
            Impersonate = false;
            Language = RuleLanguage.Unknown;
            References = new List<string>();
            Imports = new List<string>();
            RuleCodeOffset = 0;
            RuleCode = new List<string>();
        }

        public bool Impersonate { get; set; }

        public RuleLanguage Language { get; internal set; }

        IReadOnlyList<string> IRuleDirectives.References => new ReadOnlyCollection<string>(References);

        IReadOnlyList<string> IRuleDirectives.Imports => new ReadOnlyCollection<string>(Imports);

        IReadOnlyList<string> IRuleDirectives.RuleCode => new ReadOnlyCollection<string>(RuleCode);


        public IList<string> References { get; }

        public IList<string> Imports { get; }

        public int RuleCodeOffset { get; set; }

        public IList<string> RuleCode { get; }
    }
}
using System.Collections.Generic;


namespace aggregator.Engine.Language {
    public interface IRuleDirectives
    {
        bool Impersonate { get; }
        RuleLanguage Language { get; }
        IReadOnlyList<string> References { get; }
        IReadOnlyList<string> Imports { get; }
        IReadOnlyList<string> RuleCode { get; }
    }
}
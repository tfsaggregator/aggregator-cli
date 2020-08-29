using System.Collections.Generic;


namespace aggregator.Engine.Language
{
    public interface IPreprocessedRule
    {
        bool Impersonate { get; set; }
        IRuleSettings Settings { get; }
        RuleLanguage Language { get; }
        IReadOnlyList<string> References { get; }
        IReadOnlyList<string> Imports { get; }
        int FirstCodeLine { get; }
        IReadOnlyList<string> RuleCode { get; }
    }
}

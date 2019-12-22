using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace aggregator.Engine.Language {
    public static class RuleDirectivesExtensions
    {
        internal static string GetRuleCode(this IPreprocessedRule preprocessedRule)
        {
            return string.Join(Environment.NewLine, preprocessedRule.RuleCode);
        }

        internal static bool IsValid(this IPreprocessedRule preprocessedRule)
        {
            return preprocessedRule.IsSupportedLanguage() && preprocessedRule.RuleCode.Any();
        }

        internal static bool IsCSharp(this IPreprocessedRule preprocessedRule)
        {
            return preprocessedRule.Language == RuleLanguage.Csharp;
        }

        internal static bool IsSupportedLanguage(this IPreprocessedRule preprocessedRule)
        {
            return preprocessedRule.Language != RuleLanguage.Unknown;
        }


        public static string LanguageAsString(this IPreprocessedRule preprocessedRule)
        {
            switch (preprocessedRule.Language)
            {
                case RuleLanguage.Csharp:
                    return "C#";
                case RuleLanguage.Unknown:
                    return "Unknown";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static IEnumerable<Assembly> LoadAssemblyReferences(this IPreprocessedRule preprocessedRule)
        {
            return preprocessedRule.References
                                 .Select(reference => new AssemblyName(reference))
                                 .Select(Assembly.Load);
        }
    }
}
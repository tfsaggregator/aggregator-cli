using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace aggregator.Engine.Language
{
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
            return preprocessedRule.Language switch
            {
                RuleLanguage.Csharp => "C#",
                RuleLanguage.Unknown => "Unknown",
#pragma warning disable S3928 // The parameter name {0} is not declared in the argument list
                _ => throw new ArgumentOutOfRangeException($"BUG: {nameof(LanguageAsString)} is not a valid {nameof(RuleLanguage)}"),
#pragma warning restore S3928
            };
        }

        internal static IEnumerable<Assembly> LoadAssemblyReferences(this IPreprocessedRule preprocessedRule)
        {
            return preprocessedRule.References
                                 .Select(reference => new AssemblyName(reference))
                                 .Select(Assembly.Load);
        }
    }
}

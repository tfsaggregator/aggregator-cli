using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;


namespace aggregator.Engine.Language {
    public static class RuleDirectivesExtensions
    {
        internal static string GetRuleCode(this IRuleDirectives ruleDirectives)
        {
            return string.Join(Environment.NewLine, ruleDirectives.RuleCode);
        }

        internal static bool IsValid(this IRuleDirectives ruleDirectives)
        {
            return ruleDirectives.IsSupportedLanguage() && ruleDirectives.RuleCode.Any();
        }

        internal static bool IsCSharp(this IRuleDirectives ruleDirectives)
        {
            return ruleDirectives.Language == RuleLanguage.Csharp;
        }

        internal static bool IsSupportedLanguage(this IRuleDirectives ruleDirectives)
        {
            return ruleDirectives.Language != RuleLanguage.Unknown;
        }


        public static string LanguageAsString(this IRuleDirectives ruleDirectives)
        {
            switch (ruleDirectives.Language)
            {
                case RuleLanguage.Csharp:
                    return "C#";
                case RuleLanguage.Unknown:
                    return "Unknown";
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static IEnumerable<Assembly> LoadAssemblyReferences(this IRuleDirectives ruleDirectives)
        {
            return ruleDirectives.References
                                 .Select(reference => new AssemblyName(reference))
                                 .Select(Assembly.Load);
        }
    }
}
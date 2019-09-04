using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Microsoft.VisualStudio.Services.Common;


namespace aggregator.Engine.Language
{
    /// <summary>
    /// Scan the top lines of a script looking for directives, whose lines start with a dot
    /// </summary>
    public static class RuleFileParser
    {
        public static (IRuleDirectives ruleDirectives, bool result, string[] messages) ReadFile(string ruleFilePath)
        {
            var content = File.ReadAllLines(ruleFilePath);
            return Read(content);
        }

        /// <summary>
        /// Grab directives
        /// </summary>
        internal static (IRuleDirectives ruleDirectives, bool parseSuccess, string[] messages) Read(string[] ruleCode)
        {
            var messages = new List<string>();
            var directiveLineIndex = 0;
            var ruleDirectives = new RuleDirectives()
                                 {
                                     Language = RuleLanguage.Csharp
                                 };

            while (directiveLineIndex < ruleCode.Length
                && ruleCode[directiveLineIndex].Length > 0
                && ruleCode[directiveLineIndex][0] == '.')
            {
                string directive = ruleCode[directiveLineIndex].Substring(1);
                var parts = directive.Split('=');

                switch (parts[0].ToLowerInvariant())
                {
                    case "lang":
                    case "language":
                        if (parts.Length < 2)
                        {
                            messages.Add($"Invalid language directive {directive}");
                            ruleDirectives.Language = RuleLanguage.Unknown;
                        }
                        else
                        {
                            switch (parts[1].ToUpperInvariant())
                            {
                                case "C#":
                                case "CS":
                                case "CSHARP":
                                    ruleDirectives.Language = RuleLanguage.Csharp;
                                    break;
                                default:
                                {
                                    messages.Add($"Unrecognized language {parts[1]}");
                                    ruleDirectives.Language = RuleLanguage.Unknown;
                                    break;
                                }
                            }
                        }
                        break;

                    case "r":
                    case "ref":
                    case "reference":
                        if (parts.Length < 2)
                        {
                            messages.Add($"Invalid reference directive {directive}");
                        }
                        else
                        {
                            ruleDirectives.References.Add(parts[1]);
                        }
                        break;

                    case "import":
                    case "imports":
                    case "namespace":
                        if (parts.Length < 2)
                        {
                            messages.Add($"Invalid import directive {directive}");
                        }
                        else
                        {
                            ruleDirectives.Imports.Add(parts[1]);
                        }
                        break;

                    case "impersonate":
                        if (parts.Length < 2)
                        {
                            messages.Add($"Invalid impersonate directive {directive}");
                        }
                        else
                        {
                            ruleDirectives.Impersonate = string.Equals("onBehalfOfInitiator", parts[1].TrimEnd(), StringComparison.OrdinalIgnoreCase);
                        }
                        break;

                    default:
                    {
                        messages.Add($"Unrecognized directive {directive}");
                        break;
                    }
                }//switch

                directiveLineIndex++;
            }//while

            ruleDirectives.RuleCode.AddRange(ruleCode.Skip(directiveLineIndex));
            return (ruleDirectives, messages.Count == 0, messages.ToArray());
        }

        public static void WriteFile(string ruleFilePath, IRuleDirectives ruleDirectives)
        {
            var content = Write(ruleDirectives);

            File.WriteAllLines(ruleFilePath, content);
        }

        public static string[] Write(IRuleDirectives ruleDirectives)
        {
            var content = new List<string>
                          {
                              $".language={ruleDirectives.LanguageAsString()}"
                          };

            if (ruleDirectives.Impersonate)
            {
                content.Add($".impersonate=onBehalfOfInitiator");
            }

            content.AddRange(ruleDirectives.References.Select(reference => $".reference={reference}"));
            content.AddRange(ruleDirectives.Imports.Select(import => $".import={import}"));
            content.AddRange(ruleDirectives.RuleCode);

            return content.ToArray();
        }
    }
}

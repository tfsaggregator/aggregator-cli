using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Common;


namespace aggregator.Engine.Language
{
    /// <summary>
    /// Scan the top lines of a script looking for directives, whose lines start with a dot
    /// </summary>
    public class RuleFileParser
    {
        public static async Task<(IPreprocessedRule preprocessedRule, bool result)> ReadFile(string ruleFilePath, CancellationToken cancellationToken = default)
        {
            return await ReadFile(ruleFilePath, new NullLogger(), cancellationToken);
        }

        public static async Task<(IPreprocessedRule preprocessedRule, bool result)> ReadFile(string ruleFilePath, IAggregatorLogger logger, CancellationToken cancellationToken = default)
        {
            var content = await ReadAllLinesAsync(ruleFilePath, cancellationToken);
            return Read(content, logger);
        }

        /// <summary>
        /// Grab directives
        /// </summary>
        public static (IPreprocessedRule preprocessedRule, bool parseSuccess) Read(string[] ruleCode, IAggregatorLogger logger = default)
        {
            var me = new RuleFileParser(logger);
            var result = me.Parse(ruleCode);
            if (result.parseSuccess)
            {
                result.parseSuccess = me.Validate(result.preprocessedRule);
            }
            return result;
        }

        bool parsingIssues = false;
        private readonly IAggregatorLogger logger;

        public RuleFileParser(IAggregatorLogger logger)
        {
            this.logger = logger;
        }

        void FailParsingWithMessage(string message)
        {
            logger?.WriteWarning(message);
            parsingIssues = true;
        }

        (IPreprocessedRule preprocessedRule, bool parseSuccess) Parse(string[] ruleCode)
        {
            var directiveLineIndex = 0;
            var preprocessedRule = new PreprocessedRule()
            {
                Language = RuleLanguage.Csharp
            };

            while (directiveLineIndex < ruleCode.Length
                && ruleCode[directiveLineIndex].Length > 0
                && ruleCode[directiveLineIndex][0] == '.')
            {
                string directive = ruleCode[directiveLineIndex][1..];
                // stop at first '=' or ' '
                int endVerb = directive.IndexOfAny(new char[] { '=', ' ' });
                if (endVerb < 1)
                {
                    FailParsingWithMessage($"Invalid language directive {directive}");
                }
                string verb = directive[..endVerb];
                string arguments = directive[(endVerb + 1)..];

                ParseDirective(preprocessedRule, directive, verb, arguments);

                // this keep the same number of lines
                preprocessedRule.RuleCode.Add($"// {directive}");

                directiveLineIndex++;
            }//while

            preprocessedRule.FirstCodeLine = directiveLineIndex;

            preprocessedRule.RuleCode.AddRange(ruleCode.Skip(preprocessedRule.FirstCodeLine));

            var parseSuccessful = !parsingIssues;
            return (preprocessedRule, parseSuccessful);

        }

        void ParseDirective(PreprocessedRule preprocessedRule, string directive, string verb, string arguments)
        {
            switch (verb.ToLowerInvariant())
            {
                case "lang":
                case "language":
                    ParseLanguageDirective(preprocessedRule, directive, arguments);
                    break;

                case "r":
                case "ref":
                case "reference":
                    ParseReferenceDirective(preprocessedRule, directive, arguments);
                    break;

                case "import":
                case "imports":
                case "namespace":
                    ParseImportDirective(preprocessedRule, directive, arguments);
                    break;

                case "event":
                case "events":
                    ParseEventDirective(preprocessedRule, directive, arguments);
                    break;

                case "impersonate":
                    ParseImpersonateDirective(preprocessedRule, directive, arguments);
                    break;
                case "bypassrules":
                    ParseBypassRulesDirective(preprocessedRule, directive, arguments);
                    break;
                case "check":
                    ParseCheckDirective(preprocessedRule, directive, arguments);
                    break;

                default:
                    FailParsingWithMessage($"Unrecognized directive {directive}");
                    break;
            }//switch
        }

        private void ParseLanguageDirective(PreprocessedRule preprocessedRule, string directive, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                FailParsingWithMessage($"Invalid language directive {directive}");
            }
            else
            {
                switch (arguments.ToUpperInvariant())
                {
                    case "C#":
                    case "CS":
                    case "CSHARP":
                        preprocessedRule.Language = RuleLanguage.Csharp;
                        break;
                    default:
                        {
                            FailParsingWithMessage($"Unrecognized language {arguments}");
                            preprocessedRule.Language = RuleLanguage.Unknown;
                            break;
                        }
                }
            }
        }

        private void ParseReferenceDirective(PreprocessedRule preprocessedRule, string directive, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                FailParsingWithMessage($"Invalid reference directive {directive}");
            }
            else
            {
                preprocessedRule.References.Add(arguments);
            }
        }

        private void ParseImportDirective(PreprocessedRule preprocessedRule, string directive, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                FailParsingWithMessage($"Invalid import directive {directive}");
            }
            else
            {
                preprocessedRule.Imports.Add(arguments);
            }
        }

        private void ParseEventDirective(PreprocessedRule preprocessedRule, string directive, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                FailParsingWithMessage($"Invalid event directive {directive}");
            }
            else
            {
                var events = arguments.Split(',', ' ', ' ');

                var invalidEvents = events.Where(e => !DevOpsEvents.IsValidEvent(e));
                if (invalidEvents.Any())
                {
                    FailParsingWithMessage($"Invalid event {invalidEvents.First()} in directive");
                }
                preprocessedRule.Events.AddRange(events);
            }
        }

        private void ParseImpersonateDirective(PreprocessedRule preprocessedRule, string directive, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                FailParsingWithMessage($"Invalid impersonate directive {directive}");
            }
            else
            {
                preprocessedRule.Impersonate = string.Equals("onBehalfOfInitiator", arguments.TrimEnd(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private void ParseBypassRulesDirective(PreprocessedRule preprocessedRule, string directive, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                FailParsingWithMessage($"Invalid bypassrules directive {directive}");
            }
            else
            {
                preprocessedRule.BypassRules = string.Equals("true", arguments.TrimEnd(), StringComparison.OrdinalIgnoreCase);
            }
        }

        private void ParseCheckDirective(PreprocessedRule preprocessedRule, string directive, string arguments)
        {
            if (string.IsNullOrWhiteSpace(arguments))
            {
                FailParsingWithMessage($"Invalid check directive {directive}");
            }
            else
            {
                var elements = arguments.Trim().Split(' ');
                if (elements.Length < 2)
                {
                    FailParsingWithMessage($"Invalid check directive {directive}");
                }
                else
                {
                    string checkName = elements[0].Trim();
                    if (!bool.TryParse(elements[1].Trim(), out bool checkValue))
                    {
                        FailParsingWithMessage($"Invalid check directive {directive}");
                    }
                    else
                    {
                        switch (checkName)
                        {
                            case "revision":
                                preprocessedRule.Settings.EnableRevisionCheck = checkValue;
                                break;
                            default:
                                FailParsingWithMessage($"Invalid check directive {directive}");
                                break;
                        }
                    }
                }
            }
        }

        // TODO after 1.0 uncomment
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1172:Unused method parameters should be removed", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out", Justification = "<Pending>")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "<Pending>")]
        private bool Validate(IPreprocessedRule _/*preprocessedRule*/)
        {
            //if (preprocessedRule.Events.Count < 1)
            //    return false;
            return true;
        }

        public static async Task WriteFile(string ruleFilePath, IPreprocessedRule preprocessedRule, CancellationToken cancellationToken = default)
        {
            var content = Write(preprocessedRule);

            await WriteAllLinesAsync(ruleFilePath, content, cancellationToken);
        }

        public static string[] Write(IPreprocessedRule preprocessedRule)
        {
            var content = new List<string>
                          {
                              $".language={preprocessedRule.LanguageAsString()}"
                          };

            if (preprocessedRule.Impersonate)
            {
                content.Add($".impersonate=onBehalfOfInitiator");
            }
            if (!preprocessedRule.Settings.EnableRevisionCheck)
            {
                content.Add($".check revision false");
            }
            if (preprocessedRule.BypassRules)
            {
                content.Add($".bypassrules=true");
            }

            content.AddRange(preprocessedRule.References.Select(reference => $".reference={reference}"));
            content.AddRange(preprocessedRule.Imports.Select(import => $".import={import}"));
            content.AddRange(preprocessedRule.Events.Select(evnt => $".event={evnt}"));

            content.AddRange(preprocessedRule.RuleCode.Skip(preprocessedRule.FirstCodeLine));

            return content.ToArray();
        }

        private static async Task<string[]> ReadAllLinesAsync(string ruleFilePath, CancellationToken cancellationToken)
        {
            using var fileStream = File.OpenRead(ruleFilePath);
            using var streamReader = new StreamReader(fileStream);
            var lines = new List<string>();
            string line;
            while ((line = await streamReader.ReadLineAsync().ConfigureAwait(false)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                lines.Add(line);
            }

            return lines.ToArray();
        }

        private static async Task WriteAllLinesAsync(string ruleFilePath, IEnumerable<string> ruleContent, CancellationToken cancellationToken)
        {
            using var fileStream = File.OpenWrite(ruleFilePath);
            using var streamWriter = new StreamWriter(fileStream);
            foreach (var line in ruleContent)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await streamWriter.WriteLineAsync(line).ConfigureAwait(false);
            }
        }
    }
}

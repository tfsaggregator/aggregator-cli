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
    public static class RuleFileParser
    {
        public static async Task<(IRuleDirectives ruleDirectives, bool result)> ReadFile(string ruleFilePath, CancellationToken cancellationToken = default)
        {
            return await ReadFile(ruleFilePath, new NullLogger(), cancellationToken);
        }

        public static async Task<(IRuleDirectives ruleDirectives, bool result)> ReadFile(string ruleFilePath, IAggregatorLogger logger, CancellationToken cancellationToken = default)
        {
            var content = await ReadAllLinesAsync(ruleFilePath, cancellationToken);
            return Read(content, logger);
        }

        /// <summary>
        /// Grab directives
        /// </summary>
        public static (IRuleDirectives ruleDirectives, bool parseSuccess) Read(string[] ruleCode, IAggregatorLogger logger = default)
        {
            var parsingIssues = false;
            void FailParsingWithMessage(string message)
            {
                logger?.WriteWarning(message);
                parsingIssues = true;
            }

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
                            FailParsingWithMessage($"Invalid language directive {directive}");
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
                                    FailParsingWithMessage($"Unrecognized language {parts[1]}");
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
                            FailParsingWithMessage($"Invalid reference directive {directive}");
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
                            FailParsingWithMessage($"Invalid import directive {directive}");
                        }
                        else
                        {
                            ruleDirectives.Imports.Add(parts[1]);
                        }
                        break;

                    case "impersonate":
                        if (parts.Length < 2)
                        {
                            FailParsingWithMessage($"Invalid impersonate directive {directive}");
                        }
                        else
                        {
                            ruleDirectives.Impersonate = string.Equals("onBehalfOfInitiator", parts[1].TrimEnd(), StringComparison.OrdinalIgnoreCase);
                        }
                        break;

                    default:
                    {
                        FailParsingWithMessage($"Unrecognized directive {directive}");
                        break;
                    }
                }//switch

                directiveLineIndex++;
            }//while

            ruleDirectives.RuleCode.AddRange(ruleCode.Skip(directiveLineIndex));
            var parseSuccessful = !parsingIssues;
            return (ruleDirectives, parseSuccessful);
        }

        public static async Task WriteFile(string ruleFilePath, IRuleDirectives ruleDirectives, CancellationToken cancellationToken = default)
        {
            var content = Write(ruleDirectives);

            await WriteAllLinesAsync(ruleFilePath, content, cancellationToken);
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

        private static async Task<string[]> ReadAllLinesAsync(string ruleFilePath, CancellationToken cancellationToken)
        {
            using (var fileStream = File.OpenRead(ruleFilePath))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    var lines = new List<string>();
                    string line;
                    while ((line = await streamReader.ReadLineAsync().ConfigureAwait(false)) != null)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        lines.Add(line);
                    }

                    return lines.ToArray();
                }
            }
        }

        private static async Task WriteAllLinesAsync(string ruleFilePath, IEnumerable<string> ruleContent, CancellationToken cancellationToken)
        {
            using (var fileStream = File.OpenWrite(ruleFilePath))
            {
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    foreach (var line in ruleContent)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        await streamWriter.WriteLineAsync(line).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}

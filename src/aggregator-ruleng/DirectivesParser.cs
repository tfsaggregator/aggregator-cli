using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.Engine
{
    /// <summary>
    /// Scan the top lines of a script looking for directives, whose lines start with a .
    /// </summary>
    internal class DirectivesParser
    {
        int firstCodeLine = 0;
        private readonly IAggregatorLogger logger;
        string[] ruleCode;

        internal DirectivesParser(IAggregatorLogger logger, string[] ruleCode)
        {
            this.ruleCode = ruleCode;
            this.logger = logger;

            //defaults
            Language = Languages.Csharp;
        }

        /// <summary>
        /// Grab directives
        /// </summary>
        internal bool Parse()
        {
            while (firstCodeLine < ruleCode.Length
                && ruleCode[firstCodeLine].Length > 0
                && ruleCode[firstCodeLine][0] == '.')
            {
                string directive = ruleCode[firstCodeLine].Substring(1);
                var parts = directive.Split('=');

                switch (parts[0].ToLowerInvariant())
                {
                    case "lang":
                    case "language":
                        if (parts.Length < 2)
                        {
                            logger.WriteWarning($"Unrecognized directive {directive}");
                            return false;
                        }
                        else
                        {
                            switch (parts[1].ToUpperInvariant())
                            {
                                case "C#":
                                case "CS":
                                case "CSHARP":
                                    Language = Languages.Csharp;
                                    break;
                                default:
                                    logger.WriteWarning($"Unrecognized language {parts[1]}");
                                    return false;
                            }
                        }
                        break;
                    default:
                        logger.WriteWarning($"Unrecognized directive {directive}");
                        return false;
                }//switch

                firstCodeLine++;
            }//while
            return true;
        }

        internal string GetRuleCode()
        {
            return string.Join(Environment.NewLine, ruleCode, firstCodeLine, ruleCode.Length - firstCodeLine);
        }

        // directives

        internal enum Languages
        {
            Csharp
        }

        internal Languages Language { get; private set; }
    }
}

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using aggregator.Engine;
using aggregator.Engine.Language;

namespace aggregator
{
    internal class AzureFunctionRuleProvider : IRuleProvider
    {
        private const string SCRIPT_RULE_NAME_PATTERN = "*.rule";

        private readonly IAggregatorLogger _logger;
        private readonly string _rulesPath;

        public AzureFunctionRuleProvider(IAggregatorLogger logger, string functionDirectory)
        {
            _logger = logger;
            _rulesPath = functionDirectory;
        }

        /// <inheritdoc />
        public async Task<IRule> GetRule(string name)
        {
            var ruleFilePath = GetRuleFilePath(name);
            var (preprocessedRule, _) = await RuleFileParser.ReadFile(ruleFilePath);

            return new ScriptedRuleWrapper(name, preprocessedRule);
        }

        private string GetRuleFilePath(string ruleName)
        {
            bool IsRequestedRule(string filePath)
            {
                return string.Equals(ruleName, Path.GetFileNameWithoutExtension(filePath), StringComparison.OrdinalIgnoreCase);
            }

            var ruleFilePath = Directory.EnumerateFiles(_rulesPath, SCRIPT_RULE_NAME_PATTERN, SearchOption.TopDirectoryOnly)
                .First(IsRequestedRule);

            if (ruleFilePath == null)
            {
                var errorMsg = $"Rule code file '{ruleName}.rule' not found at expected Path {_rulesPath}";
                _logger.WriteError(errorMsg);
                throw new FileNotFoundException(errorMsg);
            }

            _logger.WriteVerbose($"Rule code found at {ruleFilePath}");
            return ruleFilePath;
        }
    }
}

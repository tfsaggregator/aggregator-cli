using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using aggregator.Engine;
using aggregator.Engine.Language;
using Microsoft.Extensions.FileProviders;

namespace aggregator
{
    internal class AspNetRuleProvider : IRuleProvider
    {
        private const string SCRIPT_RULE_DIRECTORY = "rules";
        private const string SCRIPT_RULE_NAME_PATTERN = ".rule";

        private readonly IAggregatorLogger _logger;
        private readonly string _rulesPath;

        public AspNetRuleProvider(IAggregatorLogger logger, string applicationRoot)
        {
            _logger = logger;
            _rulesPath = applicationRoot;
        }

        /// <inheritdoc />
        public async Task<IRule> GetRule(string ruleName)
        {
            var ruleFilePath = GetRuleFilePath(ruleName);
            var (preprocessedRule, _) = await RuleFileParser.ReadFile(ruleFilePath);

            return new ScriptedRuleWrapper(ruleName, preprocessedRule);
        }

        private string GetRuleFilePath(string ruleName)
        {
            bool IsRequestedRule(IFileInfo info)
            {
                return string.Equals(ruleName, Path.GetFileNameWithoutExtension(info.Name), StringComparison.OrdinalIgnoreCase);
            }

            var provider = new PhysicalFileProvider(_rulesPath);
            var contents = provider.GetDirectoryContents(SCRIPT_RULE_DIRECTORY).Where(f=>f.Name.EndsWith(SCRIPT_RULE_NAME_PATTERN));

            var ruleFilePath = contents.First(IsRequestedRule)?.PhysicalPath;

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

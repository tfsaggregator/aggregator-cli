using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using aggregator.Engine;
using aggregator.Engine.Language;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace aggregator
{
    internal class AspNetRuleProvider : IRuleProvider
    {
        private const string SCRIPT_RULE_DIRECTORY = "rules";
        private const string SCRIPT_RULE_NAME_PATTERN = ".rule";

        private readonly IAggregatorLogger _logger;
        private IConfiguration _configuration;

        public AspNetRuleProvider(ForwarderLogger logger, IConfiguration configuration)
        {
            this._logger = logger;
            this._configuration = configuration;
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

            string ruleFilePath = null;
            string rulesPath = _configuration.GetValue<string>("Aggregator_RulesPath");
            if (string.IsNullOrEmpty(rulesPath))
            {
                rulesPath = _configuration.GetValue<string>(WebHostDefaults.ContentRootKey);
                _logger.WriteVerbose($"Searching '{ruleName}' in {rulesPath}");
                var provider = new PhysicalFileProvider(rulesPath);
                var contents = provider.GetDirectoryContents(SCRIPT_RULE_DIRECTORY).Where(f => f.Name.EndsWith(SCRIPT_RULE_NAME_PATTERN));

                ruleFilePath = contents.First(IsRequestedRule)?.PhysicalPath;
            }
            else
            {
                _logger.WriteVerbose($"Searching '{ruleName}' in {rulesPath}");
                string ruleFullPath = Path.Combine(rulesPath, $"{ruleName}{SCRIPT_RULE_NAME_PATTERN}");
                ruleFilePath = File.Exists(ruleFullPath) ? ruleFullPath : null;
            }

            if (ruleFilePath == null)
            {
                var errorMsg = $"Rule code file '{ruleName}{SCRIPT_RULE_NAME_PATTERN}' not found at expected Path {rulesPath}";
                _logger.WriteError(errorMsg);
                throw new FileNotFoundException(errorMsg);
            }

            _logger.WriteVerbose($"Rule code found at {ruleFilePath}");
            return ruleFilePath;
        }
    }
}

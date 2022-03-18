using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using aggregator.Model;

using Microsoft.VisualStudio.Services.Common;


namespace aggregator
{
    public enum DevOpsTokenType
    {
        Integrated = 0,
        PAT = 1,
    }

    public enum SaveMode
    {
        Default = 0,
        Item = 1,
        Batch = 2,
        TwoPhases = 3
    }

    public interface IAggregatorConfiguration
    {
        DevOpsTokenType DevOpsTokenType { get; set; }
        string DevOpsToken { get; set; }
        SaveMode SaveMode { get; set; }
        bool DryRun { get; set; }

        IDictionary<string, IRuleConfiguration> RulesConfiguration { get; }
    }

    public interface IRuleConfiguration
    {
        string RuleName { get; }
        bool IsDisabled { get; set; }
        bool Impersonate { get; set; }
        bool BypassRules { get; set; }
    }


    /// <summary>
    /// This class tracks the configuration data that CLI writes and Function runtime reads
    /// </summary>
    public static class AggregatorConfiguration
    {
        const string RULE_SETTINGS_PREFIX = "AzureWebJobs.";

        public static async Task<IAggregatorConfiguration> ReadConfiguration(Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp)
        {
            static (string ruleName, string key) SplitRuleNameKey(string input)
            {
                int idx = input.LastIndexOf('.');
                return (input[..idx], input[(idx + 1)..]);
            }

            var settings = await webApp.GetAppSettingsAsync();
            var ac = new Model.AggregatorConfiguration();
            foreach (var ruleSetting in settings.Where(kvp => kvp.Key.StartsWith(RULE_SETTINGS_PREFIX)).Select(kvp => new { ruleNameKey = kvp.Key[RULE_SETTINGS_PREFIX.Length..], value = kvp.Value.Value }))
            {
                var (ruleName, key) = SplitRuleNameKey(ruleSetting.ruleNameKey);

                var ruleConfig = ac.GetRuleConfiguration(ruleName);
                if (string.Equals("Disabled", key, StringComparison.OrdinalIgnoreCase))
                {
                    ruleConfig.IsDisabled = Boolean.TryParse(ruleSetting.value, out bool result) && result;
                }

                if (string.Equals("Impersonate", key, StringComparison.OrdinalIgnoreCase))
                {
                    ruleConfig.Impersonate = string.Equals("onBehalfOfInitiator", ruleSetting.value, StringComparison.OrdinalIgnoreCase);
                }

                if (string.Equals("BypassRules", key, StringComparison.OrdinalIgnoreCase))
                {
                    ruleConfig.BypassRules = string.Equals("true", ruleSetting.value, StringComparison.OrdinalIgnoreCase);
                }
            }

            Enum.TryParse(settings.GetValueOrDefault("Aggregator_VstsTokenType")?.Value, out DevOpsTokenType vtt);
            ac.DevOpsTokenType = vtt;
            ac.DevOpsToken = settings.GetValueOrDefault("Aggregator_VstsToken")?.Value;
            ac.SaveMode = Enum.TryParse(settings.GetValueOrDefault("Aggregator_SaveMode")?.Value, out SaveMode sm)
                ? sm
                : SaveMode.Default;
            ac.DryRun = false;
            return ac;
        }

        public static IAggregatorConfiguration ReadConfiguration(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            var ac = new Model.AggregatorConfiguration();
            Enum.TryParse(config["Aggregator_VstsTokenType"], out DevOpsTokenType vtt);
            ac.DevOpsTokenType = vtt;
            ac.DevOpsToken = config["Aggregator_VstsToken"];
            ac.SaveMode = Enum.TryParse(config["Aggregator_SaveMode"], out SaveMode sm)
                ? sm
                : SaveMode.Default;
            ac.DryRun = false;
            return ac;
        }

        public static void WriteConfiguration(this IAggregatorConfiguration config, Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp)
        {
            var settings = new Dictionary<string, string>()
            {
                {"Aggregator_VstsTokenType", config.DevOpsTokenType.ToString()},
                {"Aggregator_VstsToken", config.DevOpsToken},
                {"Aggregator_SaveMode", config.SaveMode.ToString()},
            };

            foreach (var ruleSetting in config.RulesConfiguration.Select(kvp => kvp.Value))
            {
                settings.AddRuleSettings(ruleSetting);
            }

            webApp.ApplyWithAppSettings(settings);
        }

        public static void WriteConfiguration(this IRuleConfiguration config, Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp)
        {
            var settings = new Dictionary<string, string>();

            settings.AddRuleSettings(config);

            webApp.ApplyWithAppSettings(settings);
        }

        public static void Delete(this IRuleConfiguration config, Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp)
        {
            var settings = new Dictionary<string, string>();

            settings.AddRuleSettings(config);

            var update = webApp.Update();

            foreach (var key in settings.Keys)
            {
                update.WithoutAppSetting(key);
            }

            update.Apply();
        }

        public static IRuleConfiguration GetRuleConfiguration(this IAggregatorConfiguration config, string ruleName)
        {
            var ruleConfig = config.RulesConfiguration.GetValueOrDefault(ruleName);
            if (ruleConfig == null)
            {
                ruleConfig = new RuleConfiguration(ruleName);
                config.RulesConfiguration[ruleName] = ruleConfig;
            }

            return ruleConfig;
        }

        private static void AddRuleSettings(this Dictionary<string, string> settings, IRuleConfiguration ruleSetting)
        {
            settings[$"{RULE_SETTINGS_PREFIX}{ruleSetting.RuleName}.Disabled"] = ruleSetting.IsDisabled.ToString();
            settings[$"{RULE_SETTINGS_PREFIX}{ruleSetting.RuleName}.Impersonate"] = ruleSetting.Impersonate ? "onBehalfOfInitiator" : "false";
            settings[$"{RULE_SETTINGS_PREFIX}{ruleSetting.RuleName}.BypassRules"] = ruleSetting.BypassRules ? "true" : "false";
        }

        private static void ApplyWithAppSettings(this Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp, Dictionary<string, string> settings)
        {
            webApp
                .Update()
                .WithAppSettings(settings)
                .Apply();
        }
    }
}

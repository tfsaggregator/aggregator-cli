using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("configure.rule", HelpText = "Change a rule configuration.")]
    class ConfigureRuleCommand : CommandBase
    {
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        [Option('d', "disable", SetName = "disable", HelpText = "Disable the rule.")]
        public bool? Disable { get; set; }
        [Option('e', "enable", SetName = "enable", HelpText = "Enable the rule.")]
        public bool? Enable { get; set; }

        [Option("disableImpersonate", Required = false, HelpText = "Disable do rule changes impersonated.")]
        public bool? DisableImpersonateExecution { get; set; }

        [Option("enableImpersonate", Required = false, HelpText = "Enable do rule changes on behalf of the person triggered the rule execution. See wiki for details, requires special account privileges.")]
        public bool? EnableImpersonateExecution { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .BuildAsync(cancellationToken);
            var instance = new InstanceName(Instance, ResourceGroup);
            var rules = new AggregatorRules(context.Azure, context.Logger);

            var disable = GetDisableStatus(Disable, Enable);
            var impersonate = GetEnableStatus(DisableImpersonateExecution, EnableImpersonateExecution);

            var ok = await rules.ConfigureAsync(instance, Name, disable, impersonate, cancellationToken);
            return ok ? 0 : 1;
        }


        /// <summary>
        /// in case of neither disableSetting nor enableSetting is set return null
        /// Otherwise return value of disableSetting
        /// </summary>
        /// <param name="disableSetting"></param>
        /// <param name="enableSetting"></param>
        /// <returns></returns>
        private static bool? GetDisableStatus(bool? disableSetting, bool? enableSetting)
        {
            return GetEnableDisableStatus(disableSetting, enableSetting, disableSetting);
        }

        /// <summary>
        /// in case of neither disableSetting nor enableSetting is set return null
        /// Otherwise return value of enableSetting
        /// </summary>
        /// <param name="disableSetting"></param>
        /// <param name="enableSetting"></param>
        /// <returns></returns>
        private static bool? GetEnableStatus(bool? disableSetting, bool? enableSetting)
        {
            return GetEnableDisableStatus(disableSetting, enableSetting, enableSetting);
        }

        private static bool? GetEnableDisableStatus(bool? disableSetting, bool? enableSetting, bool? defaultSetting)
        {
            return disableSetting.HasValue || enableSetting.HasValue ? (bool?)(defaultSetting ?? false) : null;
        }
    }
}

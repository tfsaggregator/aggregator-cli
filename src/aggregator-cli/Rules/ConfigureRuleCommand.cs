using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("configure.rule", HelpText = "Change a rule configuration.")]
    class ConfigureRuleCommand : CommandBase
    {
        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        [Option('u', "update", SetName = "update", HelpText = "Update the runtime and rule code.")]
        public string Update { get; set; }

        [Option('d', "disable", SetName = "disable", HelpText = "Disable the rule.")]
        public bool Disable { get; set; }
        [Option('e', "enable", SetName = "enable", HelpText = "Enable the rule.")]
        public bool Enable { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .Build();
            var instance = new InstanceName(Instance);
            var rules = new AggregatorRules(context.Azure, context.Logger);
            bool ok = false;
            if (Disable || Enable)
            {
                ok = await rules.EnableAsync(instance, Name, Disable);
            }
            if (!string.IsNullOrEmpty(Update))
            {
                ok = await rules.UpdateAsync(instance, Name, Update);
            }
            return ok ? 0 : 1;
        }
    }
}

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("update.rule", HelpText = "Update a rule code and/or runtime.")]
    class UpdateRuleCommand : CommandBase
    {
        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('n', "name", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }

        [Option('f', "file", Required = true, HelpText = "Aggregator rule code.")]
        public string File { get; set; }

        [Option("requiredVersion", SetName = "nourl", Required = false, HelpText = "Version of Aggregator Runtime required.")]
        public string RequiredVersion { get; set; }

        [Option("sourceUrl", SetName = "url", Required = false, HelpText = "URL of Aggregator Runtime.")]
        public string SourceUrl { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .BuildAsync(cancellationToken);
            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var rules = new AggregatorRules(context.Azure, context.Logger);
            bool ok = await rules.UpdateAsync(instance, Name, File, RequiredVersion, SourceUrl, cancellationToken);
            return ok ? ExitCodes.Success : ExitCodes.Failure;
        }
    }
}


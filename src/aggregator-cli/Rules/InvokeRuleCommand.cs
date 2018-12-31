using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("invoke.rule", HelpText = "Executes a rule locally or in an existing Aggregator instance.")]
    class InvokeRuleCommand : CommandBase
    {
        [Option('d', "dryrun", Required = false, Default = false, HelpText = "Real or non-committing run.")]
        public bool DryRun { get; set; }

        [Option('p', "project", Required = true, HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [Option('e', "event", Required = true, HelpText = "Event to emulate.")]
        public string Event { get; set; }

        [Option('w', "workItemId", Required = true, HelpText = "Id of workitem for the emulated event.")]
        public int WorkItemId { get; set; }

        [Option('n', "local", SetName = "Local", Required = true, HelpText = "Rule run locally.")]
        public bool Local { get; set; }

        [Option('s', "source", SetName = "Local", Required = true, HelpText = "Aggregator rule code.")]
        public string Source { get; set; }

        [Option('m', "saveMode", Required = false, HelpText = "Save behaviour.")]
        public SaveMode SaveMode { get; set; }

        [Option('a', "account", SetName = "Remote", Required = true, HelpText = "Azure DevOps account name.")]
        public string Account { get; set; }

        [Option('i', "instance", SetName = "Remote", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('g', "resourceGroup", SetName = "Remote", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }

        [Option('n', "name", SetName = "Remote", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }


        internal override async Task<int> RunAsync()
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .Build();
            var rules = new AggregatorRules(context.Azure, context.Logger);
            if (Local)
            {
                bool ok = await rules.InvokeLocalAsync(Project, Event, WorkItemId, Source, DryRun, SaveMode);
                return ok ? 0 : 1;
            }
            else
            {
                var instance = new InstanceName(Instance, ResourceGroup);
                context.Logger.WriteWarning("Untested feature!");
                bool ok = await rules.InvokeRemoteAsync(Account, Project, Event, WorkItemId, instance, Name, DryRun, SaveMode);
                return ok ? 0 : 1;
            }
        }
    }
}

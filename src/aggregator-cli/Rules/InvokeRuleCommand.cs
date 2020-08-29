using System.Threading;
using System.Threading.Tasks;
using CommandLine;


namespace aggregator.cli
{
    [Verb("invoke.rule", HelpText = "Executes a rule locally or in an existing Aggregator instance.")]
    class InvokeRuleCommand : CommandBase
    {
        [ShowInTelemetry]
        [Option('d', "dryrun", Required = false, Default = false, HelpText = "Real or non-committing run.")]
        public bool DryRun { get; set; }

        [Option('p', "project", Required = true, HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [ShowInTelemetry]
        [Option('e', "event", Required = true, HelpText = "Event to emulate.")]
        public string Event { get; set; }

        [Option('w', "workItemId", Required = true, HelpText = "Id of workitem for the emulated event.")]
        public int WorkItemId { get; set; }

        [ShowInTelemetry]
        [Option('n', "local", SetName = "Local", Required = true, HelpText = "Rule run locally.")]
        public bool Local { get; set; }

        [Option('s', "source", SetName = "Local", Required = true, HelpText = "Aggregator rule code.")]
        public string Source { get; set; }

        [ShowInTelemetry]
        [Option('m', "saveMode", Required = false, HelpText = "Save behaviour.")]
        public SaveMode SaveMode { get; set; }

        [ShowInTelemetry]
        [Option("impersonate", Required = false, HelpText = "Do rule changes on behalf of the person triggered the rule execution. See wiki for details, requires special account privileges.")]
        public bool ImpersonateExecution { get; set; }

        [Option('a', "account", SetName = "Remote", Required = true, HelpText = "Azure DevOps account name.")]
        public string Account { get; set; }

        [Option('i', "instance", SetName = "Remote", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [ShowInTelemetry(TelemetryDisplayMode.Presence)]
        [Option('g', "resourceGroup", SetName = "Remote", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instances.")]
        public string ResourceGroup { get; set; }

        [ShowInTelemetry]
        [Option('n', "name", SetName = "Remote", Required = true, HelpText = "Aggregator rule name.")]
        public string Name { get; set; }


        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            var rules = new AggregatorRules(context.Azure, context.Logger);
            bool ok = DevOpsEvents.IsValidEvent(Event);
            if (!ok)
            {
                context.Logger.WriteError($"Invalid event type.");
                return ExitCodes.InvalidArguments;
            }
            if (Local)
            {
                ok = await rules.InvokeLocalAsync(Project, Event, WorkItemId, Source, DryRun, SaveMode, ImpersonateExecution, cancellationToken);
                return ok ? ExitCodes.Success : ExitCodes.Failure;
            }
            else
            {
                var instance = context.Naming.Instance(Instance, ResourceGroup);
                context.Logger.WriteWarning("Untested feature!");
                ok = await rules.InvokeRemoteAsync(Account, Project, Event, WorkItemId, instance, Name, DryRun, SaveMode, ImpersonateExecution, cancellationToken);
                return ok ? ExitCodes.Success : ExitCodes.Failure;
            }
        }
    }
}

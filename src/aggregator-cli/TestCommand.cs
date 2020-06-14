using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{
    [Verb("test.create", HelpText = "Creates a work item and capture the log.", Hidden = true)]
    class TestCommand : CommandBase
    {
        [Option('p', "project", Required = true, HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('t', "title", Required = false, Default = "Aggregator CLI Test Task", HelpText = "Title for new Work Item.")]
        public string Title { get; set; }

        [Option('l', "lastLinePattern", Required = false, Default = @"Executed \'Functions\.", HelpText = "RegEx Pattern identifying last line of logs.")]
        public string LastLinePattern { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var instances = new AggregatorInstances(context.Azure, context.Logger, context.Naming);
            var boards = new Boards(context.Devops, context.Logger);

            var streamTask = instances.StreamLogsAsync(instance, lastLinePattern: this.LastLinePattern, cancellationToken: cancellationToken);
            int id = await boards.CreateWorkItemAsync(this.Project, this.Title, cancellationToken);
            streamTask.Wait(cancellationToken);
            return id > 0 ? 0 : 1;
        }
    }
}

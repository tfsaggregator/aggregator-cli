using System;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{
    [Verb("test.create", HelpText = "Creates a work item and capture the log.", Hidden = true)]
    class CreateTestCommand : CommandBase
    {
        [Option('p', "project", Required = true, HelpText = "Azure DevOps project name.")]
        public string Project { get; set; }

        [Option('i', "instance", Required = true, HelpText = "Aggregator instance name.")]
        public string Instance { get; set; }

        [Option('g', "resourceGroup", Required = false, Default = "", HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        [Option('t', "title", Required = false, Default = "Aggregator CLI Test Task", HelpText = "Title for new Work Item.")]
        public string Title { get; set; }

        //[Option('l', "lastLinePattern", Required = false, Default = @"Executed \'Functions\.", HelpText = "RegEx Pattern identifying last line of logs.")]
        //public string LastLinePattern { get; set; }
        [Option('r', "rule", Required = true, HelpText = "Aggregator rule name.")]
        public string RuleName { get; set; }


        [Option('n', "returnId", Required = false, Default =false, HelpText = "Return work item id instead of return code.")]
        public bool returnId { get; set; }


#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            throw new NotImplementedException("Use CreateTestCommand.RunWithReturnAsync() instead");
        }

        internal override async Task<(bool success, int returnCode)> RunWithReturnAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);
            var instance = context.Naming.Instance(Instance, ResourceGroup);
            var instances = new AggregatorInstances(context.Azure, null, context.Logger, context.Naming);
            var boards = new Boards(context.Devops, context.Logger);

            int id = await boards.CreateWorkItemAsync(this.Project, this.Title, cancellationToken);

            // wait for the Event to be processed in AzDO, sent via WebHooks, and the Function to run
            await Task.Delay(new TimeSpan(0, 2, 0), cancellationToken);

            // no need to use the output, it is checked by user or test
            await instances.ReadLogAsync(instance, this.RuleName, -1, cancellationToken: cancellationToken);

            return returnId ? (id > 0, id) : (id > 0, 0);
        }
    }
}

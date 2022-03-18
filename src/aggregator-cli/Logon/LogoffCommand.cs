using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace aggregator.cli
{
    [Verb("logoff", HelpText = "Logoff from all systems.")]
    class LogoffCommand : CommandBase
    {
        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context.BuildAsync(cancellationToken);

            context.Logger.WriteVerbose($"Clearing cached credentials");
            AzureLogon.Clear();
            DevOpsLogon.Clear();

            return ExitCodes.Success;
        }
    }
}

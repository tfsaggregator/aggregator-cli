using Microsoft.Azure.Management.Fluent;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Threading.Tasks;

namespace aggregator.cli
{

    internal class CommandContext
    {
        internal ILogger Logger { get; }
        internal IAzure Azure { get; }
        internal VssConnection Devops { get; }
        internal CommandContext(ILogger logger, IAzure azure, VssConnection devops)
        {
            Logger = logger;
            Azure = azure;
            Devops = devops;
        }
    }

    internal class ContextBuilder
    {
        private readonly ILogger logger;
        private bool azureLogon;
        private bool devopsLogon;

        internal ContextBuilder(ILogger logger) => this.logger = logger;

        internal ContextBuilder WithAzureLogon()
        {
            azureLogon = true;
            return this;
        }

        internal ContextBuilder WithDevOpsLogon()
        {
            devopsLogon = true;
            return this;
        }

        internal async Task<CommandContext> Build()
        {
            IAzure azure = null;
            VssConnection devops = null;

            if (azureLogon)
            {
                logger.WriteVerbose($"Authenticating to Azure...");
                var (connection, reason) = AzureLogon.Load();
                if (reason != LogonResult.Succeeded)
                {
                    string msg = TranslateResult(reason);
                    throw new InvalidOperationException(string.Format(msg, "Azure","logon.azure"));
                }

                azure = await connection.LogonAsync();
                logger.WriteInfo($"Connected to subscription {azure.SubscriptionId}");
            }

            if (devopsLogon)
            {
                logger.WriteVerbose($"Authenticating to Azure DevOps...");
                var (connection, reason) = DevOpsLogon.Load();
                if (reason != LogonResult.Succeeded)
                {
                    string msg = TranslateResult(reason);
                    throw new InvalidOperationException(string.Format(msg, "Azure DevOps", "logon.ado"));
                }

                devops = await connection.LogonAsync();
                logger.WriteInfo($"Connected to {devops.Uri.Host}");
            }

            return new CommandContext(logger, azure, devops);
        }

        private string TranslateResult(LogonResult reason)
        {
            switch (reason)
            {
                case LogonResult.Succeeded:
                    // this should never happen!!!
                    return "Valid credential, logon succeeded";
                case LogonResult.NoLogonData:
                    return "No cached {0} credential: use the {1} command.";
                case LogonResult.LogonExpired:
                    return "Cached {0} credential expired: use the {1} command.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason));
            }
        }
    }
}

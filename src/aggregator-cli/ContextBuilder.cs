using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.VisualStudio.Services.WebApi;

namespace aggregator.cli
{

    internal class CommandContext
    {
        internal ILogger Logger { get; }
        internal IAzure Azure { get; }
        internal VssConnection Devops { get; }
        internal INamingTemplates Naming { get; }
        internal CommandContext(ILogger logger, IAzure azure, VssConnection devops, INamingTemplates naming)
        {
            Logger = logger;
            Azure = azure;
            Devops = devops;
            Naming = naming;
        }
    }

    internal class ContextBuilder
    {
        private readonly string namingTemplate;
        private readonly ILogger logger;
        private bool azureLogon;
        private bool devopsLogon;

        internal ContextBuilder(ILogger logger, string namingTemplate)
        {
            this.logger = logger;
            this.namingTemplate = namingTemplate;
        }

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

        internal async Task<CommandContext> BuildAsync(CancellationToken cancellationToken)
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
                    throw new InvalidOperationException(string.Format(msg, "Azure", "logon.azure"));
                }

                azure = connection.Logon();
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

                devops = await connection.LogonAsync(cancellationToken);
                logger.WriteInfo($"Connected to {devops.Uri.Host}");
            }

            INamingTemplates naming = null;
            switch (namingTemplate.ToLower())
            {
                case "builtin":
                    goto case "";
                case "":
                    naming = new BuiltInNamingTemplates();
                    break;
                default:
                    // implement custom Naming Templates, e.g. reading from a file
                    naming = new FileNamingTemplates(File.ReadAllText(namingTemplate));
                    break;
            }

            return new CommandContext(logger, azure, devops, naming);
        }

        private string TranslateResult(LogonResult reason)
        {
            switch (reason)
            {
                case LogonResult.Succeeded:
                    // this should never happen!!!
                    return "Valid credential, logon succeeded";
                case LogonResult.NoLogonData:
                    return "No cached {0} credential: run the {1} command.";
                case LogonResult.LogonExpired:
                    return "Cached {0} credential expired: run the {1} command.";
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason));
            }
        }
    }
}

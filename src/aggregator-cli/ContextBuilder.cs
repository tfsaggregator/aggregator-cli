using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.VisualStudio.Services.WebApi;

namespace aggregator.cli
{

    internal class CommandContext
    {
        internal ILogger Logger { get; }
        internal IAzure Azure { get; }
        internal IResourceManagementClient AzureManagement { get; }
        internal VssConnection Devops { get; }
        internal INamingTemplates Naming { get; }
        internal CommandContext(ILogger logger, IAzure azure, IResourceManagementClient azureManagement, VssConnection devops, INamingTemplates naming)
        {
            Logger = logger;
            Azure = azure;
            AzureManagement = azureManagement;
            Devops = devops;
            Naming = naming;
        }
    }

    internal class ContextBuilder
    {
        private readonly string namingTemplate;
        private readonly ILogger logger;
        private bool azureLogon;
        private bool azureManagementLogon;
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

        internal ContextBuilder WithAzureManagement()
        {
            azureManagementLogon = true;
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
            IResourceManagementClient azureManagement = null;
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
            if (azureManagementLogon)
            {
                logger.WriteVerbose($"Authenticating to Azure...");
                var (connection, reason) = AzureLogon.Load();
                if (reason != LogonResult.Succeeded)
                {
                    string msg = TranslateResult(reason);
                    throw new InvalidOperationException(string.Format(msg, "Azure", "logon.azure"));
                }

                azureManagement = connection.LogonManagement();
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

            INamingTemplates naming;
            switch (namingTemplate.ToLower())
            {
                case "builtin":
#pragma warning disable S907 // "goto" statement should not be used
                    goto case "";
#pragma warning restore S907 // "goto" statement should not be used
                case "":
                    naming = new BuiltInNamingTemplates();
                    break;
                default:
                    // implement custom Naming Templates, e.g. reading from a file
                    naming = new FileNamingTemplates(File.ReadAllText(namingTemplate));
                    break;
            }

            return new CommandContext(logger, azure, azureManagement, devops, naming);
        }

        private static string TranslateResult(LogonResult reason)
        {
            return reason switch
            {
                LogonResult.Succeeded => "Valid credential, logon succeeded",// this should never happen!!!
                LogonResult.NoLogonData => "No cached {0} credential: run the {1} command.",
                LogonResult.LogonExpired => "Cached {0} credential expired: run the {1} command.",
                _ => throw new ArgumentOutOfRangeException(nameof(reason)),
            };
        }
    }
}

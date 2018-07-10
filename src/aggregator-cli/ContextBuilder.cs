using Microsoft.Azure.Management.Fluent;
using Microsoft.VisualStudio.Services.WebApi;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{

    internal class CommandContext
    {
        internal ILogger Logger { get; private set; }
        internal IAzure Azure { get; private set; }
        internal VssConnection Vsts { get; private set; }
        internal CommandContext(ILogger logger, IAzure azure, VssConnection vsts)
        {
            Logger = logger;
            Azure = azure;
            Vsts = vsts;
        }
    }

    internal class ContextBuilder
    {
        ILogger logger;
        bool azureLogon = false;
        bool vstsLogon = false;

        internal ContextBuilder(ILogger logger) => this.logger = logger;

        internal ContextBuilder WithAzureLogon()
        {
            azureLogon = true;
            return this;
        }
        internal ContextBuilder WithVstsLogon()
        {
            vstsLogon = true;
            return this;
        }
        internal async Task<CommandContext> Build()
        {
            IAzure azure = null;
            VssConnection vsts = null;

            if (azureLogon)
            {
                logger.WriteInfo($"Authenticating to Azure...");
                var logon = AzureLogon.Load();
                if (logon == null)
                {
                    throw new ApplicationException($"No cached Azure credential: use the logon.azure command.");
                }
                azure = await logon.LogonAsync();
            }

            if (vstsLogon)
            {
                logger.WriteInfo($"Authenticating to VSTS...");
                var logon = VstsLogon.Load();
                if (logon == null)
                {
                    throw new ApplicationException($"No cached VSTS credential: use the logon.vsts command.");
                }
                vsts = await logon.LogonAsync();
            }

            return new CommandContext(logger, azure, vsts);
        }
    }
}

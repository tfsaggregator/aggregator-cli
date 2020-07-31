using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("logon.env", HelpText = "Logon into systems.")]
    class LogonEnvCommand : CommandBase
    {
        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context.BuildAsync(cancellationToken);

            var azData = new AzureLogon()
            {
                SubscriptionId = Environment.GetEnvironmentVariable("AGGREGATOR_SUBSCRIPTIONID") ?? string.Empty,
                ClientId = Environment.GetEnvironmentVariable("AGGREGATOR_CLIENTID") ?? string.Empty,
                ClientSecret = Environment.GetEnvironmentVariable("AGGREGATOR_CLIENTSECRET") ?? string.Empty,
                TenantId = Environment.GetEnvironmentVariable("AGGREGATOR_TENANTID") ?? string.Empty
            };
            _ = azData.Save();
            // now check for validity
            context.Logger.WriteInfo("Connecting to Azure...");
            var azure = azData.Logon();
            if (azure == null)
            {
                context.Logger.WriteError("Invalid azure credentials");
                return ExitCodes.InvalidArguments;
            }
            // FIX #60: call some read API to validate parameters
            try
            {
                await azure.Subscriptions.ListAsync(false, cancellationToken);
            }
            catch (Exception ex)
            {
                int nl = ex.Message.IndexOf(Environment.NewLine);
                string m = nl != -1 ? ex.Message.Remove(nl) : ex.Message;
                context.Logger.WriteError("Invalid azure credentials: " + m);
                return ExitCodes.InvalidArguments;
            }

            var data = new DevOpsLogon()
            {
                Url = Environment.GetEnvironmentVariable("AGGREGATOR_AZDO_URL") ?? string.Empty,
                Mode = (DevOpsTokenType) Enum.Parse(typeof(DevOpsTokenType), 
                        Environment.GetEnvironmentVariable("AGGREGATOR_AZDO_MODE") ?? "PAT"),
                Token = Environment.GetEnvironmentVariable("AGGREGATOR_AZDO_TOKEN") ?? string.Empty,
            };
            _ = data.Save();
            // now check for validity
            context.Logger.WriteInfo($"Connecting to Azure DevOps using {data.Mode} credential...");
            var devops = await data.LogonAsync(cancellationToken);
            if (devops == null)
            {
                context.Logger.WriteError("Invalid Azure DevOps credentials");
                return ExitCodes.InvalidArguments;
            }

            return ExitCodes.Success;
        }
    }
}

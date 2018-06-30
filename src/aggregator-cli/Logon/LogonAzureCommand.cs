using CommandLine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace aggregator.cli
{
    [Verb("logon.azure", HelpText = "Logon into Azure.")]
    class LogonAzureCommand : CommandBase
    {
        [Option('s', "subscription", Required = true, HelpText = "Subscription Id.")]
        public string SubscriptionId { get; set; }
        [Option('c', "client", Required = true, HelpText = "Client Id.")]
        public string ClientId { get; set; }
        [Option('p', "password", Required = true, HelpText = "Client secret.")]
        public string ClientSecret { get; set; }
        [Option('t', "tenant", Required = true, HelpText = "Tenant Id.")]
        public string TenantId { get; set; }

        internal override async Task<int> RunAsync()
        {
            var data = new AzureLogon()
            {
                SubscriptionId = this.SubscriptionId,
                ClientId = this.ClientId,
                ClientSecret = this.ClientSecret,
                TenantId = this.TenantId
            };
            string path = data.Save();
            // now check for validity
            var azure = data.LogonAsync();
            if (azure == null)
            {
                WriteError("Invalid azure credentials");
                return 2;
            }
            return 0;
        }
    }
}

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;

namespace aggregator.cli
{
    [Verb("logon.azure", HelpText = "Logon into Azure.")]
    class LogonAzureCommand : CommandBase
    {
        [Option('u', "user", Required = true, HelpText = "Username to connect.")]
        public string Username { get; set; }
        [Option('p', "password", Required = true, HelpText = "Password.")]
        public string Password { get; set; }
        [Option('c', "client", Required = true, HelpText = "Client Id.")]
        public string ClientId { get; set; }
        [Option('t', "tenant", Required = true, HelpText = "Tenant Id.")]
        public string TenantId { get; set; }

        override internal int Run()
        {
            return 2;
        }
    }
}

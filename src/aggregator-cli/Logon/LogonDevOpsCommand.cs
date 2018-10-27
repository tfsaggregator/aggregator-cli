using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{

    [Verb("logon.ado", HelpText = "Logon into Azure DevOps.")]
    class LogonDevOpsCommand : CommandBase
    {
        [Option('u', "url", Required = true, HelpText = "Account/server URL, e.g. myaccount.visualstudio.com .")]
        public string Url { get; set; }

        [Option('m', "mode", Required = true, HelpText = "Logon mode (valid modes: PAT).")]
        public DevOpsTokenType Mode { get; set; }

        [Option('t', "token", SetName = "PAT", HelpText = "Azure DevOps Personal Authentication Token.")]
        public string Token { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context.Build();


            var data = new DevOpsLogon()
            {
                Url = this.Url,
                Mode = this.Mode,
                Token = this.Token
            };
            string path = data.Save();
            // now check for validity
            context.Logger.WriteInfo($"Connecting to Azure DevOps using {Mode} credential...");
            var devops = await data.LogonAsync();
            if (devops == null)
            {
                context.Logger.WriteError("Invalid Azure DevOps credentials");
                return 2;
            }
            return 0;
        }
    }
}

using CommandLine;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace aggregator.cli
{
    public enum VstsLogonMode
    {
        // HACK: CommandLineParser is case sensitive, sigh!
        integrated = 0,
        Integrated = integrated,
        pat = 1,
        PAT = pat,
    }

    [Verb("logon.vsts", HelpText = "Logon into Visual Studio Team Services.")]
    class LogonVstsCommand : CommandBase
    {
        [Option('u', "url", Required = true, HelpText = "Account/server URL, e.g. myaccount.visualstudio.com .")]
        public string Url { get; set; }

        [Option('m', "mode", Required = true, HelpText = "Logon mode.")]
        public VstsLogonMode Mode { get; set; }

        [Option('t', "token", SetName = "PAT", HelpText = "VSTS Personal Authentication Token.")]
        public string Token { get; set; }

        internal override async Task<int> RunAsync()
        {
            var context = await Context.Build();


            var data = new VstsLogon()
            {
                Url = this.Url,
                Mode = this.Mode,
                Token = this.Token
            };
            string path = data.Save();
            // now check for validity
            var vsts = await data.LogonAsync();
            if (vsts == null)
            {
                context.Logger.WriteError("Invalid VSTS credentials");
                return 2;
            }
            return 0;
        }
    }
}

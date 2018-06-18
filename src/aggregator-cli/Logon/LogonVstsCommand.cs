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
        Integrated = 0,
        pat = 1,
        PAT = 1,
    }

    [Verb("logon.vsts", HelpText = "Logon into Visual Studio Team Services.")]
    class LogonVstsCommand : CommandBase
    {
        [Option('m', "mode", Required = true, HelpText = "Logon mode.")]
        public VstsLogonMode Mode { get; set; }

        [Option('t', "token", SetName = "PAT", HelpText = "VSTS Personal Authentication Token.")]
        public string Token { get; set; }

        internal override Task<int> RunAsync()
        {
            return Task.Run(() => 2);
        }
    }
}

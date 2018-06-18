using CommandLine;
using System;

namespace aggregator.cli
{
    class Program
    {
        /*
- configure.instance
- unistall.instance
- configure.rule
- remove.rule
- list.mappings
- map.rule
- unmap.rule
- run.local
         */
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<
                LogonAzureCommand, LogonVstsCommand,
                ListInstancesCommand, InstallInstanceCommand,
                ListRulesCommand, AddRuleCommand
                >(args)
                .MapResult(
                    (LogonAzureCommand cmd) => cmd.Run(),
                    (LogonVstsCommand cmd) => cmd.Run(),
                    (ListInstancesCommand cmd) => cmd.Run(),
                    (InstallInstanceCommand cmd) => cmd.Run(),
                    (ListRulesCommand cmd) => cmd.Run(),
                    (AddRuleCommand cmd)=> cmd.Run(),
                    errs => 1);
        }
    }
}

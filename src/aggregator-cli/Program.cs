using CommandLine;
using System;

namespace aggregator.cli
{
    class Program
    {
        /*
- configure.instance
- run.local
         */
        static int Main(string[] args)
        {
            int rc = Parser.Default.ParseArguments<
                LogonAzureCommand, LogonVstsCommand,
                ListInstancesCommand, InstallInstanceCommand, UninstallInstanceCommand,
                ListRulesCommand, AddRuleCommand, ConfigureRuleCommand,
                ListMappingsCommand, MapRuleCommand, UnmapRuleCommand
                >(args)
                .MapResult(
                    (LogonAzureCommand cmd) => cmd.Run(),
                    (LogonVstsCommand cmd) => cmd.Run(),
                    (ListInstancesCommand cmd) => cmd.Run(),
                    (InstallInstanceCommand cmd) => cmd.Run(),
                    (UninstallInstanceCommand cmd) => cmd.Run(),
                    (ListRulesCommand cmd) => cmd.Run(),
                    (AddRuleCommand cmd) => cmd.Run(),
                    (ConfigureRuleCommand cmd) => cmd.Run(),
                    (ListMappingsCommand cmd) => cmd.Run(),
                    (MapRuleCommand cmd) => cmd.Run(),
                    (UnmapRuleCommand cmd) => cmd.Run(),
                    errs => 1);
            return rc;
        }
    }
}

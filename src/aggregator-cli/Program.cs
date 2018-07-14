using CommandLine;
using System;

namespace aggregator.cli
{
    /*
    Ideas for verbs and options:

    add.rule --verbose --instance INSTANCE --name RULE --file FILE --slot SLOT
    configure.instance --swap SLOT
    invoke.rule --verbose --instance INSTANCE --rule RULE --event EVENT --workItemId WORK_ITEM_ID --doNotFakeVsts
    invoke.rule --verbose --local --ruleSource FILE --event EVENT --workItemId WORK_ITEM_ID
    logon.vsts --url URL --mode MODE --token TOKEN --secondary

    */
    class Program
    {
        static int Main(string[] args)
        {
            var parser = new Parser(settings =>
            {
                settings.CaseSensitive = false;
                settings.CaseInsensitiveEnumValues = true;
            });
            int rc = parser.ParseArguments<
                LogonAzureCommand, LogonVstsCommand,
                ListInstancesCommand, InstallInstanceCommand, UninstallInstanceCommand,
                ListRulesCommand, AddRuleCommand, RemoveRuleCommand, ConfigureRuleCommand,
                ListMappingsCommand, MapRuleCommand, UnmapRuleCommand
                >(args)
                .MapResult(
                    (LogonAzureCommand cmd) => cmd.Run(),
                    (LogonVstsCommand cmd) => cmd.Run(),
                    (ListInstancesCommand cmd) => cmd.Run(),
                    (InstallInstanceCommand cmd) => cmd.Run(),
                    (UninstallInstanceCommand cmd) => cmd.Run(),
                    (ConfigureInstanceCommand cmd) => cmd.Run(),
                    (ListRulesCommand cmd) => cmd.Run(),
                    (AddRuleCommand cmd) => cmd.Run(),
                    (RemoveRuleCommand cmd) => cmd.Run(),
                    (ConfigureRuleCommand cmd) => cmd.Run(),
                    (ListMappingsCommand cmd) => cmd.Run(),
                    (MapRuleCommand cmd) => cmd.Run(),
                    (UnmapRuleCommand cmd) => cmd.Run(),
                    errs => 1);
            return rc;
        }
    }
}

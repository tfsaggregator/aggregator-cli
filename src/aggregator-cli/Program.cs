using CommandLine;
using CommandLine.Text;
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
                // fails see https://github.com/commandlineparser/commandline/issues/198
                settings.CaseInsensitiveEnumValues = true;
            });
            var parserResult = parser.ParseArguments(args,
                typeof(LogonAzureCommand), typeof(LogonVstsCommand),
                typeof(ListInstancesCommand), typeof(InstallInstanceCommand), typeof(UninstallInstanceCommand),
                typeof(ListRulesCommand), typeof(AddRuleCommand), typeof(RemoveRuleCommand), typeof(ConfigureRuleCommand),
                typeof(ListMappingsCommand), typeof(MapRuleCommand), typeof(UnmapRuleCommand)
                );
            int rc = -1;
            parserResult
                .WithParsed<LogonAzureCommand>(cmd => rc = cmd.Run())
                .WithParsed<LogonVstsCommand>(cmd => rc = cmd.Run())
                .WithParsed<ListInstancesCommand>(cmd => rc = cmd.Run())
                .WithParsed<InstallInstanceCommand>(cmd => rc = cmd.Run())
                .WithParsed<UninstallInstanceCommand>(cmd => rc = cmd.Run())
                .WithParsed<ConfigureInstanceCommand>(cmd => rc = cmd.Run())
                .WithParsed<ListRulesCommand>(cmd => rc = cmd.Run())
                .WithParsed<AddRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<RemoveRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<ConfigureRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<ListMappingsCommand>(cmd => rc = cmd.Run())
                .WithParsed<MapRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<UnmapRuleCommand>(cmd => rc = cmd.Run())
                .WithNotParsed(errs =>
                {
                    var helpText = HelpText.AutoBuild(parserResult);
                    Console.Error.Write(helpText);
                    rc = 1;
                });
            return rc;
        }
    }
}

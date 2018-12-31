using CommandLine;
using CommandLine.Text;
using System;

namespace aggregator.cli
{
    /*
    Ideas for verbs and options:

    logon.azure --subscription SUBSCRIPTION_ID --client CLIENT_ID --password CLIENT_PASSWORD --tenant TENANT_ID  --instance INSTANCE --resourceGroup RESOURCEGROUP
        default values for instance and resource group
    logon.ado --url URL --mode MODE --token TOKEN --project PROJECT
        default value for project

    logon.ado --url URL --mode MODE --token TOKEN --slot SLOT
        to use different credentials

    configure.instance --slot SLOT --swap --avzone ZONE
        add a deployment slot with the option to specify an availability zone, the swap option will set the new slot as primary
    configure.instance --listOutboundIPs
        use `azure.AppServices.WebApps.GetByResourceGroup(instance.ResourceGroupName,instance.FunctionAppName).OutboundIPAddresses`
    configure.instance --MSI
        support for Managed service identity when AzDO is backed by AAD

    configure.rule --verbose --instance INSTANCE --name RULE --file FILE --slot SLOT
        change rule code on a single deployment slot
        
    invoke.rule --verbose --dryrun --instance INSTANCE --rule RULE --event EVENT --workItemId WORK_ITEM_ID --slot SLOT
        emulates the event on the rule

    */
    public class Program
    {
        public static int Main(string[] args)
        {
            var save = Console.ForegroundColor;
            Console.CancelKeyPress += delegate {
                // call methods to clean up
                Console.ForegroundColor = save;
            };

            var parser = new Parser(settings =>
            {
                settings.CaseSensitive = false;
                // fails see https://github.com/commandlineparser/commandline/issues/198
                settings.CaseInsensitiveEnumValues = true;
            });
            var types = new Type[] {
                typeof(LogonAzureCommand), typeof(LogonDevOpsCommand),
                typeof(ListInstancesCommand), typeof(InstallInstanceCommand), typeof(UninstallInstanceCommand),
                typeof(ConfigureInstanceCommand), typeof(StreamLogsCommand),
                typeof(ListRulesCommand), typeof(AddRuleCommand), typeof(RemoveRuleCommand),
                typeof(ConfigureRuleCommand), typeof(UpdateRuleCommand), typeof(InvokeRuleCommand),
                typeof(ListMappingsCommand), typeof(MapRuleCommand), typeof(UnmapRuleCommand)
            };
            var parserResult = parser.ParseArguments(args, types);
            int rc = -1;
            parserResult
                .WithParsed<LogonAzureCommand>(cmd => rc = cmd.Run())
                .WithParsed<LogonDevOpsCommand>(cmd => rc = cmd.Run())
                .WithParsed<ListInstancesCommand>(cmd => rc = cmd.Run())
                .WithParsed<InstallInstanceCommand>(cmd => rc = cmd.Run())
                .WithParsed<UninstallInstanceCommand>(cmd => rc = cmd.Run())
                .WithParsed<ConfigureInstanceCommand>(cmd => rc = cmd.Run())
                .WithParsed<ListRulesCommand>(cmd => rc = cmd.Run())
                .WithParsed<AddRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<RemoveRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<ConfigureRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<StreamLogsCommand>(cmd => rc = cmd.Run())
                .WithParsed<UpdateRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<InvokeRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<ListMappingsCommand>(cmd => rc = cmd.Run())
                .WithParsed<MapRuleCommand>(cmd => rc = cmd.Run())
                .WithParsed<UnmapRuleCommand>(cmd => rc = cmd.Run())
                .WithNotParsed(errs =>
                {
                    var helpText = HelpText.AutoBuild(parserResult);
                    Console.Error.Write(helpText);
                    rc = 1;
                });
            Console.ForegroundColor = save;
            return rc;
        }
    }
}

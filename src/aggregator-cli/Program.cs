﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using aggregator.cli.Instances;
using CommandLine;
using CommandLine.Text;

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
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var mainTimer = new Stopwatch();
            mainTimer.Start();

            var save = Console.ForegroundColor;

            Telemetry.TrackEvent("CLI Start");
            var tempLogger = new ConsoleLogger(false);

            using var cancellationTokenSource = new CancellationTokenSource();
            void cancelEventHandler(object sender, ConsoleCancelEventArgs e)
            {
                // call methods to clean up
                Console.ForegroundColor = save;
                if (!cancellationTokenSource.IsCancellationRequested)
                {
                    cancellationTokenSource.Cancel();
                }
            }
            Console.CancelKeyPress += cancelEventHandler;
            var cancellationToken = cancellationTokenSource.Token;

            bool versionCheckEnabled = !EnvironmentVariables.GetAsBool("AGGREGATOR_NEW_VERSION_CHECK_DISABLED", false);
            if (versionCheckEnabled)
            {
                var verChecker = new FunctionRuntimePackage(tempLogger);
                (bool upgrade, string newversion) = await verChecker.IsCliUpgradable();
                if (upgrade)
                {
                    // bug user
                    tempLogger.WriteWarning($"A new version ({newversion}) of Aggregator CLI is available, please upgrade.");
                }
            }

            var parser = new Parser(settings =>
            {
                settings.CaseSensitive = false;
                    // fails see https://github.com/commandlineparser/commandline/issues/198
                    settings.CaseInsensitiveEnumValues = true;
            });
            var types = new Type[]
            {
                    typeof(CreateTestCommand), typeof(UpdateTestCommand), typeof(CleanupTestCommand),
                    typeof(LogonAzureCommand), typeof(LogonDevOpsCommand), typeof(LogoffCommand), typeof(LogonEnvCommand),
                    typeof(ListInstancesCommand), typeof(InstallInstanceCommand), typeof(UpdateInstanceCommand),
                    typeof(UninstallInstanceCommand), typeof(ConfigureInstanceCommand), typeof(StreamLogsCommand),
                    typeof(ListRulesCommand), typeof(AddRuleCommand), typeof(RemoveRuleCommand),
                    typeof(ConfigureRuleCommand), typeof(UpdateRuleCommand), typeof(InvokeRuleCommand),
                    typeof(ListMappingsCommand), typeof(MapRuleCommand), typeof(UnmapRuleCommand), typeof(UpdateMappingsCommand),
                    typeof(MapLocalRuleCommand)
            };
            var parserResult = parser.ParseArguments(args, types);
            (bool success, int returnCode) returnValue = (false, ExitCodes.Unexpected);
            parserResult
                .WithParsed<CreateTestCommand>(cmd => returnValue = cmd.Run(cancellationToken, ReturnType.SuccessBooleanPlusIntegerValue))
                .WithParsed<UpdateTestCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<CleanupTestCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<LogonAzureCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<LogonDevOpsCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<LogoffCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<LogonEnvCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<ListInstancesCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<InstallInstanceCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<UpdateInstanceCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<UninstallInstanceCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<ConfigureInstanceCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<ListRulesCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<AddRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<RemoveRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<ConfigureRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<StreamLogsCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<UpdateRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<InvokeRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<ListMappingsCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<MapRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<UnmapRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<UpdateMappingsCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithParsed<MapLocalRuleCommand>(cmd => returnValue = cmd.Run(cancellationToken))
                .WithNotParsed(errs =>
                {
                    var helpText = HelpText.AutoBuild(parserResult);
                    Console.Error.Write(helpText);
                    returnValue = (false, ExitCodes.InvalidArguments);
                });

            int rc = returnValue.returnCode;
            mainTimer.Stop();
            Telemetry.TrackEvent("CLI End", null,
                new Dictionary<string, double> {
                        { "RunDuration", mainTimer.ElapsedMilliseconds }
                });
            tempLogger.WriteInfo($"Exiting with code {rc}");

            Telemetry.Shutdown();

            Console.ForegroundColor = save;
            Console.CancelKeyPress -= cancelEventHandler;
            return rc;
        }
    }
}

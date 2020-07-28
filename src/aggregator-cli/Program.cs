﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
        public static int Main(string[] args)
        {
            var mainTimer = new Stopwatch();
            mainTimer.Start();

            var save = Console.ForegroundColor;

            Telemetry.Current.TrackEvent("ApplicationStart");

            using (var cancellationTokenSource = new CancellationTokenSource())
            {

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

                var parser = new Parser(settings =>
                {
                    settings.CaseSensitive = false;
                    // fails see https://github.com/commandlineparser/commandline/issues/198
                    settings.CaseInsensitiveEnumValues = true;
                });
                var types = new Type[]
                {
                    typeof(CreateTestCommand), typeof(CleanupTestCommand),
                    typeof(LogonAzureCommand), typeof(LogonDevOpsCommand), typeof(LogoffCommand), typeof(LogonEnvCommand),
                    typeof(ListInstancesCommand), typeof(InstallInstanceCommand), typeof(UpdateInstanceCommand),
                    typeof(UninstallInstanceCommand), typeof(ConfigureInstanceCommand), typeof(StreamLogsCommand),
                    typeof(ListRulesCommand), typeof(AddRuleCommand), typeof(RemoveRuleCommand),
                    typeof(ConfigureRuleCommand), typeof(UpdateRuleCommand), typeof(InvokeRuleCommand),
                    typeof(ListMappingsCommand), typeof(MapRuleCommand), typeof(UnmapRuleCommand)
                };
                var parserResult = parser.ParseArguments(args, types);
                int rc = ExitCodes.Unexpected;
                var cancellationToken = cancellationTokenSource.Token;
                parserResult
                    .WithParsed<CreateTestCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<CleanupTestCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<LogonAzureCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<LogonDevOpsCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<LogoffCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<LogonEnvCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<ListInstancesCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<InstallInstanceCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<UpdateInstanceCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<UninstallInstanceCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<ConfigureInstanceCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<ListRulesCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<AddRuleCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<RemoveRuleCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<ConfigureRuleCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<StreamLogsCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<UpdateRuleCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<InvokeRuleCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<ListMappingsCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<MapRuleCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithParsed<UnmapRuleCommand>(cmd => rc = cmd.Run(cancellationToken))
                    .WithNotParsed(errs =>
                    {
                        var helpText = HelpText.AutoBuild(parserResult);
                        Console.Error.Write(helpText);
                        rc = ExitCodes.InvalidArguments;
                    });


                mainTimer.Stop();
                Telemetry.Current.TrackEvent("ApplicationEnd", null,
                    new Dictionary<string, double> {
                        { "ApplicationDuration", mainTimer.ElapsedMilliseconds }
                    });
                if (Telemetry.Current != null)
                {
                    // before exit, flush the remaining data
                    Telemetry.Current.Flush();
                    // flush is not blocking when not using InMemoryChannel so wait a bit. There is an active issue regarding the need for `Sleep`/`Delay`
                    // which is tracked here: https://github.com/microsoft/ApplicationInsights-dotnet/issues/407
                    System.Threading.Tasks.Task.Delay(5000).Wait();
                }


                Console.ForegroundColor = save;
                Console.CancelKeyPress -= cancelEventHandler;
                return rc;
            }
        }
    }
}

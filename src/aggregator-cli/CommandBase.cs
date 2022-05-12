using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.Services.Common;

namespace aggregator.cli
{
    internal enum ReturnType
    {
        ExitCode,
        SuccessBooleanPlusIntegerValue
    }

    abstract class CommandBase
    {
        [ShowInTelemetry]
        [Option('v', "verbose", Default = false, HelpText = "Prints all messages to standard output.")]
        public bool Verbose { get; set; }

        [ShowInTelemetry(TelemetryDisplayMode.Presence)]
        [Option("namingTemplate", Default = "", HelpText = "Define template-set for generating names of Azure Resources.")]
        public string NamingTemplate { get; set; }

        protected ContextBuilder Context => new(Logger, this.NamingTemplate);

        internal ILogger Logger { get; private set; }

        internal abstract Task<int> RunAsync(CancellationToken cancellationToken);

        // virtual because it is the exception not the norm, don't want to force every command with a dummy implementation
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        internal virtual async Task<(bool success, int returnCode)> RunWithReturnAsync(CancellationToken cancellationToken)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            return (true, 0);
        }

        internal (bool success, int returnCode) Run(CancellationToken cancellationToken, ReturnType returnMode = ReturnType.ExitCode)
        {
            var thisCommandName = this.GetType().GetCustomAttribute<VerbAttribute>().Name;

            var eventStart = new EventTelemetry
            {
                // use Reflection to capture Command and Options
                Name = $"{thisCommandName} Start"
            };
            this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).ForEach(
                prop =>
                {
                    var show = prop.GetCustomAttribute<ShowInTelemetryAttribute>();
                    if (show != null)
                    {
                        var attr = prop.GetCustomAttribute<OptionAttribute>();
                        if (attr != null)
                        {
                            eventStart.Properties[attr.LongName] = TelemetryFormatter.Format(show.Mode, prop.GetValue(this));
                        }
                    }
                });
            Telemetry.TrackEvent(eventStart);

            Logger = new ConsoleLogger(Verbose);
            try
            {
                SayHello();
                var packedResult = returnMode switch
                {
                    ReturnType.ExitCode => CoreRunReturningExitCode(thisCommandName, cancellationToken),
                    ReturnType.SuccessBooleanPlusIntegerValue => CoreRunReturningSuccess(thisCommandName, cancellationToken),
                    _ => throw new NotImplementedException($"Fix code and add {returnMode} to ReturnType enum."),
                };

                var eventEnd = new EventTelemetry
                {
                    Name = $"{thisCommandName} End"
                };
                // SuccessBooleanPlusIntegerValue is used only in testing scenarios
                eventEnd.Properties["exitCode"] = packedResult.returnCode.ToString(CultureInfo.InvariantCulture);
                Telemetry.TrackEvent(eventEnd);

                return packedResult;
            }
            catch (Exception ex)
            {
                Logger.WriteError(
                    ex.InnerException == null
                    ? ex.Message
                    : ex.InnerException.Message
                    );
                Telemetry.TrackException(ex);
                return (false, ExitCodes.Unexpected);
            }
        }

        private (bool success, int returnCode) CoreRunReturningSuccess(string thisCommandName, CancellationToken cancellationToken)
        {
            (bool success, int returnCode) packedResult;
            var t2 = RunWithReturnAsync(cancellationToken);
            t2.Wait(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            packedResult = t2.Result;
            if (packedResult.success)
            {
                Logger.WriteSuccess($"{thisCommandName} Succeeded");
            }
            else
            {
                Logger.WriteError($"{thisCommandName} Failed!");
            }

            return packedResult;
        }

        private (bool success, int returnCode) CoreRunReturningExitCode(string thisCommandName, CancellationToken cancellationToken)
        {
            (bool success, int returnCode) packedResult;
            var t = RunAsync(cancellationToken);
            t.Wait(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            packedResult = (default, t.Result);
            if (packedResult.returnCode == ExitCodes.Success)
            {
                packedResult.success = true;
                Logger.WriteSuccess($"{thisCommandName} Succeeded");
            }
            else if (packedResult.returnCode == ExitCodes.NotFound)
            {
                packedResult.success = true;
                Logger.WriteWarning($"{thisCommandName} Item Not found");
            }
            else
            {
                packedResult.success = false;
                Logger.WriteError($"{thisCommandName} Failed!");
            }

            return packedResult;
        }

        private void SayHello()
        {
            var title = GetCustomAttribute<AssemblyTitleAttribute>();
            var config = GetCustomAttribute<AssemblyConfigurationAttribute>();
            var fileVersion = GetCustomAttribute<AssemblyFileVersionAttribute>();
            var infoVersion = GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var copyright = GetCustomAttribute<AssemblyCopyrightAttribute>();

            // Hello World
            Logger.WriteInfo($"{title.Title} v{infoVersion.InformationalVersion} (build: {fileVersion.Version} {config.Configuration}) (c) {copyright.Copyright}");
        }

        private static T GetCustomAttribute<T>()
            where T : Attribute
        {
            return Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }
    }
}

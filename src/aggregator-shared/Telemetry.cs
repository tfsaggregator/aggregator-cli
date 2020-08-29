using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace aggregator
{

    public static class Telemetry
    {
        private const string applicationInsightsKey = "b5896615-5bbe-4cd8-bbb8-9bdeb59463ba";

        private static TelemetryClient telemetryClient;
        private static ITelemetrySettings telemetrySettings;

        private static TelemetryClient Current
        {
            get
            {
                if (telemetryClient == null)
                {
                    InitializeTelemetry();
                }
                return telemetryClient;
            }
        }

        public static bool Enabled { get; private set; }
        private static bool WaitAtShutdown { get; set; }

        public static void InitializeTelemetry()
        {
            string dll = Assembly.GetEntryAssembly().GetName().Name;
            if (dll == "aggregator-cli")
            {
                telemetrySettings = CliTelemetrySettings.Get();
                WaitAtShutdown = false;
            }
            else if (dll == "aggregator-host")
            {
                telemetrySettings = HostTelemetrySettings.Get();
                WaitAtShutdown = true;
            }
            else
            {
                // another option would be to disable telemetry
                throw new InvalidOperationException("Telemetry can be used only in CLI or Host");
            }

            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = applicationInsightsKey;
            // use default InMemory channel, if there are network issues, who cares
            configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
#if DEBUG
            configuration.TelemetryChannel.DeveloperMode = true;
#endif

            Enabled = telemetrySettings.Enabled;

            if (Enabled)
            {
                telemetryClient = new TelemetryClient(configuration);
                // this is portable
                telemetryClient.Context.User.Id = telemetrySettings.UserId;
                // this is time based, cannot be a new one at each run
                telemetryClient.Context.Session.Id = telemetrySettings.SessionId;
                telemetryClient.Context.Session.IsFirst = telemetrySettings.IsNewSession;
                ///Environment.Is64BitOperatingSystem;
                telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
                telemetryClient.Context.Device.Id = telemetrySettings.DeviceId;
                telemetryClient.Context.Component.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Trace.WriteLine(string.Format("SessionID: {0}", telemetryClient.Context.Session.Id));
            }
        }

        public static void TrackEvent(EventTelemetry ev)
        {
            if (Enabled)
            {
                Current.TrackEvent(ev);
            }
        }

        public static void TrackEvent(string eventName, IDictionary<string, string> properties = null, IDictionary<string, double> metrics = null)
        {
            if (Enabled)
            {
                Current.TrackEvent(eventName, properties, metrics);
            }
        }

        public static void TrackException(Exception ex)
        {
            if (Enabled)
            {
                Current.TrackException(ex);
            }
        }

        public static void Shutdown()
        {
            if (Enabled)
            {
                telemetrySettings.Save();
                // before exit, flush the remaining data
                telemetryClient.Flush();
                // flush is not blocking when not using InMemoryChannel so wait a bit. There is an active issue regarding the need for `Sleep`/`Delay`
                // which is tracked here: https://github.com/microsoft/ApplicationInsights-dotnet/issues/407
                if (WaitAtShutdown)
                {
                    System.Threading.Tasks.Task.Delay(5000).Wait();
                }
            }
        }

    }
}

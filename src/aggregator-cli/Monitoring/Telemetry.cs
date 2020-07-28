using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;

namespace aggregator.cli
{
    public static class Telemetry
    {
        // this is the DEV key
        private const string applicationInsightsKey = "7d9e8f41-c508-4e2c-9851-e1a513ad6587";
        private static TelemetryClient telemetryClient;

        public static TelemetryClient Current
        {
            get
            {
                if (telemetryClient == null)
                {
                    InitializeTelemetry();
                }
                return telemetryClient;
                // No change
            }
        }

        public static void InitializeTelemetry()
        {
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = applicationInsightsKey;
            telemetryClient = new TelemetryClient(configuration);
            // this is WIN only
            telemetryClient.Context.User.Id = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            // this is time based, cannot be a new one at each run
            telemetryClient.Context.Session.Id = Guid.NewGuid().ToString();
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            telemetryClient.Context.Component.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Trace.WriteLine(string.Format("SessionID: {0}", telemetryClient.Context.Session.Id));
            AddModules();
            telemetryClient.TrackEvent("ApplicationStarted");
        }

        public static void AddModules()
        {
        }
    }
}

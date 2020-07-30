using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Newtonsoft.Json;

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
            var settings = TelemetrySettings.Get();

            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = applicationInsightsKey;
            configuration.TelemetryChannel = new Microsoft.ApplicationInsights.WindowsServer.TelemetryChannel.ServerTelemetryChannel();
            configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
#if DEBUG
            configuration.TelemetryChannel.DeveloperMode = true;
#endif

            telemetryClient = new TelemetryClient(configuration);
            // this is portable
            telemetryClient.Context.User.Id = settings.UserId;
            // this is time based, cannot be a new one at each run
            telemetryClient.Context.Session.Id = settings.SessionId;
            telemetryClient.Context.Session.IsFirst = settings.IsNewSession;
            ///Environment.Is64BitOperatingSystem;
            telemetryClient.Context.Device.OperatingSystem = Environment.OSVersion.ToString();
            telemetryClient.Context.Device.Id = settings.DeviceId;
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

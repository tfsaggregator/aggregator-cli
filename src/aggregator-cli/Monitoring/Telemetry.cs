using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.TeamFoundation.WorkItemTracking.Process.WebApi.Models;
using Newtonsoft.Json;

namespace aggregator.cli
{

    public static class Telemetry
    {
        private const string applicationInsightsKey = "b5896615-5bbe-4cd8-bbb8-9bdeb59463ba";

        private static TelemetryClient telemetryClient;
        private static TelemetrySettings telemetrySettings;

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
            telemetrySettings = TelemetrySettings.Get();

            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = applicationInsightsKey;
            // use default InMemory channel, if there are network issues, who cares
            configuration.TelemetryChannel.DeveloperMode = Debugger.IsAttached;
#if DEBUG
            configuration.TelemetryChannel.DeveloperMode = true;
#endif

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
            telemetryClient.TrackEvent("ApplicationStarted");
        }

        public static void Shutdown()
        {
            telemetrySettings.Save();
            // before exit, flush the remaining data
            telemetryClient.Flush();
            // flush is not blocking when not using InMemoryChannel so wait a bit. There is an active issue regarding the need for `Sleep`/`Delay`
            // which is tracked here: https://github.com/microsoft/ApplicationInsights-dotnet/issues/407
            //System.Threading.Tasks.Task.Delay(5000).Wait();
        }
    }
}

using System;
using System.IO;
using aggregator.cli;
using Newtonsoft.Json;

namespace aggregator
{
    /// <summary>
    /// Per-machine and per-user data across CLI invocation
    /// </summary>
    class CliTelemetrySettings : ITelemetrySettings
    {
        private static string SettingsFileName => LocalAppData.GetPath("telemetry.settings.json");

        private static readonly TimeSpan MaxSessionDuration = new TimeSpan(0, 0, 15, 0);

        [JsonProperty]
        private DateTime LastUpdate { get; set; }

        [JsonIgnore]
        public bool Enabled { get; private set; }

        public static ITelemetrySettings Get()
        {
            CliTelemetrySettings s;
            if (File.Exists(SettingsFileName))
            {
                s = JsonConvert.DeserializeObject<CliTelemetrySettings>(
                    File.ReadAllText(SettingsFileName));

                bool sessionExpired = DateTime.Now.Subtract(MaxSessionDuration) > s.LastUpdate;
                if (sessionExpired)
                {
                    s.SessionId = Guid.NewGuid().ToString();
                    s.IsNewSession = true;
                }
                else
                {
                    s.IsNewSession = false;
                }
            }
            else
            {
                s = new CliTelemetrySettings()
                {
                    SessionId = Guid.NewGuid().ToString(),
                    IsNewSession = true,
                    // I assume these won't change during a session :-)
                    TrueUserId = Environment.UserName,
                    TrueDeviceId = Environment.MachineName,
                    UserId = GetHash($"{Environment.UserName}@{Environment.MachineName}"),
                    DeviceId = GetHash(Environment.MachineName),
                };
            }
            s.Enabled = !EnvironmentVariables.GetAsBool("AGGREGATOR_TELEMETRY_DISABLED", false);

            return s;
        }

        private static string GetHash(string text)
        {
#pragma warning disable S4790 // Make sure that hashing data is safe here
            using var sha = new System.Security.Cryptography.SHA256Managed();
            byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] hash = sha.ComputeHash(textData);
            return BitConverter.ToString(hash).Replace("-", String.Empty);
#pragma warning restore S4790 // Make sure that hashing data is safe here
        }

        public bool Save()
        {
            try
            {
                this.LastUpdate = DateTime.Now;
                File.WriteAllText(SettingsFileName, JsonConvert.SerializeObject(this));
                return true;
            }
            catch (Exception)
            {
                // worst case we reset the settings to default
                return false;
            }

        }

        public string SessionId { get; set; }
        public bool IsNewSession { get; set; }
        // TrueXXX is in troubleshooting: telemetry sees the hashed value but the user can trace back to the original value looking at the settings file
#pragma warning disable IDE0052 // Remove unread private members
        [JsonProperty]
        private string TrueUserId { get; set; }
        [JsonProperty]
        private string TrueDeviceId { get; set; }
#pragma warning restore IDE0052 // Remove unread private members
        public string UserId { get; set; }
        public string DeviceId { get; set; }
    }
}

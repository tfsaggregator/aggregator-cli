using System;
using System.IO;
using Newtonsoft.Json;

namespace aggregator.cli
{
    /// <summary>
    /// Per-machine and per-user data across CLI invocation
    /// </summary>
    class TelemetrySettings
    {
        private static string SettingsFileName => LocalAppData.GetPath("telemetry.settings.json");

        private static readonly TimeSpan MaxSessionDuration = new TimeSpan(0, 0, 15, 0);

        [JsonProperty]
        private DateTime LastUpdate { get; set; }

        public static TelemetrySettings Get()
        {
            TelemetrySettings s;
            if (File.Exists(SettingsFileName))
            {
                s = JsonConvert.DeserializeObject<TelemetrySettings>(
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
                s = new TelemetrySettings()
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

            return s;
        }

        private static string GetHash(string text)
        {
            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
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
        [JsonProperty]
        private string TrueUserId { get; set; }
        [JsonProperty]
        private string TrueDeviceId { get; set; }
        public string UserId { get; set; }
        public string DeviceId { get; set; }
    }
}

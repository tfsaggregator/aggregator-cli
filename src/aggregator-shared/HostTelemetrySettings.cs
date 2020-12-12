using System;

namespace aggregator
{
    class HostTelemetrySettings : ITelemetrySettings
    {
        public bool Enabled { get; private set; }

        public static ITelemetrySettings Get()
        {
            var s = new HostTelemetrySettings()
            {
                SessionId = Guid.NewGuid().ToString(),
                IsNewSession = true,
                UserId = GetHash($"{Environment.UserName}@{Environment.MachineName}"),
                DeviceId = GetHash(Environment.MachineName),
            };
            s.Enabled = !GetEnvironmentVariableAsBool("AGGREGATOR_TELEMETRY_DISABLED", false);

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
            // not used
            return true;
        }

        private static bool GetEnvironmentVariableAsBool(string varName, bool valueIfMissing = false)
        {
            string str = Environment.GetEnvironmentVariable(varName);
            if (str == null)
                return valueIfMissing;

            bool isTrue;
            switch (str.ToLowerInvariant())
            {
                case "true":
                case "yes":
                case "1":
                    isTrue = true;
                    break;
                case "false":
                case "no":
                case "0":
                    isTrue = false;
                    break;
                default:
                    throw new ArgumentException("Environment variable was not truthy nor falsy", varName);
            }
            return isTrue;
        }

        public string SessionId { get; set; }
        public bool IsNewSession { get; set; }
        public string UserId { get; set; }
        public string DeviceId { get; set; }
    }
}

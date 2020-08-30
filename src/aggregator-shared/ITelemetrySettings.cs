namespace aggregator
{
    internal interface ITelemetrySettings
    {
        public bool Enabled { get; }
        public string SessionId { get; }
        public bool IsNewSession { get; }
        public string UserId { get; }
        public string DeviceId { get; }

        public bool Save();
    }
}

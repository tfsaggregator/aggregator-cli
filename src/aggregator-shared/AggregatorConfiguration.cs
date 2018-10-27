using System;

namespace aggregator
{
    public enum DevOpsTokenType
    {
        Integrated = 0,
        PAT = 1,
    }

    /// <summary>
    /// This class tracks the configuration data that CLI writes and Function runtime reads
    /// </summary>
    public class AggregatorConfiguration
    {
        public AggregatorConfiguration() {}

        static public AggregatorConfiguration Read(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            var ac = new AggregatorConfiguration();
            Enum.TryParse(config["Aggregator_VstsTokenType"], out DevOpsTokenType vtt);
            ac.DevOpsTokenType = vtt;
            ac.DevOpsToken = config["Aggregator_VstsToken"];
            return ac;
        }

        public void Write(Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp)
        {
            webApp
                .Update()
                .WithAppSetting("Aggregator_VstsTokenType", DevOpsTokenType.ToString())
                .WithAppSetting("Aggregator_VstsToken", DevOpsToken)
                .Apply();
        }

        public DevOpsTokenType DevOpsTokenType { get; set; }
        public string DevOpsToken { get; set; }
    }
}

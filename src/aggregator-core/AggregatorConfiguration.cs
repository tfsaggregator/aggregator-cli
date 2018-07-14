using System;

namespace aggregator
{
    public enum VstsTokenType
    {
        Integrated = 0,
        PAT = 1,
    }

    public class AggregatorConfiguration
    {
        public AggregatorConfiguration() {}

        static public AggregatorConfiguration Read(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            var ac = new AggregatorConfiguration();
            VstsTokenType vtt;
            Enum.TryParse<VstsTokenType>(config["Aggregator_VstsTokenType"], out vtt);
            ac.VstsTokenType = vtt;
            ac.VstsToken = config["Aggregator_VstsToken"];
            return ac;
        }

        public void Write(Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp)
        {
            webApp
                .Update()
                .WithAppSetting("Aggregator_VstsTokenType", VstsTokenType.ToString())
                .WithAppSetting("Aggregator_VstsToken", VstsToken)
                .Apply();
        }

        public VstsTokenType VstsTokenType { get; set; }
        public string VstsToken { get; set; }
    }
}

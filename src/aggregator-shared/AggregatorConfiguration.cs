using System;

namespace aggregator
{
    public enum DevOpsTokenType
    {
        Integrated = 0,
        PAT = 1,
    }

    public enum SaveMode
    {
        Default = 0,
        Item = 1,
        Batch = 2,
        TwoPhases = 3
    }

    /// <summary>
    /// This class tracks the configuration data that CLI writes and Function runtime reads
    /// </summary>
    public class AggregatorConfiguration
    {
        public static AggregatorConfiguration Read(Microsoft.Extensions.Configuration.IConfiguration config)
        {
            var ac = new AggregatorConfiguration();
            Enum.TryParse(config["Aggregator_VstsTokenType"], out DevOpsTokenType vtt);
            ac.DevOpsTokenType = vtt;
            ac.DevOpsToken = config["Aggregator_VstsToken"];
            ac.SaveMode = Enum.TryParse(config["Aggregator_SaveMode"], out SaveMode sm)
                ? sm
                : SaveMode.Default;
            ac.DryRun = false;
            return ac;
        }

        public void Write(Microsoft.Azure.Management.AppService.Fluent.IWebApp webApp)
        {
            webApp
                .Update()
                .WithAppSetting("Aggregator_VstsTokenType", DevOpsTokenType.ToString())
                .WithAppSetting("Aggregator_VstsToken", DevOpsToken)
                .WithAppSetting("Aggregator_SaveMode", SaveMode.ToString())
                .Apply();
        }

        public DevOpsTokenType DevOpsTokenType { get; set; }
        public string DevOpsToken { get; set; }
        public SaveMode SaveMode { get; set; }
        public bool DryRun { get; internal set; }
        public bool Impersonate { get; internal set; }
    }
}

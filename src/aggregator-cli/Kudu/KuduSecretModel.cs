using Newtonsoft.Json;

namespace aggregator.cli
{
    public class KuduSecret
    {
        [JsonProperty(PropertyName = "key")]
        public string Key { get; set; }
        [JsonProperty(PropertyName = "trigger_url")]
        public string TriggerUrl { get; set; }
    }
}
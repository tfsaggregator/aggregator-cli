using Newtonsoft.Json;

namespace aggregator.cli
{
    public class KuduFunctionKey
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "value")]
        public string Value { get; set; }
    }

    public class KuduFunctionKeys
    {
        [JsonProperty(PropertyName = "keys")]
        public KuduFunctionKey[] Keys { get; set; }
        // NOT USED links
    }

    public class KuduFunctionBinding
    {
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "direction")]
        public string Direction { get; set; }
        [JsonProperty(PropertyName = "webHookType")]
        public string WebHookType { get; set; }
    }

    public class KuduFunctionConfig
    {
        [JsonProperty(PropertyName = "bindings")]
        public KuduFunctionBinding[] Bindings { get; set; }
        [JsonProperty(PropertyName = "disabled")]
        public bool Disabled { get; set; }
    }

    public class KuduFunction
    {
        [JsonProperty(PropertyName = "url")]
        public string Url { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "config")]
        public KuduFunctionConfig Config { get; set; }
    }
}

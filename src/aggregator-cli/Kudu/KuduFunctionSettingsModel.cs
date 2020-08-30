using System.Collections.Generic;
using Newtonsoft.Json;

namespace aggregator.cli
{

    internal class Binding
    {
        [JsonProperty(PropertyName = "type")]
        public string Type { get; set; }
        [JsonProperty(PropertyName = "direction")]
        public string Direction { get; set; }
        [JsonProperty(PropertyName = "webHookType")]
        public string WebHookType { get; set; }
        [JsonProperty(PropertyName = "name")]
        public string Name { get; set; }
        [JsonProperty(PropertyName = "queueName")]
        public string QueueName { get; set; }
        [JsonProperty(PropertyName = "connection")]
        public string Connection { get; set; }
        [JsonProperty(PropertyName = "accessRights")]
        public string AccessRights { get; set; }
        [JsonProperty(PropertyName = "schedule")]
        public string Schedule { get; set; }
    }

    internal class FunctionSettings
    {
        [JsonProperty(PropertyName = "bindings")]
        public List<Binding> Bindings { get; set; }
        [JsonProperty(PropertyName = "disabled")]
        public bool Disabled { get; set; }
    }
}
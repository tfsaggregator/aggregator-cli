using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;

namespace aggregator.Engine
{
    class JsonPatchOperationConverter : JsonConverter<Microsoft.VisualStudio.Services.WebApi.Patch.Json.JsonPatchOperation>
    {
        public override bool CanRead => false;

        public override JsonPatchOperation ReadJson(JsonReader reader, Type objectType, JsonPatchOperation existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            throw new NotImplementedException("Unnecessary because CanRead is false. The type will skip the converter.");
        }

        public override void WriteJson(JsonWriter writer, JsonPatchOperation value, JsonSerializer serializer)
        {
            JToken t = JToken.FromObject(value);

            if (t.Type != JTokenType.Object)
            {
                t.WriteTo(writer);
            }
            else
            {
                writer.WriteStartObject();
                writer.WritePropertyName("op");
                writer.WriteValue(value.Operation.ToString().ToLower());
                writer.WritePropertyName("path");
                writer.WriteValue(value.Path);
                if (!string.IsNullOrEmpty(value.From))
                {
                    writer.WritePropertyName("from");
                    writer.WriteValue(value.From);
                }
                writer.WritePropertyName("value");
                t = JToken.FromObject(value.Value);
                t.WriteTo(writer);
                writer.WriteEndObject();
            }
        }
    }
}
using System.Buffers;
using System.Text.Json;
using Newtonsoft.Json;

namespace aggregator
{
    static class JsonExtensions
    {
        public static T ToObject<T>(this JsonElement element)
        {
            var json = element.GetRawText();
            T obj = JsonConvert.DeserializeObject<T>(json);
            return obj;
        }
    }
}

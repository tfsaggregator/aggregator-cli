using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Newtonsoft.Json;

namespace aggregator
{
    public static class HttpExtensions
    {
        public static HttpResponse AddCustomHeaders(this HttpResponse response, string aggregatorVersion, string ruleName)
        {
            response?.Headers.Add("X-Aggregator-Version", aggregatorVersion);
            response?.Headers.Add("X-Aggregator-Rule", ruleName);

            return response;
        }

        public static async Task<WebHookEvent> GetWebHookEvent(this HttpRequest request, ILogger logger)
        {
            using var bodyStreamReader = new StreamReader(request.Body);
            var jsonContent = await bodyStreamReader.ReadToEndAsync();
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                logger.LogWarning($"Failed parsing request body: empty");
                return null;
            }

            var data = JsonConvert.DeserializeObject<WebHookEvent>(jsonContent);
            return data;
        }
    }
}

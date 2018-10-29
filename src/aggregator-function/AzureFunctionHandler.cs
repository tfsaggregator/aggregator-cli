using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace aggregator
{
    /// <summary>
    /// Azure Function wrapper for Aggregator v3
    /// </summary>
    public class AzureFunctionHandler
    {
        private readonly Microsoft.Extensions.Logging.ILogger log;
        private readonly ExecutionContext context;

        public AzureFunctionHandler(Microsoft.Extensions.Logging.ILogger logger, ExecutionContext context)
        {
            this.log = logger;
            this.context = context;
        }

        public async Task<HttpResponseMessage> Run(HttpRequestMessage req)
        {
            log.LogDebug($"Context: {context.InvocationId} {context.FunctionName} {context.FunctionDirectory} {context.FunctionAppDirectory}");

            var aggregatorVersion = GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            try
            {
                string rule = context.FunctionName;
                log.LogInformation($"Aggregator v{aggregatorVersion} executing rule '{rule}'");
            }
            catch (Exception ex)
            {
                log.LogWarning($"Failed parsing request headers: {ex.Message}");
            }

            // Get request body
            string jsonContent = await req.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                log.LogWarning($"Failed parsing request body: empty");

                var resp = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("Request body is empty")
                };
                return resp;
            }
            dynamic data = JsonConvert.DeserializeObject(jsonContent);
            string eventType = data.eventType;

            // sanity check
            if (!DevOpsEvents.IsValidEvent(eventType)
                || data.publisherId != DevOpsEvents.PublisherId)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Not a good Azure DevOps post..."
                });
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var configuration = AggregatorConfiguration.Read(config);

            var logger = new ForwarderLogger(log);
            var wrapper = new RuleWrapper(configuration, logger, context.FunctionName, context.FunctionDirectory);
            try
            {
                string execResult = await wrapper.Execute(data);

                if (string.IsNullOrEmpty(execResult))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    return resp;
                }
                else
                {
                    log.LogInformation($"Returning '{execResult}' from '{context.FunctionName}'");

                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(execResult)
                    };
                    return resp;
                }
            }
            catch (Exception ex)
            {
                log.LogWarning($"Rule '{context.FunctionName}' failed: {ex.Message}");

                var resp = new HttpResponseMessage(HttpStatusCode.NotImplemented)
                {
                    Content = new StringContent(ex.Message)
                };
                return resp;
            }
        }

        private static T GetCustomAttribute<T>()
            where T : Attribute
        {
            return System.Reflection.Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes(typeof(T), false)
                .FirstOrDefault() as T;
        }
    }
}

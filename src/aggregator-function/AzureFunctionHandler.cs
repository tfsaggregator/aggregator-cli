using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System.Text;

namespace aggregator
{
    /// <summary>
    /// Azure Function wrapper for Aggregator v3
    /// </summary>
    public class AzureFunctionHandler
    {
        private readonly TraceWriter log;
        private readonly ExecutionContext context;

        public AzureFunctionHandler(TraceWriter log, ExecutionContext context)
        {
            this.log = log;
            this.context = context;
        }

        public async Task<HttpResponseMessage> Run(HttpRequestMessage req)
        {
            log.Verbose($"Context: {context.InvocationId} {context.FunctionName} {context.FunctionDirectory} {context.FunctionAppDirectory}");

            // TODO
            string aggregatorVersion = null;

            try
            {
                string rule = context.FunctionName;
                log.Info($"Welcome to {rule}");
            }
            catch (Exception ex)
            {
                log.Warning($"Failed parsing headers: {ex.Message}");
            }

            // Get request body
            string jsonContent = await req.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(jsonContent);

            // sanity check
            if (!VstsEvents.IsValidEvent(data.eventType)
                || data.publisherId != VstsEvents.PublisherId)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Not a good VSTS post..."
                });
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var configuration = AggregatorConfiguration.Read(config);

            var logger = new TraceWriterLogger(log);
            var wrapper = new RuleWrapper(configuration, logger, context.FunctionName, context.FunctionDirectory);
            string execResult = await wrapper.Execute(aggregatorVersion, data);

            log.Info($"Returning {execResult}");

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(execResult)
            };
            return resp;
        }
    }
}

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
    internal class RequestProcessor
    {
        private readonly TraceWriter log;
        private readonly ExecutionContext context;

        internal RequestProcessor(TraceWriter log, ExecutionContext context)
        {
            this.log = log;
            this.context = context;
        }

        internal async Task<IActionResult> Run(HttpRequest req)
        {
            log.Info("C# HTTP trigger function processed a request.");
            log.Verbose($"Context: {context.InvocationId} {context.FunctionName} {context.FunctionDirectory} {context.FunctionAppDirectory}");

            // TODO
            string aggregatorVersion = null;

            try
            {
                string base64Auth = req.Headers["Authorization"].FirstOrDefault().Substring(6);
                string user = Encoding.Default.GetString(Convert.FromBase64String(base64Auth)).Split(':')[0];
                string rule = req.Headers["rule"].FirstOrDefault();
                log.Info($"Welcome {user} to {rule}");
            }
            catch (Exception ex)
            {
                log.Warning($"Failed parsing headers: {ex.Message}");
            }

            // Get request body
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            // sanity check
            if ((data.eventType != "workitem.created"
                && data.eventType != "workitem.updated")
                 || data.publisherId != "tfs")
            {
                return new BadRequestObjectResult("Not a good VSTS post");
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var logger = new TraceWriterLogger(log);
            /*
             public string FunctionDirectory { get; set; }
             public string FunctionAppDirectory { get; set; }
            */
            var wrapper = new RuleWrapper(config, logger, context.FunctionDirectory);
            string result = await wrapper.Execute(aggregatorVersion, data);

            log.Info($"Returning {result}");

            return (ActionResult)new OkObjectResult(result);
        }
    }
}

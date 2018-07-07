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

namespace aggregator
{
    public static class FunctionCore
    {
        [FunctionName("FunctionCore")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]
            HttpRequest req,
            TraceWriter log,
            ExecutionContext context)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            // TODO
            string aggregatorVersion = "0.1";

            // Get request body
            string requestBody = new StreamReader(req.Body).ReadToEnd();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            var rule = new RuleWrapper(config);
            string result = await rule.Execute(aggregatorVersion, data);


            return (ActionResult)new OkObjectResult(result);
            // new BadRequestObjectResult("Please pass a name on the query string or in the request body");
        }
    }
}

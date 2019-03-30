using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ExecutionContext = Microsoft.Azure.WebJobs.ExecutionContext;

namespace aggregator
{
    /// <summary>
    /// Azure Function wrapper for Aggregator v3
    /// </summary>
    public class AzureFunctionHandler
    {
        private readonly ILogger _log;
        private readonly ExecutionContext _context;

        public AzureFunctionHandler(ILogger logger, ExecutionContext context)
        {
            _log = logger;
            _context = context;
        }

        public async Task<HttpResponseMessage> RunAsync(HttpRequestMessage req, CancellationToken cancellationToken)
        {
            _log.LogDebug($"Context: {_context.InvocationId} {_context.FunctionName} {_context.FunctionDirectory} {_context.FunctionAppDirectory}");

            var aggregatorVersion = GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            try
            {
                var rule = _context.FunctionName;
                _log.LogInformation($"Aggregator v{aggregatorVersion} executing rule '{rule}'");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"Failed parsing request headers: {ex.Message}");
            }

            cancellationToken.ThrowIfCancellationRequested();

            // Get request body
            var jsonContent = await req.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _log.LogWarning($"Failed parsing request body: empty");

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

            string resourceUrl = data.resource.url;
            if (resourceUrl.StartsWith("http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/"))
            {
                var resp = req.CreateResponse(HttpStatusCode.OK, new
                {
                    message = $"Hello from Aggregator v{aggregatorVersion} executing rule '{_context.FunctionName}'"
                });
                resp.Headers.Add("X-Aggregator-Version", aggregatorVersion);
                resp.Headers.Add("X-Aggregator-Rule", _context.FunctionName);
                return resp;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(_context.FunctionAppDirectory)
                .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();
            var configuration = AggregatorConfiguration.Read(config);
            configuration = InvokeOptions.ExtendFromUrl(configuration, req.RequestUri);

            var logger = new ForwarderLogger(_log);
            var wrapper = new RuleWrapper(configuration, logger, _context.FunctionName, _context.FunctionDirectory);
            try
            {
                string execResult = await wrapper.ExecuteAsync(data, cancellationToken);

                if (string.IsNullOrEmpty(execResult))
                {
                    var resp = new HttpResponseMessage(HttpStatusCode.OK);
                    return resp;
                }
                else
                {
                    _log.LogInformation($"Returning '{execResult}' from '{_context.FunctionName}'");

                    var resp = new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(execResult)
                    };
                    return resp;
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"Rule '{_context.FunctionName}' failed: {ex.Message}");

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

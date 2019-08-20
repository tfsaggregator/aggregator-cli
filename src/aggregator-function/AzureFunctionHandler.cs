using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Newtonsoft.Json.Linq;
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

            var data = JsonConvert.DeserializeObject<WebHookEvent>(jsonContent);

            // sanity check
            if (!DevOpsEvents.IsValidEvent(data.EventType)
                || data.PublisherId != DevOpsEvents.PublisherId)
            {
                return req.CreateResponse(HttpStatusCode.BadRequest, new
                {
                    error = "Not a good Azure DevOps post..."
                });
            }

            var eventContext = CreateContextFromEvent(data);
            if (eventContext.IsTestEvent())
            {
                return RespondToTestEventMessage(req, aggregatorVersion);
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
                string execResult = await wrapper.ExecuteAsync(eventContext, cancellationToken);

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

        private HttpResponseMessage RespondToTestEventMessage(HttpRequestMessage req, string aggregatorVersion)
        {
            var resp = req.CreateResponse(HttpStatusCode.OK, new
                                                             {
                                                                 message = $"Hello from Aggregator v{aggregatorVersion} executing rule '{_context.FunctionName}'"
                                                             });
            resp.Headers.Add("X-Aggregator-Version", aggregatorVersion);
            resp.Headers.Add("X-Aggregator-Rule", _context.FunctionName);
            return resp;
        }

        private static WorkItemEventContext CreateContextFromEvent(WebHookEvent eventData)
        {
            var collectionUrl = eventData.ResourceContainers.GetValueOrDefault("collection")?.BaseUrl;
            var teamProjectId = eventData.ResourceContainers.GetValueOrDefault("project")?.Id ?? Guid.Empty;

            var resourceObject = eventData.Resource as JObject;
            if (ServiceHooksEventTypeConstants.WorkItemUpdated == eventData.EventType)
            {
                var workItem = resourceObject.GetValue("revision").ToObject<WorkItem>();
                var workItemUpdate = resourceObject.ToObject<WorkItemUpdate>();
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, workItemUpdate);
            }
            else
            {
                var workItem = resourceObject.ToObject<WorkItem>();
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem);
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

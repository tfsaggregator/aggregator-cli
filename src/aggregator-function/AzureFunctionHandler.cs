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
using Microsoft.VisualStudio.Services.WebApi;

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

            var ruleName = _context.FunctionName;
            var aggregatorVersion = GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            _log.LogInformation($"Aggregator v{aggregatorVersion} executing rule '{ruleName}'");
            cancellationToken.ThrowIfCancellationRequested();

            // Get request body
            var eventData = await GetWebHookEvent(req);
            if (eventData == null)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Request body is empty");
            }

            // sanity check
            if (!DevOpsEvents.IsValidEvent(eventData.EventType)
                || eventData.PublisherId != DevOpsEvents.PublisherId)
            {
                return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Not a good Azure DevOps post...");
            }

            var eventContext = CreateContextFromEvent(eventData);
            if (eventContext.IsTestEvent())
            {
                return req.CreateTestEventResponse(aggregatorVersion, ruleName);
            }

            var configContext = GetConfigurationContext();
            var configuration = AggregatorConfiguration.ReadConfiguration(configContext)
                                                       .UpdateFromUrl(ruleName, req.RequestUri);

            var logger = new ForwarderLogger(_log);
            var ruleProvider = new AzureFunctionRuleProvider(logger, _context.FunctionDirectory);
            var ruleExecutor = new RuleExecutor(logger, configuration);
            using (_log.BeginScope($"WorkItem #{eventContext.WorkItemPayload.WorkItem.Id}"))
            {
                try
                {
                    var rule = await ruleProvider.GetRule(ruleName);
                    var execResult = await ruleExecutor.ExecuteAsync(rule, eventContext, cancellationToken);

                    if (string.IsNullOrEmpty(execResult))
                    {
                        return req.CreateResponse(HttpStatusCode.OK);
                    }
                    else
                    {
                        _log.LogInformation($"Returning '{execResult}' from '{rule.Name}'");
                        return req.CreateResponse(HttpStatusCode.OK, execResult);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"Rule '{ruleName}' failed: {ex.Message}");
                    return req.CreateErrorResponse(HttpStatusCode.NotImplemented, ex);
                }
            }
        }


        private IConfigurationRoot GetConfigurationContext()
        {
            var config = new ConfigurationBuilder()
                         .SetBasePath(_context.FunctionAppDirectory)
                         .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                         .AddEnvironmentVariables()
                         .Build();
            return config;
        }


        private async Task<WebHookEvent> GetWebHookEvent(HttpRequestMessage req)
        {
            var jsonContent = await req.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                _log.LogWarning($"Failed parsing request body: empty");
                return null;
            }

            var data = JsonConvert.DeserializeObject<WebHookEvent>(jsonContent);
            return data;
        }

        protected WorkItemEventContext CreateContextFromEvent(WebHookEvent eventData)
        {
            var collectionUrl = eventData.ResourceContainers.GetValueOrDefault("collection")?.BaseUrl ?? "https://example.com";
            var teamProjectId = eventData.ResourceContainers.GetValueOrDefault("project")?.Id ?? Guid.Empty;

            var resourceObject = eventData.Resource as JObject;
            if (ServiceHooksEventTypeConstants.WorkItemUpdated == eventData.EventType)
            {
                var workItem = resourceObject.GetValue("revision").ToObject<WorkItem>();
                MigrateIdentityInformation(eventData.ResourceVersion, workItem);
                var workItemUpdate = resourceObject.ToObject<WorkItemUpdate>();
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, workItemUpdate);
            }
            else
            {
                var workItem = resourceObject.ToObject<WorkItem>();
                MigrateIdentityInformation(eventData.ResourceVersion, workItem);
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem);
            }
        }

        /// <summary>
        /// in Event Resource Version == 1.0 the Identity Information is provided in a single string "DisplayName <UniqueName>", in newer Revisions
        /// the Identity Information as on Object of type IdentityRef
        /// As we rely on IdentityRef we could switch the WebHook to use ResourceVersion > 1.0 but unfortunately there is a bug
        /// as these Resources do not send the relation information in the event (although resource details is set to all).
        /// Option 1: Use Resource Version > 1.0 and load work item later to get relation information
        /// Option 2: Use Resource Version == 1.0 and convert string to IdentityRef
        ///
        /// Use Option 2, as less server round trips, write warning in case of too new Resource Version, Open ticket at Microsoft and see if they accept it as Bug.
        /// </summary>
        /// <param name="resourceVersion"></param>
        /// <param name="workItem"></param>
        protected void MigrateIdentityInformation(string resourceVersion, WorkItem workItem)
        {
            const char UNIQUE_NAME_START_CHAR = '<';
            const char UNIQUE_NAME_END_CHAR = '>';

            if (!resourceVersion.StartsWith("1.0"))
            {
                _log.LogWarning($"Mapping is using Resource Version {resourceVersion}, which can lead to some issues with e.g. not available relation information on trigger work item.");
                return;
            }

            IdentityRef ConvertOrDefault(string input)
            {
                var uniqueNameStartIndex = input.LastIndexOf(UNIQUE_NAME_START_CHAR);
                var uniqueNameEndIndex = input.LastIndexOf(UNIQUE_NAME_END_CHAR);

                if (uniqueNameStartIndex < 0 || uniqueNameEndIndex != input.Length -1)
                {
                    return null;
                }

                var uniqueNameLength = uniqueNameEndIndex - uniqueNameStartIndex + 1;

                return new IdentityRef()
                       {
                            DisplayName = input.Substring(0, uniqueNameStartIndex).Trim(),
                            UniqueName = new string(input.Skip(uniqueNameStartIndex + 1).Take(uniqueNameLength).ToArray())
                        };
            }

            var identityFieldReferenceNameEndings = new[]
                                 {
                                     "By", "To"
                                 };

            foreach (var identityField in workItem.Fields.Where(field => identityFieldReferenceNameEndings.Any(name => field.Key.EndsWith(name))).ToList())
            {
                if (identityField.Value is string identityString)
                {
                    workItem.Fields[identityField.Key] = ConvertOrDefault(identityString) ?? identityField.Value;
                }
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


    internal static class HttpResponseMessageExtensions
    {
        public static HttpResponseMessage CreateTestEventResponse(this HttpRequestMessage req, string aggregatorVersion, string ruleName)
        {
            var resp = req.CreateResponse(HttpStatusCode.OK, new
                                                             {
                                                                 message = $"Hello from Aggregator v{aggregatorVersion} executing rule '{ruleName}'"
                                                             });
            resp.Headers.Add("X-Aggregator-Version", aggregatorVersion);
            resp.Headers.Add("X-Aggregator-Rule", ruleName);
            return resp;
        }
    }
}

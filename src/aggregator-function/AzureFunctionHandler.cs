using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using aggregator.Engine;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Newtonsoft.Json;
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
        private readonly ExecutionContext _executionContext;


        public AzureFunctionHandler(ILogger logger, ExecutionContext executionContext, HttpContext httpContext)
        {
            _log = logger;
            _executionContext = executionContext;
            HttpContext = httpContext;
        }

        public async Task<IActionResult> RunAsync(/*HttpRequest request, */WebHookEvent eventData, CancellationToken cancellationToken)
        {
            _log.LogDebug($"Context: {_executionContext.InvocationId} {_executionContext.FunctionName} {_executionContext.FunctionDirectory} {_executionContext.FunctionAppDirectory}");

            var ruleName = _executionContext.FunctionName;
            var aggregatorVersion = GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            _log.LogInformation($"Aggregator v{aggregatorVersion} executing rule '{ruleName}'");
            cancellationToken.ThrowIfCancellationRequested();

            if (eventData == null)
            {
                return BadRequest("Request body is empty");
            }

            // sanity check
            if (!DevOpsEvents.IsValidEvent(eventData.EventType)
                || eventData.PublisherId != DevOpsEvents.PublisherId)
            {
                _log.LogDebug("return BadRequest");
                return BadRequest(new {Error = "Not a good Azure DevOps post..."});
            }

            var eventContext = CreateContextFromEvent(eventData);
            if (eventContext.IsTestEvent())
            {
                Response.AddCustomHeaders(aggregatorVersion, ruleName);
                return RespondToTestEventMessage(aggregatorVersion, ruleName);
            }

            var configContext = GetConfigurationContext();
            var configuration = AggregatorConfiguration.ReadConfiguration(configContext)
                                                       .UpdateFromUrl(ruleName, GetRequestUri());

            var logger = new ForwarderLogger(_log);
            var ruleProvider = new AzureFunctionRuleProvider(logger, _executionContext.FunctionDirectory);
            var ruleExecutor = new RuleExecutor(logger, configuration);
            using (_log.BeginScope($"WorkItem #{eventContext.WorkItemPayload.WorkItem.Id}"))
            {
                try
                {
                    var rule = await ruleProvider.GetRule(ruleName);
                    var execResult = await ruleExecutor.ExecuteAsync(rule, eventContext, cancellationToken);

                    if (string.IsNullOrEmpty(execResult))
                    {
                        return Ok();
                    }
                    else
                    {
                        _log.LogInformation($"Returning '{execResult}' from '{rule.Name}'");
                        return Ok(execResult);
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"Rule '{ruleName}' failed: {ex.Message}");
                    return BadRequest(ex.Message);
                }
            }
        }


        private IConfiguration GetConfigurationContext()
        {
            var config = new ConfigurationBuilder()
                         .SetBasePath(_executionContext.FunctionAppDirectory)
                         .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
                         .AddEnvironmentVariables()
                         .Build();
            return config;
        }

        private Uri GetRequestUri()
        {
            //during testing this can be null
            var requestUrl = Request?.GetDisplayUrl() ?? "https://google.com/";
            return new Uri(requestUrl);
        }


        private IActionResult RespondToTestEventMessage(string aggregatorVersion, string ruleName)
        {
            return Ok(new { message = $"Hello from Aggregator v{aggregatorVersion} executing rule '{ruleName}'" });
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
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, eventData.EventType, workItemUpdate);
            }
            else
            {
                var workItem = resourceObject.ToObject<WorkItem>();
                MigrateIdentityInformation(eventData.ResourceVersion, workItem);
                return new WorkItemEventContext(teamProjectId, new Uri(collectionUrl), workItem, eventData.EventType);
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

            // assumtion to get all string Identity Fields, normally the naming convention is: These fields ends with TO or BY (e.g. AssignedTO, CreatedBY)
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


        #region Microsoft.AspNetCore.Mvc.ControllerBase

        /// <summary>
        /// Gets the <see cref="Http.HttpContext"/> for the executing action.
        /// </summary>
        public HttpContext HttpContext { get; }

        /// <summary>
        /// Gets the <see cref="HttpRequest"/> for the executing action.
        /// </summary>
        public HttpRequest Request => HttpContext?.Request;

        /// <summary>
        /// Gets the <see cref="HttpResponse"/> for the executing action.
        /// </summary>
        public HttpResponse Response => HttpContext?.Response;

        /// <summary>
        /// Creates an <see cref="BadRequestResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <returns>The created <see cref="BadRequestResult"/> for the response.</returns>
        [NonAction]
        public virtual BadRequestResult BadRequest()
            => new BadRequestResult();

        /// <summary>
        /// Creates an <see cref="BadRequestObjectResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <param name="error">An error object to be returned to the client.</param>
        /// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual BadRequestObjectResult BadRequest(object error)
            => new BadRequestObjectResult(error);


        /// <summary>
        /// Creates a <see cref="OkResult"/> object that produces an empty <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <returns>The created <see cref="OkResult"/> for the response.</returns>
        [NonAction]
        public virtual OkResult Ok()
            => new OkResult();

        /// <summary>
        /// Creates an <see cref="OkObjectResult"/> object that produces an <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual OkObjectResult Ok(object value)
            => new OkObjectResult(value);

        /// <summary>
        /// Creates an <see cref="NotFoundResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <returns>The created <see cref="NotFoundResult"/> for the response.</returns>
        [NonAction]
        public virtual NotFoundResult NotFound()
            => new NotFoundResult();

        /// <summary>
        /// Creates an <see cref="NotFoundObjectResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <returns>The created <see cref="NotFoundObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual NotFoundObjectResult NotFound(object value)
            => new NotFoundObjectResult(value);

        #endregion
    }


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

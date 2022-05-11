using System;
using System.Threading;
using System.Threading.Tasks;
using aggregator.Engine;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
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
            var aggregatorVersion = RequestHelper.AggregatorVersion;
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
                return BadRequest(new { Error = "Not a good Azure DevOps post..." });
            }

            var helper = new RequestHelper(_log);
            var eventContext = helper.CreateContextFromEvent(eventData);
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
            var requestUrl = Request?.GetDisplayUrl() ?? MagicConstants.MissingUrl;
            return new Uri(requestUrl);
        }

        private IActionResult RespondToTestEventMessage(string aggregatorVersion, string ruleName)
        {
            return Ok(new { message = RequestHelper.GetTestEventMessageReply(aggregatorVersion, ruleName) });
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
        public virtual BadRequestResult BadRequest() => new();

        /// <summary>
        /// Creates an <see cref="BadRequestObjectResult"/> that produces a <see cref="StatusCodes.Status400BadRequest"/> response.
        /// </summary>
        /// <param name="error">An error object to be returned to the client.</param>
        /// <returns>The created <see cref="BadRequestObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual BadRequestObjectResult BadRequest(object error) => new(error);


        /// <summary>
        /// Creates a <see cref="OkResult"/> object that produces an empty <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <returns>The created <see cref="OkResult"/> for the response.</returns>
        [NonAction]
        public virtual OkResult Ok() => new();

        /// <summary>
        /// Creates an <see cref="OkObjectResult"/> object that produces an <see cref="StatusCodes.Status200OK"/> response.
        /// </summary>
        /// <param name="value">The content value to format in the entity body.</param>
        /// <returns>The created <see cref="OkObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual OkObjectResult Ok(object value) => new(value);

        /// <summary>
        /// Creates an <see cref="NotFoundResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <returns>The created <see cref="NotFoundResult"/> for the response.</returns>
        [NonAction]
        public virtual NotFoundResult NotFound() => new();

        /// <summary>
        /// Creates an <see cref="NotFoundObjectResult"/> that produces a <see cref="StatusCodes.Status404NotFound"/> response.
        /// </summary>
        /// <returns>The created <see cref="NotFoundObjectResult"/> for the response.</returns>
        [NonAction]
        public virtual NotFoundObjectResult NotFound(object value) => new (value);

        #endregion
    }


}

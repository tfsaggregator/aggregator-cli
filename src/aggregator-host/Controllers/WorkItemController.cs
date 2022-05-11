using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using aggregator;
using aggregator.Engine;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;

namespace aggregator_host.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize(AuthenticationSchemes = ApiKeyAuthenticationScheme.DefaultScheme)]
    public class WorkItemController : ControllerBase
    {
        private readonly ILogger _log;
        private readonly IConfiguration _configuration;

        public WorkItemController(IConfiguration configuration, ILoggerFactory loggerFactory)
        {
            _configuration = configuration;
            _log = loggerFactory.CreateLogger(MagicConstants.LoggerCategoryName);
        }

        [HttpPost("{ruleName}")]
        public async Task<IActionResult> PostAsync(WebHookEvent eventData, string ruleName, CancellationToken cancellationToken)
        {
            var aggregatorVersion = RequestHelper.AggregatorVersion;
            _log.LogInformation("Aggregator v{aggregatorVersion} executing rule '{ruleName}'", aggregatorVersion, ruleName);

            var webHookStartEvent = new EventTelemetry()
            {
                Name = "WebHookEvent Start"
            };
            webHookStartEvent.Properties["rule"] = ruleName;
            webHookStartEvent.Properties["eventType"] = eventData?.EventType;
            webHookStartEvent.Properties["resourceVersion"] = eventData?.ResourceVersion;
            Telemetry.TrackEvent(webHookStartEvent);

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

            var configContext = _configuration;
            var configuration = AggregatorConfiguration.ReadConfiguration(configContext)
                                                       .UpdateFromUrl(ruleName, GetRequestUri());

            var logger = new ForwarderLogger(_log);
            var ruleProvider = new AspNetRuleProvider(logger, _configuration);
            var ruleExecutor = new RuleExecutor(logger, configuration);
            using (_log.BeginScope($"WorkItem #{eventContext.WorkItemPayload.WorkItem.Id}"))
            {
                try
                {
                    var rule = await ruleProvider.GetRule(ruleName);

                    var ruleStartEvent = new EventTelemetry()
                    {
                        Name = "Rule Exec Start"
                    };
                    ruleStartEvent.Properties["rule"] = rule.Name;
                    ruleStartEvent.Properties["ImpersonateExecution"] = rule.ImpersonateExecution.ToString();
                    ruleStartEvent.Properties["EnableRevisionCheck"] = rule.Settings.EnableRevisionCheck.ToString();
                    ruleStartEvent.Properties["eventType"] = eventContext.EventType;
                    Telemetry.TrackEvent(ruleStartEvent);
                    var ruleExecTimer = new Stopwatch();
                    ruleExecTimer.Start();

                    var execResult = await ruleExecutor.ExecuteAsync(rule, eventContext, cancellationToken);

                    ruleExecTimer.Stop();
                    var ruleEndEvent = new EventTelemetry()
                    {
                        Name = "Rule Exec End"
                    };
                    ruleEndEvent.Properties["rule"] = ruleName;
                    ruleEndEvent.Metrics["duration"] = ruleExecTimer.ElapsedMilliseconds;
                    Telemetry.TrackEvent(ruleEndEvent);

                    if (string.IsNullOrEmpty(execResult))
                    {
                        return Ok();
                    }
                    else
                    {
                        _log.LogInformation("Returning '{execResult}' from '{ruleName}'", execResult, ruleName);
                        return Ok(execResult);
                    }
                }
                catch (Exception ex)
                {
                    string message = ex.Message;
                    _log.LogWarning("Rule '{ruleName}' failed: {message}", ruleName, message);
                    Telemetry.TrackException(ex);
                    return BadRequest(ex.Message);
                }
            }
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
    }
}

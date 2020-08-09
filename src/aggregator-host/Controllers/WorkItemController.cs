using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using aggregator;
using aggregator.Engine;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;

namespace aggregator_host.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class WorkItemController : ControllerBase
    {
        private readonly ILogger<WorkItemController> _log;
        private readonly IConfiguration _configuration;

        public WorkItemController(IConfiguration configuration, ILogger<WorkItemController> logger)
        {
            _configuration = configuration;
            _log = logger;
        }

        [HttpGet]
        public string Get(string rule)
        {
            _log.LogInformation("Get!");
            return "Hi from self-hosted!";
        }

        [HttpPost("{ruleName}")]
        public async Task<IActionResult> PostAsync(WebHookEvent eventData, string ruleName, CancellationToken cancellationToken)
        {
            var aggregatorVersion = RequestHelper.AggregatorVersion;
            _log.LogInformation($"Aggregator v{aggregatorVersion} executing rule '{ruleName}'");

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
            var ruleProvider = new AspNetRuleProvider(logger, _configuration.GetValue<string>(WebHostDefaults.ContentRootKey));
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

        private Uri GetRequestUri()
        {
            //during testing this can be null
            var requestUrl = Request?.GetDisplayUrl() ?? "https://google.com/";
            return new Uri(requestUrl);
        }

        private IActionResult RespondToTestEventMessage(string aggregatorVersion, string ruleName)
        {
            return Ok(new { message = RequestHelper.GetTestEventMessageReply(aggregatorVersion, ruleName) });
        }
    }
}

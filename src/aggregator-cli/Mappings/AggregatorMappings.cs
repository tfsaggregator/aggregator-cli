using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.Azure.Management.Fluent;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Services.FormInput;

namespace aggregator.cli
{
    internal class AggregatorMappings
    {
        private VssConnection vsts;
        private readonly IAzure azure;
        private readonly ILogger logger;

        public AggregatorMappings(VssConnection vsts, IAzure azure, ILogger logger)
        {
            this.vsts = vsts;
            this.azure = azure;
            this.logger = logger;
        }

        internal async Task<IEnumerable<(string rule, string project, string @event, string status)>> ListAsync(InstanceName instance)
        {
            logger.WriteVerbose($"Searching aggregator mappings in Azure DevOps...");
            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = await serviceHooksClient.QuerySubscriptionsAsync();
            var filteredSubs = subscriptions.Where(s
                    => s.PublisherId == VstsEvents.PublisherId
                    && s.ConsumerInputs["url"].ToString().StartsWith(
                        instance.FunctionAppUrl)
                    );
            var projectClient = vsts.GetClient<ProjectHttpClient>();
            var projects = await projectClient.GetProjects();
            var projectsDict = projects.ToDictionary(p => p.Id);

            var result = new List<(string rule, string project, string @event, string status)>();
            foreach (var subscription in filteredSubs)
            {
                var foundProject = projectsDict[
                    new Guid(subscription.PublisherInputs["projectId"])
                    ];
                // HACK need to factor the URL<->rule_name
                string ruleUrl = subscription.ConsumerInputs["url"].ToString();
                string ruleName = ruleUrl.Substring(ruleUrl.LastIndexOf('/'));
                string ruleFullName = instance.PlainName + ruleName;
                result.Add(
                    (ruleFullName, foundProject.Name, subscription.EventType, subscription.Status.ToString())
                    );
            }
            return result;
        }

        internal async Task<Guid> Add(string projectName, string @event, InstanceName instance, string ruleName)
        {
            logger.WriteVerbose($"Reading VSTS project data...");
            var projectClient = vsts.GetClient<ProjectHttpClient>();
            var project = await projectClient.GetProject(projectName);
            logger.WriteInfo($"Project {projectName} data read.");

            var rules = new AggregatorRules(azure, logger);
            logger.WriteVerbose($"Retrieving {ruleName} Function Key...");
            (string ruleUrl, string ruleKey) = await rules.GetInvocationUrlAndKey(instance, ruleName);
            logger.WriteInfo($"{ruleName} Function Key retrieved.");

            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();

            // check if the subscription already exists and bail out
            var query = new SubscriptionsQuery {
                PublisherId = VstsEvents.PublisherId,
                ConsumerInputFilters = new InputFilter[] {
                    new InputFilter {
                        Conditions = new List<InputFilterCondition> {
                            new InputFilterCondition
                            {
                                InputId = "url",
                                InputValue  = ruleUrl,
                                Operator = InputFilterOperator.Equals,
                                CaseSensitive = false
                            }
                        }
                    }
                }
            };
            var queryResult = await serviceHooksClient.QuerySubscriptionsAsync(query);
            if (queryResult.Results.Any())
            {
                logger.WriteWarning($"There is already such a mapping.");
                return Guid.Empty;
            }

            // see https://docs.microsoft.com/en-us/vsts/service-hooks/consumers?toc=%2Fvsts%2Fintegrate%2Ftoc.json&bc=%2Fvsts%2Fintegrate%2Fbreadcrumb%2Ftoc.json&view=vsts#web-hooks
            var subscriptionParameters = new Subscription()
            {
                ConsumerId = "webHooks",
                ConsumerActionId = "httpRequest",
                ConsumerInputs = new Dictionary<string, string>
                {
                    { "url", ruleUrl },
                    { "httpHeaders", $"x-functions-key:{ruleKey}" },
                    // careful with casing!
                    { "resourceDetailsToSend", "all" },
                    { "messagesToSend", "none" },
                    { "detailedMessagesToSend", "none" },
                },
                EventType = @event,
                PublisherId = VstsEvents.PublisherId,
                PublisherInputs = new Dictionary<string, string>
                {
                    { "projectId", project.Id.ToString() },
                    /* TODO consider offering additional filters using the following
                    { "tfsSubscriptionId", vsts.ServerId },
                    { "teamId", null },
                    // Filter events to include only work items under the specified area path.
                    { "areaPath", null },
                    // Filter events to include only work items of the specified type.
                    { "workItemType", null },
                    // Filter events to include only work items with the specified field(s) changed
                    { "changedFields", null },
                    // The string that must be found in the comment.
                    { "commentPattern", null },
                    */
                },
            };

            logger.WriteVerbose($"Adding mapping for {@event}...");
            var newSubscription = await serviceHooksClient.CreateSubscriptionAsync(subscriptionParameters);
            logger.WriteInfo($"Event subscription {newSubscription.Id} setup.");
            return newSubscription.Id;
        }

        internal async Task<bool> RemoveInstanceAsync(InstanceName instance)
        {
            return await RemoveRuleEventAsync("*", instance, "*");
        }

        internal async Task<bool> RemoveRuleAsync(InstanceName instance, string rule)
        {
            return await RemoveRuleEventAsync("*", instance, rule);
        }

        internal async Task<bool> RemoveRuleEventAsync(string @event, InstanceName instance, string rule)
        {
            logger.WriteInfo($"Querying the Azure DevOps subscriptions for rule(s) {instance.PlainName}/{rule}");
            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = await serviceHooksClient.QuerySubscriptionsAsync(VstsEvents.PublisherId);
            var ruleSubs = subscriptions
                // TODO can we trust this equality?
                // && s.ActionDescription == $"To host {instance.DnsHostName}"
                .Where(s => s.ConsumerInputs["url"].ToString().StartsWith(
                    instance.FunctionAppUrl));
            if (@event != "*")
            {
                ruleSubs = ruleSubs.Where(s => s.EventType == @event);
            }
            if (rule != "*")
            {
                ruleSubs = ruleSubs
                .Where(s => s.ConsumerInputs["url"].ToString().StartsWith(
                    AggregatorRules.GetInvocationUrl(instance, rule)));
            }
            foreach (var ruleSub in ruleSubs)
            {
                logger.WriteVerbose($"Deleting subscription {ruleSub.EventDescription}...");
                await serviceHooksClient.DeleteSubscriptionAsync(ruleSub.Id);
                logger.WriteInfo($"Subscription {ruleSub.EventDescription} deleted.");
            }

            return true;
        }
    }
}
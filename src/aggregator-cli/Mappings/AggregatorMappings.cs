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

        internal IEnumerable<(string rule, string project, string events)> List(string instance)
        {
            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = serviceHooksClient
                .QuerySubscriptionsAsync()
                .Result
                .Where(s
                    => s.PublisherId == "tfs"
                    && s.ConsumerInputs["url"].ToString().StartsWith($"https://{instance}.azurewebsites.net")
                    );

            foreach (var subscription in subscriptions)
            {
                yield return (
                    subscription.ConsumerInputs["url"],
                    subscription.PublisherInputs["projectId"],
                    subscription.EventType
                    );
            }
        }

        internal bool ValidateEvent(string @event)
        {
            // TODO this table should be visible in the help
            var validValues = new string[] {
                "workitem.created",
                "workitem.deleted",
                "workitem.restored",
                "workitem.updated",
                "workitem.commented"
            };
            return validValues.Contains(@event);
        }

        internal async Task<Guid> Add(string projectName, string @event, string instance, string ruleName)
        {
            var projectClient = vsts.GetClient<ProjectHttpClient>();
            var project = await projectClient.GetProject(projectName);

            var rules = new AggregatorRules(azure, logger);
            string ruleUrl = await rules.GetInvocationUrl(instance, ruleName);

            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();

            // TODO see https://docs.microsoft.com/en-us/vsts/service-hooks/events?toc=%2Fvsts%2Fintegrate%2Ftoc.json&bc=%2Fvsts%2Fintegrate%2Fbreadcrumb%2Ftoc.json&view=vsts#work-item-created
            var subscriptionParameters = new Subscription()
            {
                ConsumerId = "webHooks",
                ConsumerActionId = "httpRequest",
                ConsumerInputs = new Dictionary<string, string>
                {
                    { "url", ruleUrl },
                    { "httpHeaders", $"rule:{ruleName}" }, // HACK use this as metadata
                    { "basicAuthUsername", "me"}, // TODO
                    { "basicAuthPassword", "pass" }, // TODO
                    { "resourceDetailsToSend", "All" },
                    { "messagesToSend", "None" },
                    { "detailedMessagesToSend", "None" },
                },
                EventType = @event,
                PublisherId = "tfs",
                PublisherInputs = new Dictionary<string, string>
                {
                    { "projectId", project.Id.ToString() },
                    /* TODO
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

            var newSubscription = await serviceHooksClient.CreateSubscriptionAsync(subscriptionParameters);
            return newSubscription.Id;
        }

        internal async Task<bool> RemoveInstanceAsync(string instance)
        {
            return await RemoveRuleEventAsync("*", instance, "*");
        }

        internal async Task<bool> RemoveRuleAsync(string instance, string rule)
        {
            return await RemoveRuleEventAsync("*", instance, rule);
        }

        internal async Task<bool> RemoveRuleEventAsync(string @event, string instance, string rule)
        {
            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = await serviceHooksClient.QuerySubscriptionsAsync("tfs");
            var ruleSubs = subscriptions
                // TODO can we trust this?
                // && s.ActionDescription == $"To host {instance}.azurewebsites.net"
                .Where(s => s.ConsumerInputs["url"].ToString().StartsWith($"https://{instance}.azurewebsites.net"));
            if (@event != "*")
            {
                ruleSubs = ruleSubs.Where(s => s.EventType == @event);
            }
            if (rule != "*")
            {
                ruleSubs = ruleSubs.Where(s => s.ConsumerInputs["httpHeaders"].ToString() == $"rule:{rule}");
            }
            foreach (var ruleSub in ruleSubs)
            {
                await serviceHooksClient.DeleteSubscriptionAsync(ruleSub.Id);
            }

            return true;
        }
    }
}
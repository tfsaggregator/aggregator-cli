using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.Azure.Management.Fluent;
using System.Text.RegularExpressions;

namespace aggregator.cli
{
    internal class AggregatorMappings
    {
        private VssConnection vsts;
        private IAzure azure;

        public AggregatorMappings(VssConnection vsts, IAzure azure)
        {
            this.vsts = vsts;
            this.azure = azure;
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
            //HACK
            switch (@event)
            {
                case "workitem.created": return true;
                default: return false;
            }
        }

        internal async Task<bool> Add(string projectName, string @event, string instance, string ruleName)
        {
            var projectClient = vsts.GetClient<ProjectHttpClient>();
            var project = await projectClient.GetProject(projectName);

            var rules = new AggregatorRules(azure);
            var rule = await rules.Get(instance, ruleName);

            string ruleUrl = rule.url;

            var serviceHooksClient = vsts.GetClient<ServiceHooksPublisherHttpClient>();

            // TODO see https://docs.microsoft.com/en-us/vsts/service-hooks/consumers?toc=%2Fvsts%2Fintegrate%2Ftoc.json&bc=%2Fvsts%2Fintegrate%2Fbreadcrumb%2Ftoc.json&view=vsts#web-hooks
            var subscriptionParameters = new Subscription()
            {
                ConsumerId = "webHooks",
                ConsumerActionId = "httpRequest",
                ConsumerInputs = new Dictionary<string, string>
                {
                    { "url", ruleUrl },
                    { "httpHeaders", "Key1:value1" },
                    { "basicAuthUsername", "me"},
                    { "basicAuthPassword", "pass" },
                    { "resourceDetailsToSend", "All" },
                    { "messagesToSend", "None" },
                    { "detailedMessagesToSend", "None" },
                },
                EventType = @event,
                PublisherId = "tfs",
                PublisherInputs = new Dictionary<string, string>
                {
                    { "projectId", project.Id.ToString() },
                    //{ "subscriberId" },
                    //"teamId"
                },
            };

            Subscription newSubscription = await serviceHooksClient.CreateSubscriptionAsync(subscriptionParameters);
            Guid subscriptionId = newSubscription.Id;
            return true;
        }
    }
}
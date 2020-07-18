﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.Azure.Management.Fluent;
using System.Threading;
using Microsoft.VisualStudio.Services.FormInput;
using aggregator;

namespace aggregator.cli
{
    internal enum RemoveOutcome
    {
        Succeeded   = 0,
        NotFound    = 2,
        Failed      = 1
    }

    internal class AggregatorMappings
    {
        private readonly VssConnection devops;
        private readonly IAzure azure;
        private readonly ILogger logger;
        private readonly INamingTemplates naming;

        public AggregatorMappings(VssConnection devops, IAzure azure, ILogger logger, INamingTemplates naming)
        {
            this.devops = devops;
            this.azure = azure;
            this.logger = logger;
            this.naming = naming;
        }

        internal async Task<IEnumerable<MappingOutputData>> ListAsync(InstanceName instance, string projectName)
        {
            logger.WriteVerbose($"Searching aggregator mappings in Azure DevOps...");
            var serviceHooksClient = devops.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = await serviceHooksClient.QuerySubscriptionsAsync();
            var filteredSubs = instance != null
                    ? subscriptions.Where(s
                        => s.PublisherId == DevOpsEvents.PublisherId
                        && s.ConsumerInputs.GetValue("url","").StartsWith(
                            instance.FunctionAppUrl))
                    : subscriptions.Where(s
                        => s.PublisherId == DevOpsEvents.PublisherId
                        // HACK
                        && s.ConsumerInputs.GetValue("url","").IndexOf("aggregator.azurewebsites.net") > 8);
            var projectClient = devops.GetClient<ProjectHttpClient>();
            var projects = await projectClient.GetProjects();
            var projectsDict = projects.ToDictionary(p => p.Id);

            var result = new List<MappingOutputData>();
            foreach (var subscription in filteredSubs)
            {
                var foundProject = projectsDict[
                    new Guid(subscription.PublisherInputs["projectId"])
                    ];
                if (!string.IsNullOrEmpty(projectName) && foundProject.Name != projectName)
                {
                    continue;
                }
                // HACK need to factor the URL<->rule_name
                Uri ruleUrl = new Uri(subscription.ConsumerInputs.GetValue("url","https://example.com"));
                string ruleName = ruleUrl.Segments.LastOrDefault() ?? string.Empty;
                string ruleFullName = $"{naming.FromFunctionAppUrl(ruleUrl).PlainName}/{ruleName}";
                result.Add(
                    new MappingOutputData(instance, ruleFullName, ruleUrl.IsImpersonationEnabled(), foundProject.Name, subscription.EventType, subscription.Status.ToString())
                    );
            }
            return result;
        }

        internal async Task<Guid> AddAsync(string projectName, string @event, EventFilters filters, InstanceName instance, string ruleName, bool impersonateExecution, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Reading Azure DevOps project data...");
            var projectClient = devops.GetClient<ProjectHttpClient>();
            var project = await projectClient.GetProject(projectName);
            logger.WriteInfo($"Project {projectName} data read.");

            var rules = new AggregatorRules(azure, logger);
            logger.WriteVerbose($"Retrieving {ruleName} Function Key...");
            (Uri ruleUrl, string ruleKey) = await rules.GetInvocationUrlAndKey(instance, ruleName, cancellationToken);
            logger.WriteInfo($"{ruleName} Function Key retrieved.");

            ruleUrl = ruleUrl.AddToUrl(impersonate: impersonateExecution);

            // check if the subscription already exists and bail out
            var query = new SubscriptionsQuery {
                PublisherId = DevOpsEvents.PublisherId,
                PublisherInputFilters= new InputFilter[] {
                    new InputFilter {
                        Conditions = new List<InputFilterCondition> (filters.ToFilterConditions()) {
                            new InputFilterCondition
                            {
                                InputId = "projectId",
                                InputValue  = project.Id.ToString(),
                                Operator = InputFilterOperator.Equals,
                                CaseSensitive = false
                            }
                        }
                    }
                },
                EventType = @event,
                ConsumerInputFilters = new InputFilter[] {
                    new InputFilter {
                        Conditions = new List<InputFilterCondition> {
                            new InputFilterCondition
                            {
                                InputId = "url",
                                InputValue  = ruleUrl.ToString(),
                                Operator = InputFilterOperator.Equals,
                                CaseSensitive = false
                            }
                        }
                    }
                }
            };

            cancellationToken.ThrowIfCancellationRequested();
            var serviceHooksClient = devops.GetClient<ServiceHooksPublisherHttpClient>();
            var queryResult = await serviceHooksClient.QuerySubscriptionsAsync(query);
            if (queryResult.Results.Any())
            {
                logger.WriteWarning($"There is already such a mapping.");
                return Guid.Empty;
            }

            // see https://docs.microsoft.com/en-us/azure/devops/service-hooks/consumers?toc=%2Fvsts%2Fintegrate%2Ftoc.json&bc=%2Fvsts%2Fintegrate%2Fbreadcrumb%2Ftoc.json&view=vsts#web-hooks
            var subscriptionParameters = new Subscription()
            {
                ConsumerId = "webHooks",
                ConsumerActionId = "httpRequest",
                ConsumerInputs = new Dictionary<string, string>
                {
                    { "url", ruleUrl.ToString() },
                    { "httpHeaders", $"x-functions-key:{ruleKey}" },
                    // careful with casing!
                    { "resourceDetailsToSend", "all" },
                    { "messagesToSend", "none" },
                    { "detailedMessagesToSend", "none" },
                },
                EventType = @event,
                PublisherId = DevOpsEvents.PublisherId,
                PublisherInputs = new Dictionary<string, string> (filters.ToInputs())
                {
                    { "projectId", project.Id.ToString() },
                    /* TODO consider offering additional filters using the following
                    { "tfsSubscriptionId", devops.ServerId },
                    { "teamId", null },
                    // The string that must be found in the comment.
                    { "commentPattern", null },
                    */
                },
                // Resource Version 1.0 currently needed for WorkItems, newer Version send EMPTY Relation Information.
                ResourceVersion = "1.0",
            };

            logger.WriteVerbose($"Adding mapping for {@event}...");
            var newSubscription = await serviceHooksClient.CreateSubscriptionAsync(subscriptionParameters);
            logger.WriteInfo($"Event subscription {newSubscription.Id} setup.");
            return newSubscription.Id;
        }

        internal async Task<RemoveOutcome> RemoveInstanceAsync(InstanceName instance)
        {
            return await RemoveRuleEventAsync("*", instance, "*", "*");
        }

        internal async Task<RemoveOutcome> RemoveRuleAsync(InstanceName instance, string rule)
        {
            return await RemoveRuleEventAsync("*", instance, "*", rule);
        }

        internal async Task<RemoveOutcome> RemoveRuleEventAsync(string @event, InstanceName instance, string projectName, string rule)
        {
            logger.WriteInfo($"Querying the Azure DevOps subscriptions for rule(s) {instance.PlainName}/{rule}");
            var serviceHooksClient = devops.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = await serviceHooksClient.QuerySubscriptionsAsync(DevOpsEvents.PublisherId);
            var ruleSubs = subscriptions
                // TODO can we trust this equality?
                // && s.ActionDescription == $"To host {instance.DnsHostName}"
                .Where(s => s.ConsumerInputs.GetValue("url","").StartsWith(
                    instance.FunctionAppUrl));
            if (@event != "*")
            {
                ruleSubs = ruleSubs.Where(s => string.Equals(s.EventType, @event, StringComparison.OrdinalIgnoreCase));
            }

            if (projectName != "*")
            {
                logger.WriteVerbose($"Reading Azure DevOps project data...");
                var projectClient = devops.GetClient<ProjectHttpClient>();
                var project = await projectClient.GetProject(projectName);
                logger.WriteInfo($"Project {projectName} data read.");

                ruleSubs = ruleSubs.Where(s => string.Equals(s.PublisherInputs["projectId"], project.Id.ToString(), StringComparison.OrdinalIgnoreCase));
            }

            if (rule != "*")
            {
                var invocationUrl = AggregatorRules.GetInvocationUrl(instance, rule).ToString();
                ruleSubs = ruleSubs.Where(s => s.ConsumerInputs
                                                .GetValue("url", "")
                                                .StartsWith(invocationUrl, StringComparison.OrdinalIgnoreCase));
            }

            uint count = 0;
            foreach (var ruleSub in ruleSubs)
            {
                logger.WriteVerbose($"Deleting subscription {ruleSub.EventDescription} {ruleSub.EventType}...");
                await serviceHooksClient.DeleteSubscriptionAsync(ruleSub.Id);
                logger.WriteInfo($"Subscription {ruleSub.EventDescription} {ruleSub.EventType} deleted.");
                count++;
            }

            return count > 0 ? RemoveOutcome.Succeeded : RemoveOutcome.NotFound;
        }
    }

    internal class EventFilters
    {
        public string AreaPath { get; set; }
        public string Type { get; set; }
        public string Tag { get; set; }
        public IEnumerable<string> Fields { get; set; }
    }

    internal static class EventFiltersExtension
    {
        public static IEnumerable<KeyValuePair<string, string>> ToInputs(this EventFilters filters)
        {
            if (!string.IsNullOrWhiteSpace(filters.AreaPath))
            {
                var areaPath = filters.AreaPath.First() == '\\' ? filters.AreaPath : $@"\{filters.AreaPath}";
                areaPath = filters.AreaPath.Last() == '\\' ? areaPath : $@"{areaPath}\";

                // Filter events to include only work items under the specified area path.
                yield return new KeyValuePair<string, string>("areaPath", areaPath);
            }
            if (!string.IsNullOrWhiteSpace(filters.Type))
            {
                // Filter events to include only work items of the specified type.
                yield return new KeyValuePair<string, string>("workItemType", filters.Type);
            }
            if (!string.IsNullOrWhiteSpace(filters.Tag))
            {
                // Filter events to include only work items containing the specified tag.
                yield return new KeyValuePair<string, string>("tag", filters.Tag);
            }
            if (filters.Fields?.Any() ?? false)
            {
                // Filter events to include only work items with the specified field(s) changed
                yield return new KeyValuePair<string, string>("changedFields", string.Join(',', filters.Fields));
            }
        }


        public static IEnumerable<InputFilterCondition> ToFilterConditions(this EventFilters filters)
        {
            return filters.ToInputs()
                          .Select(input => new InputFilterCondition
                                           {
                                               InputId = input.Key,
                                               InputValue = input.Value,
                                               Operator = InputFilterOperator.Equals,
                                               CaseSensitive = false
                                           });
        }
    }
}

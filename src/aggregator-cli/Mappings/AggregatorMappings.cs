using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.FormInput;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;
using Microsoft.VisualStudio.Services.WebApi;

namespace aggregator.cli
{
    internal enum RemoveOutcome
    {
        Succeeded = 0,
        NotFound = 2,
        Failed = 1
    }

    internal enum UpdateOutcome
    {
        Succeeded = 0,
        NotFound = 2,
        Failed = 1
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
                        && s.ConsumerInputs.GetValue("url", "").StartsWith(
                            instance.FunctionAppUrl, StringComparison.OrdinalIgnoreCase))
                    : subscriptions.Where(s
                        => s.PublisherId == DevOpsEvents.PublisherId
                        // HACK
                        && s.ConsumerInputs.GetValue("url", "").IndexOf("aggregator.azurewebsites.net") > 8);
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
                Uri ruleUrl = new Uri(subscription.ConsumerInputs.GetValue("url", MagicConstants.MissingUrl));
                string ruleFullName = GetRuleFullName(ruleUrl);
                result.Add(
                    new MappingOutputData(instance, ruleFullName, ruleUrl.IsImpersonationEnabled(), foundProject.Name, subscription.EventType, subscription.Status.ToString())
                    );
            }
            return result;
        }

        private string GetRuleFullName(Uri ruleUri)
        {
            // HACK need to factor the URL<->rule_name
            string ruleName = ruleUri.Segments.LastOrDefault() ?? string.Empty;
            string ruleFullName = $"{naming.FromFunctionAppUrl(ruleUri).PlainName}/{ruleName}";
            return ruleFullName;
        }

        internal async Task<UpdateOutcome> RemapAsync(InstanceName sourceInstance, InstanceName destInstance, string projectName, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Searching aggregator mappings in Azure DevOps...");
            var serviceHooksClient = devops.GetClient<ServiceHooksPublisherHttpClient>();
            var subscriptions = await serviceHooksClient.QuerySubscriptionsAsync();
            var filteredSubs = subscriptions.Where(s
                        => s.PublisherId == DevOpsEvents.PublisherId
                        && s.ConsumerInputs.GetValue("url", "").StartsWith(
                            sourceInstance.FunctionAppUrl, StringComparison.OrdinalIgnoreCase));
            var projectClient = devops.GetClient<ProjectHttpClient>();
            var projects = await projectClient.GetProjects();
            var projectsDict = projects.ToDictionary(p => p.Id);

            int processedCount = 0;
            int succeededCount = 0;

            foreach (var subscription in filteredSubs)
            {
                var foundProject = projectsDict[
                    new Guid(subscription.PublisherInputs["projectId"])
                    ];
                if (!string.IsNullOrEmpty(projectName) && foundProject.Name != projectName)
                {
                    logger.WriteInfo($"Skipping mapping {subscription.Id} in project {projectName}");
                    continue;
                }
                if (subscription.Status != SubscriptionStatus.Enabled && subscription.Status != SubscriptionStatus.OnProbation)
                {
                    logger.WriteInfo($"Skipping mapping {subscription.Id} because has status {subscription.Status.ToString()}");
                    continue;
                }

                processedCount++;

                Uri ruleUrl = new Uri(subscription.ConsumerInputs.GetValue("url", MagicConstants.MissingUrl));
                string ruleName = ruleUrl.Segments.LastOrDefault() ?? string.Empty;

                var rules = new AggregatorRules(azure, logger);
                try
                {
                    var destRuleTarget = await rules.GetInvocationUrlAndKey(destInstance, ruleName, cancellationToken);
                    // PATCH the object
                    subscription.ConsumerInputs["url"] = destRuleTarget.url.AbsoluteUri;
                    subscription.ConsumerInputs["httpHeaders"] = $"{MagicConstants.AzureFunctionKeyHeaderName}:{destRuleTarget.key}";

                    logger.WriteVerbose($"Replacing {subscription.EventType} mapping from {ruleUrl.AbsoluteUri} to {subscription.Url}...");
                    try
                    {
                        var newSubscription = await serviceHooksClient.UpdateSubscriptionAsync(subscription);
                        logger.WriteInfo($"Event subscription {newSubscription.Id} updated.");
                        succeededCount++;
                    }
                    catch (Exception ex)
                    {
                        logger.WriteError($"Failed updating subscription {subscription.Id}: {ex.Message}.");
                    }
                }
                catch (Exception ex)
                {
                    logger.WriteError($"Destination rule {destInstance.PlainName}/{ruleName} does not exists or cannot retrieve key: {ex.Message}.");
                }

            }

#pragma warning disable S3358 // Extract this nested ternary operation into an independent statement
            return processedCount == 0 ? UpdateOutcome.NotFound
                : (processedCount > succeededCount) ? UpdateOutcome.Failed
                : UpdateOutcome.Succeeded;
#pragma warning restore S3358 // Extract this nested ternary operation into an independent statement
        }

        internal async Task<Guid> AddAsync(string projectName, string @event, EventFilters filters, InstanceName instance, string ruleName, bool impersonateExecution, CancellationToken cancellationToken)
        {
            async Task<(Uri, string)> RetrieveAzureFunctionUrl(string _ruleName, CancellationToken _cancellationToken)
            {
                var rules = new AggregatorRules(azure, logger);
                return await rules.GetInvocationUrlAndKey(instance, _ruleName, _cancellationToken);
            }

            return await CoreAddAsync(projectName, @event, filters, ruleName, impersonateExecution, RetrieveAzureFunctionUrl, MagicConstants.AzureFunctionKeyHeaderName, cancellationToken);
        }

        internal async Task<Guid> AddFromUrlAsync(string projectName, string @event, EventFilters filters, Uri targetUrl, string ruleName, bool impersonateExecution, CancellationToken cancellationToken)
        {
            async Task<(Uri, string)> RetrieveHostedUrl(string _ruleName, CancellationToken _cancellationToken)
            {
                string apiKey = MagicConstants.InvalidApiKey;

                logger.WriteVerbose($"Validating target URL {targetUrl.AbsoluteUri}");

                string userManagedPassword = Environment.GetEnvironmentVariable(MagicConstants.EnvironmentVariable_SharedSecret);
                if (string.IsNullOrEmpty(userManagedPassword))
                {
                    throw new InvalidOperationException($"{MagicConstants.EnvironmentVariable_SharedSecret} environment variable is required for this command");
                }

                string proof = SharedSecret.DeriveFromPassword(userManagedPassword);

                var configUrl = new UriBuilder(targetUrl);
                configUrl.Path += $"config/key";
                var handler = new HttpClientHandler()
                {
                    SslProtocols = SslProtocols.Tls12,// | SslProtocols.Tls11 | SslProtocols.Tls,
                    ServerCertificateCustomValidationCallback = delegate { return true; }
                };
                using (var client = new HttpClient(handler))
                using (var request = new HttpRequestMessage(HttpMethod.Post, configUrl.Uri))
                {
                    using (request.Content = new StringContent($"\"{proof}\"", Encoding.UTF8, "application/json"))
                    using (var response = await client.SendAsync(request, cancellationToken))
                    {
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                                logger.WriteVerbose($"Connection to {targetUrl} succeded");
                                apiKey = await response.Content.ReadAsStringAsync();
                                logger.WriteInfo($"Configuration retrieved.");
                                break;

                            default:
                                logger.WriteError($"{targetUrl} returned {response.ReasonPhrase}.");
                                break;
                        }//switch
                    }
                }

                if (string.IsNullOrEmpty(apiKey) || apiKey == MagicConstants.InvalidApiKey)
                {
                    throw new InvalidOperationException("Unable to retrieve API Key, please check Shared secret configuration");
                }

                var b = new UriBuilder(targetUrl);
                b.Path += $"workitem/{_ruleName}";
                return (b.Uri, apiKey);
            }

            return await CoreAddAsync(projectName, @event, filters, ruleName, impersonateExecution, RetrieveHostedUrl, MagicConstants.ApiKeyAuthenticationHeaderName, cancellationToken);
        }

#pragma warning disable S107 // Methods should not have too many parameters
        protected async Task<Guid> CoreAddAsync(string projectName, string @event, EventFilters filters, string ruleName, bool impersonateExecution, Func<string, CancellationToken, Task<(Uri, string)>> urlRetriever, string headerName, CancellationToken cancellationToken)
#pragma warning restore S107 // Methods should not have too many parameters
        {
            logger.WriteVerbose($"Reading Azure DevOps project data...");
            var projectClient = devops.GetClient<ProjectHttpClient>();
            var project = await projectClient.GetProject(projectName);
            logger.WriteInfo($"Project {projectName} data read.");

            logger.WriteVerbose($"Retrieving {ruleName} Function Key...");
            (Uri ruleUrl, string ruleKey) = await urlRetriever(ruleName, cancellationToken);
            logger.WriteInfo($"{ruleName} Function Key retrieved.");

            ruleUrl = ruleUrl.AddToUrl(impersonate: impersonateExecution);

            // check if the subscription already exists and bail out
            var query = new SubscriptionsQuery
            {
                PublisherId = DevOpsEvents.PublisherId,
                PublisherInputFilters = new InputFilter[] {
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
                    { "httpHeaders", $"{headerName}:{ruleKey}" },
                    // careful with casing!
                    { "resourceDetailsToSend", "all" },
                    { "messagesToSend", "none" },
                    { "detailedMessagesToSend", "none" },
                },
                EventType = @event,
                PublisherId = DevOpsEvents.PublisherId,
                PublisherInputs = new Dictionary<string, string>(filters.ToInputs())
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
                .Where(s => s.ConsumerInputs.GetValue("url", "").StartsWith(
                    instance.FunctionAppUrl, StringComparison.OrdinalIgnoreCase));
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
        public bool OnlyLinks { get; set; }
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
            if (filters.OnlyLinks)
            {
                // Filter events to include only work items with added or removed links
                yield return new KeyValuePair<string, string>("linksChanged", "true");
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

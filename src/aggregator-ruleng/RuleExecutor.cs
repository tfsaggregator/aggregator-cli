using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Http;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;
using Polly;

namespace aggregator.Engine
{
    public class RuleExecutor
    {
        protected readonly IAggregatorConfiguration configuration;

        protected readonly IAggregatorLogger logger;

        public RuleExecutor(IAggregatorLogger logger, IAggregatorConfiguration configuration)
        {
            this.configuration = configuration;
            this.logger = logger;
        }

        public async Task<string> ExecuteAsync(IRule rule, WorkItemEventContext eventContext, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Connecting to Azure DevOps using {configuration.DevOpsTokenType}...");
            if (configuration.DevOpsTokenType == DevOpsTokenType.PAT)
            {
                var clientCredentials = new VssBasicCredential(configuration.DevOpsTokenType.ToString(), configuration.DevOpsToken);
                // see https://rules.sonarsource.com/csharp/RSPEC-4457
                return await ExecAsyncImpl(rule, eventContext, clientCredentials, cancellationToken);
            }
            else
            {
                logger.WriteError($"Azure DevOps Token type {configuration.DevOpsTokenType} not supported!");
                throw new ArgumentException($"Azure DevOps Token type {configuration.DevOpsTokenType} not supported.");
            }
        }

        private async Task<string> ExecAsyncImpl(IRule rule, WorkItemEventContext eventContext, VssCredentials clientCredentials, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var devops = await ConnectToAzureDevOpsAsync(eventContext, clientCredentials, cancellationToken);
            using var clientsContext = new AzureDevOpsClientsContext(devops);
            var engine = new RuleEngine(logger, configuration.SaveMode, configuration.DryRun);

            var ruleResult = await engine.RunAsync(rule, eventContext.ProjectId, eventContext.WorkItemPayload, eventContext.EventType, clientsContext, cancellationToken);
            logger.WriteInfo(ruleResult);
            return ruleResult;
        }

        const int MaxRetries = 3;
        const int BaseRetryInterval = 30;

        private async Task<VssConnection> ConnectToAzureDevOpsAsync(WorkItemEventContext eventContext, VssCredentials clientCredentials, CancellationToken cancellationToken)
        {
            // see https://docs.microsoft.com/en-us/azure/devops/integrate/concepts/rate-limits#api-client-experience
            var policy = Policy
                .Handle<HttpRequestException>()
                // https://github.com/App-vNext/Polly/wiki/Retry#retryafter-when-the-response-specifies-how-long-to-wait
                .OrResult<HttpResponseMessage>(r => r.StatusCode == (HttpStatusCode)429)
                .WaitAndRetryAsync(
                    retryCount: MaxRetries,
                    sleepDurationProvider: (retryCount, response, context) => {
                        return response.Result?.Headers.RetryAfter.Delta.Value
                                ?? TimeSpan.FromSeconds(BaseRetryInterval * retryCount);
                    },
#pragma warning disable CS1998
                    onRetryAsync: async (response, timespan, retryCount, context) => {
                        logger.WriteInfo($"{Environment.NewLine}Waiting {timespan} before retrying (attemp #{retryCount}/{MaxRetries})...");
                    }
#pragma warning restore CS1998
                );
            var handler = new PolicyHttpMessageHandler(policy);

            var vssHandler = new VssHttpMessageHandler(clientCredentials, VssClientHttpRequestSettings.Default.Clone());

            var devops = new VssConnection(eventContext.CollectionUri, vssHandler, new DelegatingHandler[] { handler });
            try
            {
                await devops.ConnectAsync(cancellationToken);
                logger.WriteInfo($"Connected to Azure DevOps");
            }
            catch (System.Exception ex)
            {
                logger.WriteError(ex.Message);
                if (ex.InnerException != null)
                {
                    logger.WriteError(ex.InnerException.Message);
                }
                throw;
            }

            return devops;
        }
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

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
            var clientCredentials = default(VssCredentials);
            if (configuration.DevOpsTokenType == DevOpsTokenType.PAT)
            {
                clientCredentials = new VssBasicCredential(configuration.DevOpsTokenType.ToString(), configuration.DevOpsToken);
            }
            else
            {
                logger.WriteError($"Azure DevOps Token type {configuration.DevOpsTokenType} not supported!");
                throw new ArgumentOutOfRangeException(nameof(configuration.DevOpsTokenType));
            }

            cancellationToken.ThrowIfCancellationRequested();

            // TODO improve from https://github.com/Microsoft/vsts-work-item-migrator
            using (var devops = new VssConnection(eventContext.CollectionUri, clientCredentials))
            {
                await devops.ConnectAsync(cancellationToken);
                logger.WriteInfo($"Connected to Azure DevOps");
                using (var clientsContext = new AzureDevOpsClientsContext(devops))
                {
                    var engine = new RuleEngine(logger, configuration.SaveMode, configuration.DryRun);

                    var ruleResult = await engine.RunAsync(rule, eventContext.ProjectId, eventContext.WorkItemPayload, eventContext.EventType, clientsContext, cancellationToken);
                    logger.WriteInfo(ruleResult);
                    return ruleResult;
                }
            }
        }
    }
}

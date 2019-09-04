using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using aggregator.Engine;
using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.WebApi;

namespace aggregator
{
    /// <summary>
    /// Contains Aggregator specific code with no reference to Rule triggering
    /// </summary>
    internal class RuleWrapper
    {
        private readonly AggregatorConfiguration configuration;
        private readonly IAggregatorLogger logger;
        private readonly string ruleFilePath;

        public RuleWrapper(AggregatorConfiguration configuration, IAggregatorLogger logger, string ruleFilePath)
        {
            this.configuration = configuration;
            this.logger = logger;
            this.ruleFilePath = ruleFilePath;
        }

        internal async Task<string> ExecuteAsync(WorkItemEventContext eventContext, CancellationToken cancellationToken)
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
                    string[] ruleCode = await ReadAllLinesAsync(ruleFilePath);

                    var engine = new Engine.RuleEngine(logger, ruleCode, configuration.SaveMode, configuration.DryRun);

                    return await engine.ExecuteAsync(eventContext.ProjectId, eventContext.WorkItemPayload, clientsContext, cancellationToken);
                }
            }
        }

        private static async Task<string[]> ReadAllLinesAsync(string ruleFilePath)
        {
            using (var fileStream = File.OpenRead(ruleFilePath))
            {
                using (var streamReader = new StreamReader(fileStream))
                {
                    var lines = new List<string>();
                    string line;
                    while ((line = await streamReader.ReadLineAsync()) != null)
                    {
                        lines.Add(line);
                    }

                    return lines.ToArray();
                }
            }
        }
    }
}

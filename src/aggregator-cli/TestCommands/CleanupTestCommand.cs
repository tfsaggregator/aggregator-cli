using System.Threading;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;

namespace aggregator.cli
{
    [Verb("test.cleanup", HelpText = "Cleanup Azure resource.", Hidden = true)]
    class CleanupTestCommand : CommandBase
    {
        [Option('g', "resourceGroup", Required = true, HelpText = "Azure Resource Group hosting the Aggregator instance.")]
        public string ResourceGroup { get; set; }

        internal override async Task<int> RunAsync(CancellationToken cancellationToken)
        {
            var context = await Context
                .WithAzureLogon()
                .WithDevOpsLogon()
                .BuildAsync(cancellationToken);

            var rgName = context.Naming.GetResourceGroupName(ResourceGroup);

            Logger.WriteInfo($"Deleting all resources in {rgName}...");

            // tip from https://www.wintellect.com/how-to-remove-all-resources-in-a-resource-group-without-removing-the-group-on-azure/
            string armTemplateString = @"
{
  ""$schema"": ""https://schema.management.azure.com/schemas/2015-01-01/deploymentTemplate.json#"",
  ""contentVersion"": ""1.0.0.0"",
  ""parameters"": {},
  ""variables"": {},
  ""resources"": [],
  ""outputs"": {}
}
";
            string deploymentName = SdkContext.RandomResourceName("aggregator", 24);
            context.Azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(rgName)
                    .WithTemplate(armTemplateString)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Complete)
                    .Create();

            Logger.WriteInfo($"Resources deleted.");

            return ExitCodes.Success;
        }
    }
}

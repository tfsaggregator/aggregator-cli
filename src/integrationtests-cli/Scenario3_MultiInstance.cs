using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

[assembly: CollectionBehavior(DisableTestParallelization = true)]
// addset the custom test's collection orderer
[assembly: TestCollectionOrderer(CollectionPriorityOrderer.TypeName, CollectionPriorityOrderer.AssembyName)]

namespace integrationtests.cli
{
    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class Scenario3_MultiInstance : End2EndScenarioBase
    {
        public Scenario3_MultiInstance(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact, Order(1)]
        void Logon()
        {
            (int rc, string output) = RunAggregatorCommand(
                $"logon.azure --subscription {TestLogonData.SubscriptionId} --client {TestLogonData.ClientId} --password {TestLogonData.ClientSecret} --tenant {TestLogonData.TenantId}");
            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            (int rc2, string output2) = RunAggregatorCommand(
                $"logon.ado --url {TestLogonData.DevOpsUrl} --mode PAT --token {TestLogonData.PAT}");
            Assert.Equal(0, rc2);
            Assert.DoesNotContain("] Failed!", output2);
        }

        [Theory, Order(2)]
        [InlineData("my45")]
        [InlineData("my54")]
        void InstallInstances(string instancePrefix)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"install.instance --name {instance} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(3)]
        void ListInstances()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains("Instance my45", output);
            Assert.Contains("Instance my54", output);
        }

        [Theory, Order(4)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void AddRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"add.rule --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --name {rule} --file {rule}.rule");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(5)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void ListRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.rules --instance {instance} --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains($"Rule {instance}/{rule}", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(6)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void MapRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"map.rule --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(7)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void ListMappings(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.mappings --instance {instance} --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains($"invokes rule {instance}/{rule}", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(101)]
        void CreateWorkItemAndCheckTrigger()
        {
            // TODO
            Assert.True(true);
        }

        [Theory, Order(901)]
        [InlineData("my45")]
        void UninstallInstances(string instancePrefix)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"uninstall.instance --name {instance} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(902)]
        void ListInstancesAfterUninstall()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("Instance my45", output);
            Assert.Contains("Instance my54", output);
        }

        [Theory, Order(903)]
        [InlineData("my54", "test5")]
        void UnmapRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"unmap.rule --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(999)]
        void FinalCleanUp()
        {
            var credentials = SdkContext.AzureCredentialsFactory
                .FromServicePrincipal(
                    TestLogonData.ClientId,
                    TestLogonData.ClientSecret,
                    TestLogonData.TenantId,
                    AzureEnvironment.AzureGlobalCloud);
            var azure = Azure
                .Configure()
                .WithLogLevel(HttpLoggingDelegatingHandler.Level.None)
                .Authenticate(credentials)
                .WithSubscription(TestLogonData.SubscriptionId);

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
            azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(TestLogonData.ResourceGroup)
                    .WithTemplate(armTemplateString)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Complete)
                    .Create();

            Assert.True(true);
        }
    }
}

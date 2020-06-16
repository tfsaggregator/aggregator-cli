using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Core;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

namespace integrationtests.cli
{
    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class Scenario4_NamingTemplate : End2EndScenarioBase
    {
        string templateFile = "scenario4-namingtemplate.json";
        string resourceGroupName = "test";

        public Scenario4_NamingTemplate(ITestOutputHelper output)
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
        [InlineData("a")]
        [InlineData("b")]
        void InstallInstances(string instancePrefix)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"install.instance --verbose --namingTemplate {templateFile} --name {instance} --resourceGroup {resourceGroupName} --location {TestLogonData.Location}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {TestLogonData.RuntimeSourceUrl}"));

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(3)]
        void ListInstances()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --verbose --namingTemplate {templateFile} --resourceGroup {resourceGroupName}");

            Assert.Equal(0, rc);
            Assert.Contains("Instance a", output);
            Assert.Contains("Instance b", output);
        }

        [Theory, Order(4)]
        [InlineData("a", "test4")]
        [InlineData("b", "test5")]
        void AddRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"add.rule --verbose --namingTemplate {templateFile} --instance {instance} --resourceGroup {resourceGroupName} --name {rule} --file {rule}.rule");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(5)]
        [InlineData("a", "test4")]
        [InlineData("b", "test5")]
        void ListRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.rules --namingTemplate {templateFile} --instance {instance} --resourceGroup {resourceGroupName}");

            Assert.Equal(0, rc);
            Assert.Contains($"Rule {instance}/{rule}", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(6)]
        [InlineData("a", "test4")]
        [InlineData("b", "test5")]
        void MapRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"map.rule --verbose --namingTemplate {templateFile} --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {resourceGroupName} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(7)]
        [InlineData("a", "test4")]
        [InlineData("b", "test5")]
        void ListMappings(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.mappings --namingTemplate {templateFile} --instance {instance} --resourceGroup {resourceGroupName}");

            Assert.Equal(0, rc);
            Assert.Contains($"invokes rule {instance}/{rule}", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(101)]
        [InlineData("a", "test4")]
        void CreateWorkItemAndCheckTrigger(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"test.create --verbose --namingTemplate {templateFile} --resourceGroup {resourceGroupName} --instance {instance} --project \"{TestLogonData.ProjectName}\" ");
            Assert.Equal(0, rc);
            // Sample output from rule:
            //  Returning 'Hello Task #118 from Rule 5!' from 'test5'
            Assert.Contains($"Returning 'Hello Task #", output);
            Assert.Contains($"!' from '{rule}'", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(901)]
        [InlineData("a")]
        void UninstallInstances(string instancePrefix)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"uninstall.instance --verbose --namingTemplate {templateFile} --name {instance} --resourceGroup {resourceGroupName} --location {TestLogonData.Location}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(902)]
        void ListInstancesAfterUninstall()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --namingTemplate {templateFile} --resourceGroup {resourceGroupName}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("Instance a", output);
            Assert.Contains("Instance b", output);
        }

        [Theory, Order(903)]
        [InlineData("b", "test5")]
        void UnmapRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"unmap.rule --verbose --namingTemplate {templateFile} --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {resourceGroupName} --rule {rule}");

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
                    .WithExistingResourceGroup(resourceGroupName)
                    .WithTemplate(armTemplateString)
                    .WithParameters("{}")
                    .WithMode(DeploymentMode.Complete)
                    .Create();

            Assert.True(true);
        }
    }
}

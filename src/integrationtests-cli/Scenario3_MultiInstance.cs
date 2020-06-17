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
            (int rc, string output) = RunAggregatorCommand($"install.instance --verbose --name {instance} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {TestLogonData.RuntimeSourceUrl}"));

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
            (int rc, string output) = RunAggregatorCommand($"add.rule --verbose --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --name {rule} --file {rule}.rule");

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
            (int rc, string output) = RunAggregatorCommand($"map.rule --verbose --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --rule {rule}");

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

        [Theory, Order(101)]
        [InlineData("my45", "test4")]
        void CreateWorkItemAndCheckTrigger(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"test.create --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instance} --project \"{TestLogonData.ProjectName}\" ");
            Assert.Equal(0, rc);
            // Sample output from rule:
            //  Returning 'Hello Task #118 from Rule 5!' from 'test5'
            Assert.Contains($"Returning 'Hello Task #", output);
            Assert.Contains($"!' from '{rule}'", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(901)]
        [InlineData("my45")]
        void UninstallInstances(string instancePrefix)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"uninstall.instance --verbose --name {instance} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}");

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
            (int rc, string output) = RunAggregatorCommand($"unmap.rule --verbose --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {TestLogonData.ResourceGroup} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(999)]
        void FinalCleanUp()
        {
            (int rc, string output) = RunAggregatorCommand($"test.cleanup --verbose --resourceGroup {TestLogonData.ResourceGroup} ");
            Assert.Equal(0, rc);
        }
    }
}

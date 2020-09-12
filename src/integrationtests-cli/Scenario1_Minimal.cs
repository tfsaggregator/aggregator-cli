using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

namespace integrationtests.cli
{
    public abstract class Scenario1_Base : End2EndScenarioBase
    {
        protected readonly string instancePrefix = "mintest";
        protected readonly string ruleName = "test4";
        protected readonly string ruleFile = "test4.rule";
        protected readonly string instanceName;

        protected Scenario1_Base(ITestOutputHelper output)
            : base(output)
        {
            instanceName = instancePrefix + TestLogonData.UniqueSuffix;
        }
    }

    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class Scenario1_Minimal : Scenario1_Base
    {
        public Scenario1_Minimal(ITestOutputHelper output)
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


        [Fact, Order(10)]
        void InstallInstances()
        {
            (int rc, string output) = RunAggregatorCommand($"install.instance --verbose --name {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {TestLogonData.RuntimeSourceUrl}"));

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(20)]
        void AddRules()
        {
            (int rc, string output) = RunAggregatorCommand($"add.rule --verbose --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --name {ruleName} --file {ruleFile}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(30)]
        void MapRules()
        {
            (int rc, string output) = RunAggregatorCommand($"map.rule --verbose --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --rule {ruleName}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(40)]
        void CreateWorkItemAndCheckTrigger()
        {
            (int rc, string output) = RunAggregatorCommand($"test.create --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instanceName} --project \"{TestLogonData.ProjectName}\" ");
            Assert.Equal(0, rc);
            // Sample output from rule:
            //  Returning 'Hello Task #118 from Rule 5!' from 'TestRule5'
            Assert.Contains($"Returning 'Hello Task #", output);
            Assert.Contains($"!' from '{ruleName}'", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(99)]
        void FinalCleanUp()
        {
            (_, _) = RunAggregatorCommand($"unmap.rule --verbose --project \"{TestLogonData.ProjectName}\" --event * --rule * --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup}");
            (int rc, _) = RunAggregatorCommand($"test.cleanup --verbose --resourceGroup {TestLogonData.ResourceGroup} ");
            Assert.Equal(0, rc);
        }
    }
}

using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

namespace integrationtests.cli
{
    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class Scenario4_NamingTemplate : End2EndScenarioBase
    {
        const string TemplateFile = "scenario4-namingtemplate.json";
        const string ResourceGroupName = "test";

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
            (int rc, string output) = RunAggregatorCommand($"install.instance --verbose --namingTemplate {TemplateFile} --name {instance} --resourceGroup {ResourceGroupName} --location {TestLogonData.Location}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {TestLogonData.RuntimeSourceUrl}"));

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(3)]
        void ListInstances()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --verbose --namingTemplate {TemplateFile} --resourceGroup {ResourceGroupName}");

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
            (int rc, string output) = RunAggregatorCommand($"add.rule --verbose --namingTemplate {TemplateFile} --instance {instance} --resourceGroup {ResourceGroupName} --name {rule} --file {rule}.rule");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(5)]
        [InlineData("a", "test4")]
        [InlineData("b", "test5")]
        void ListRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.rules --namingTemplate {TemplateFile} --instance {instance} --resourceGroup {ResourceGroupName}");

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
            (int rc, string output) = RunAggregatorCommand($"map.rule --verbose --namingTemplate {TemplateFile} --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {ResourceGroupName} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(7)]
        [InlineData("a", "test4")]
        [InlineData("b", "test5")]
        void ListMappings(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.mappings --namingTemplate {TemplateFile} --instance {instance} --resourceGroup {ResourceGroupName}");

            Assert.Equal(0, rc);
            Assert.Contains($"invokes rule {instance}/{rule}", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(101)]
        [InlineData("a", "test4")]
        void CreateWorkItemAndCheckTrigger(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"test.create --verbose --namingTemplate {TemplateFile} --resourceGroup {ResourceGroupName} --instance {instance} --project \"{TestLogonData.ProjectName}\" ");
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
            (int rc, string output) = RunAggregatorCommand($"uninstall.instance --verbose --namingTemplate {TemplateFile} --name {instance} --resourceGroup {ResourceGroupName} --location {TestLogonData.Location}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(902)]
        void ListInstancesAfterUninstall()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --namingTemplate {TemplateFile} --resourceGroup {ResourceGroupName}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("Instance a", output);
            Assert.Contains("Instance b", output);
        }

        [Theory, Order(903)]
        [InlineData("b", "test5")]
        void UnmapRules(string instancePrefix, string rule)
        {
            string instance = instancePrefix + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"unmap.rule --verbose --namingTemplate {TemplateFile} --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instance} --resourceGroup {ResourceGroupName} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(999)]
        void FinalCleanUp()
        {
            (int rc, _) = RunAggregatorCommand($"test.cleanup --verbose --namingTemplate {TemplateFile} --resourceGroup {ResourceGroupName} ");
            Assert.Equal(0, rc);
        }
    }
}

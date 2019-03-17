using System.IO;
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

        const string location = "westeurope";
        const string resourceGroup = "test-aggregator45";
        const string project = "WorkItemTracking";

        [Fact, Order(1)]
        void Logon()
        {
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText("logon-data.json"));

            (int rc, string output) = RunAggregatorCommand(
                $"logon.azure --subscription {data.subscription} --client {data.client} --password {data.password} --tenant {data.tenant}");
            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            (int rc2, string output2) = RunAggregatorCommand(
                $"logon.ado --url {data.devopsUrl} --mode PAT --token {data.pat}");
            Assert.Equal(0, rc2);
            Assert.DoesNotContain("] Failed!", output2);
        }

        [Theory, Order(2)]
        [InlineData("my45")]
        [InlineData("my54")]
        void InstallInstances(string instance)
        {
            (int rc, string output) = RunAggregatorCommand($"install.instance --name {instance} --resourceGroup {resourceGroup} --location {location}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(3)]
        void ListInstances()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {resourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains("Instance my45", output);
            Assert.Contains("Instance my54", output);
        }

        [Theory, Order(4)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void AddRules(string instance, string rule)
        {
            (int rc, string output) = RunAggregatorCommand($"add.rule --instance {instance} --resourceGroup {resourceGroup} --name {rule} --file {rule}.rule");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(5)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void ListRules(string instance, string rule)
        {
            (int rc, string output) = RunAggregatorCommand($"list.rules --instance {instance} --resourceGroup {resourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains($"Rule {instance}/{rule}", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(6)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void MapRules(string instance, string rule)
        {
            (int rc, string output) = RunAggregatorCommand($"map.rule --project {project} --event workitem.created --instance {instance} --resourceGroup {resourceGroup} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(7)]
        [InlineData("my45", "test4")]
        [InlineData("my54", "test5")]
        void ListMappings(string instance, string rule)
        {
            (int rc, string output) = RunAggregatorCommand($"list.mappings --instance {instance} --resourceGroup {resourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains($"invokes rule {instance}/{rule}", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Theory, Order(8)]
        [InlineData("my45")]
        void UninstallInstances(string instance)
        {
            (int rc, string output) = RunAggregatorCommand($"uninstall.instance --name {instance} --resourceGroup {resourceGroup} --location {location}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(9)]
        void ListInstancesAfterUninstall()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {resourceGroup}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("Instance my45", output);
            Assert.Contains("Instance my54", output);
        }

        [Theory, Order(10)]
        [InlineData("my54", "test5")]
        void UnmapRules(string instance, string rule)
        {
            (int rc, string output) = RunAggregatorCommand($"unmap.rule --project {project} --event workitem.created --instance {instance} --resourceGroup {resourceGroup} --rule {rule}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }
    }
}

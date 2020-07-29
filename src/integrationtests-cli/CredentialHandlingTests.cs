using System.IO;
using aggregator.cli;
using Microsoft.TeamFoundation.Common;
using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

namespace integrationtests.cli
{
    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class CredentialHandlingTests : End2EndScenarioBase
    {
        public CredentialHandlingTests(ITestOutputHelper output)
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

        [Fact, Order(3)]
        void ListInstances()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains("No aggregator instances found", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(7)]
        void ListMappings()
        {
            string instance = "my45" + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.mappings --instance {instance}--resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(3, rc);
            Assert.Contains("No rule mappings found", output);
            Assert.DoesNotContain("] Failed!", output);
        }


        [Fact, Order(10)]
        void Logoff()
        {
            (int rc, string output) = RunAggregatorCommand($"logoff --verbose");
            bool isEmpty = Directory.GetFiles(LocalAppData.GetDirectory(), "*.dat").IsNullOrEmpty();

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            Assert.True(isEmpty);
        }

        [Fact, Order(21)]
        void ListInstancesAfterLogoff()
        {
            (int rc, string output) = RunAggregatorCommand($"list.instances --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(99, rc);
            Assert.Contains("No cached Azure credential", output);
        }

        [Fact, Order(23)]
        void ListMappingsAfterLogoff()
        {
            string instance = "my45" + TestLogonData.UniqueSuffix;
            (int rc, string output) = RunAggregatorCommand($"list.mappings --instance {instance}--resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(99, rc);
            Assert.Contains("No cached Azure DevOps credential", output);
        }

    }
}

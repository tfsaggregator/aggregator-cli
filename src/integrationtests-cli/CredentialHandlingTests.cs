using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;
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
        async Task Logon()
        {
            (int rc, string output) = await RunAggregatorCommand(
                $"logon.azure --verbose --subscription {TestLogonData.SubscriptionId} --client {TestLogonData.ClientId} --password {TestLogonData.ClientSecret} --tenant {TestLogonData.TenantId}");
            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            (int rc2, string output2) = await RunAggregatorCommand(
                $"logon.ado --verbose --url {TestLogonData.DevOpsUrl} --mode PAT --token {TestLogonData.PAT}");
            Assert.Equal(0, rc2);
            Assert.DoesNotContain("] Failed!", output2);
        }

        [Fact, Order(3)]
        async Task ListInstances()
        {
            (int rc, string output) = await RunAggregatorCommand($"list.instances --verbose --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(0, rc);
            Assert.Contains("No aggregator instances found", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(7)]
        async Task ListMappings()
        {
            string instance = "my45" + TestLogonData.UniqueSuffix;
            (int rc, string output) = await RunAggregatorCommand($"list.mappings --verbose --instance {instance}--resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(3, rc);
            Assert.Contains("No rule mappings found", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(10)]
        async Task Logoff()
        {
            (int rc, string output) = await RunAggregatorCommand($"logoff --verbose");
            bool isEmpty = Directory.GetFiles(LocalAppData.GetDirectory(), "*.dat").IsNullOrEmpty();

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            Assert.True(isEmpty);
        }

        [Fact, Order(21)]
        async Task ListInstancesAfterLogoff()
        {
            (int rc, string output) = await RunAggregatorCommand($"list.instances --verbose --resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(99, rc);
            Assert.Contains("No cached Azure credential", output);
        }

        [Fact, Order(23)]
        async Task ListMappingsAfterLogoff()
        {
            string instance = "my45" + TestLogonData.UniqueSuffix;
            (int rc, string output) = await RunAggregatorCommand($"list.mappings --verbose --instance {instance}--resourceGroup {TestLogonData.ResourceGroup}");

            Assert.Equal(99, rc);
            Assert.Contains("No cached Azure DevOps credential", output);
        }

        [Fact, Order(31)]
        async Task LogonEnv()
        {
            (int rc, string output) = await RunAggregatorCommand($"logon.env --verbose", new List<(string, string)> {
                ("AGGREGATOR_SUBSCRIPTIONID",TestLogonData.SubscriptionId),
                ("AGGREGATOR_CLIENTID",TestLogonData.ClientId),
                ("AGGREGATOR_CLIENTSECRET",TestLogonData.ClientSecret),
                ("AGGREGATOR_TENANTID",TestLogonData.TenantId),
                ("AGGREGATOR_AZDO_URL",TestLogonData.DevOpsUrl),
                ("AGGREGATOR_AZDO_MODE","PAT"),
                ("AGGREGATOR_AZDO_TOKEN",TestLogonData.PAT),
            });
            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(33)]
        [SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "Code shared")]

        async Task ListInstancesAfterLogonEnv()
        {
            ListInstances();
        }

        [Fact, Order(37)]
        [SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "Code shared")]
        async Task ListMappingsAfterLogonEnv()
        {
            ListMappings();
        }

        [Fact, Order(39)]
        [SuppressMessage("Blocker Code Smell", "S2699:Tests should include assertions", Justification = "Code shared")]
        async Task LogoffAfterLogonEnv()
        {
            Logoff();
        }
    }
}

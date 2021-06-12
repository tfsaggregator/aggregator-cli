using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

namespace integrationtests.cli
{
    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class AddRuleTests : End2EndScenarioBase
    {
        public AddRuleTests(ITestOutputHelper output)
            : base(output)
        {
            // does nothing
        }

        [Fact, Order(1)]
        async Task Logon()
        {
            (int rc, string output) = await RunAggregatorCommand(
                $"logon.azure --subscription {TestLogonData.SubscriptionId} --client {TestLogonData.ClientId} --password {TestLogonData.ClientSecret} --tenant {TestLogonData.TenantId}");
            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            (int rc2, string output2) = await RunAggregatorCommand(
                $"logon.ado --url {TestLogonData.DevOpsUrl} --mode PAT --token {TestLogonData.PAT}");
            Assert.Equal(0, rc2);
            Assert.DoesNotContain("] Failed!", output2);
        }

        [Fact, Order(2)]
        public async Task GivenAnInvalidRuleFile_WhenAddingThisRule_ThenTheProcessing_ShouldBeAborted()
        {
            //Given
            const string invalidRuleFileName = "invalid_rule1.rule";

            //When
            (int rc, string output) = await RunAggregatorCommand(FormattableString.Invariant($"add.rule --verbose --instance foobar --resourceGroup foobar --name foobar --file {invalidRuleFileName}"));

            //Then
            Assert.Equal(1, rc);
            Assert.Contains(@"Errors in the rule file invalid_rule1.rule:", output);
        }
    }
}

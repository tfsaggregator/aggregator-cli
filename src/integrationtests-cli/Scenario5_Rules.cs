using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

namespace integrationtests.cli
{

    public class Scenario5WorkItem
    {
        public int WorkItemId;
    }

    public abstract class Scenario5_Base : End2EndScenarioBase, IClassFixture<Scenario5WorkItem>
    {
        protected readonly string instancePrefix = "rulestest";
        protected readonly string ruleName = "test6";
        protected readonly string ruleFile = "test6.rule";
        protected readonly string instanceName;
        protected Scenario5WorkItem wiData;
        protected Scenario5_Base(Scenario5WorkItem wiData, ITestOutputHelper output)
            : base(output)
        {
            instanceName = instancePrefix + TestLogonData.UniqueSuffix;
            if (TestLogonData.WorkItemId > 0) wiData.WorkItemId = TestLogonData.WorkItemId;
            this.wiData = wiData;
        }
    }

    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class Scenario5_Rules : Scenario5_Base
    {
        public Scenario5_Rules(Scenario5WorkItem wiData, ITestOutputHelper output)
            : base(wiData, output)
        {
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


        [Fact, Order(10)]
        async Task InstallInstance()
        {
            (int rc, string output) = await RunAggregatorCommand($"install.instance --verbose --name {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {TestLogonData.RuntimeSourceUrl}"));

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(20)]
        async Task AddRule()
        {
            (int rc, string output) = await RunAggregatorCommand($"add.rule --verbose --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --name {ruleName} --file {ruleFile}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(30)]
        async Task MapRuleForCreate()
        {
            (int rc, string output) = await RunAggregatorCommand($"map.rule --verbose --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --rule {ruleName}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(32)]
        async Task MapRuleForUpdate()
        {
            (int rc, string output) = await RunAggregatorCommand($"map.rule --verbose --project \"{TestLogonData.ProjectName}\" --event workitem.updated --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --rule {ruleName}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(40)]
        async Task CreateWorkItemAndCheckTrigger()
        {
            (int retVal, string output) = await RunAggregatorCommand($"test.create --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instanceName} --project \"{TestLogonData.ProjectName}\"  --rule {ruleName}  --returnId");
            wiData.WorkItemId = retVal;
            //+DEBUG
            System.Console.WriteLine($"WorkItemId is {wiData.WorkItemId}");
            WriteLineToOutput($"WorkItemId is {wiData.WorkItemId}");
            //-DEBUG
            Assert.NotEqual(0, wiData.WorkItemId);
            Assert.Contains($"Returning 'Hello Task #{wiData.WorkItemId} from Rule", output);
            Assert.Contains($" from '{ruleName}'", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(50)]
        async Task TriggerRuleTransitionToStateClosed()
        {
            //+DEBUG
            System.Console.WriteLine($"WorkItemId is {wiData.WorkItemId}");
            WriteLineToOutput($"WorkItemId is {wiData.WorkItemId}");
            //-DEBUG
            string newValue = "CloseMe";
            (int rc, string output) = await RunAggregatorCommand($"test.update --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instanceName} --project \"{TestLogonData.ProjectName}\"  --id {wiData.WorkItemId}  --title \"{newValue}\"  --rule {ruleName} ");
            Assert.Equal(0, rc);
            Assert.Contains($"TransitionToState Closed", output);
            Assert.Contains($"WorkItem #{wiData.WorkItemId} state will change from 'New' to 'Closed' when Rule exits", output);
            Assert.DoesNotContain($"TransitionToState failed!", output);
        }

        [Fact, Order(51)]
        async Task TriggerRuleTransitionFromClosedToRemoved()
        {
            string newValue = "DropMe";
            (int rc, string output) = await RunAggregatorCommand($"test.update --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instanceName} --project \"{TestLogonData.ProjectName}\"  --id {wiData.WorkItemId}  --title \"{newValue}\"  --rule {ruleName} ");
            Assert.Equal(0, rc);
            Assert.Contains($"Transitioning WorkItem #{wiData.WorkItemId} from 'Closed' to 'Active' succeeded", output);
            Assert.Contains($"Transitioning WorkItem #{wiData.WorkItemId} from 'Active' to 'Removed' succeeded", output);
            Assert.Contains($"TransitionToState Removed", output);
            Assert.DoesNotContain($"TransitionToState failed!", output);
        }

        [Fact, Order(99)]
        async Task FinalCleanUp()
        {
            (_, _) = await RunAggregatorCommand($"unmap.rule --verbose --project \"{TestLogonData.ProjectName}\" --event * --rule * --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup}");
            (int rc, _) = await RunAggregatorCommand($"test.cleanup --verbose --resourceGroup {TestLogonData.ResourceGroup} ");
            Assert.Equal(0, rc);
        }
    }
}

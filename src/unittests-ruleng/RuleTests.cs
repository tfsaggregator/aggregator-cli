using System;
using System.Collections.Generic;
using System.Text;
using aggregator;
using aggregator.Engine;
using aggregator.unittests;
using Xunit;

namespace unittests_ruleng
{
    static class StringExtensions
    {
        internal static string[] Mince(this string ruleCode)
        {
            return ruleCode.Split(Environment.NewLine);
        }
    }

    public class RuleTests
    {
        const string collectionUrl = "https://dev.azure.com/fake-organization";
        Guid projectId = Guid.NewGuid();
        const string projectName = "test-project";
        const string personalAccessToken = "***personalAccessToken***";
        FakeWorkItemTrackingHttpClient client = new FakeWorkItemTrackingHttpClient(new Uri($"{collectionUrl}"), null);
        MockAggregatorLogger logger = new MockAggregatorLogger();

        [Fact]
        public async void HelloWorldRule_Succeeds()
        {
            int workItemId = 42;
            string ruleCode = @"
return $""Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Item);
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal("Hello Bug #42 - Hello!", result);
        }

        [Fact]
        public async void LanguageDirective_Succeeds()
        {
            int workItemId = 42;
            string ruleCode = @".lang=CS
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Item);
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal(EngineState.Success, engine.State);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async void LanguageDirective_Fails()
        {
            int workItemId = 42;
            string ruleCode = @".lang=WHAT
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Item);
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal(EngineState.Error, engine.State);
        }

        [Fact]
        public async void Parent_Succeeds()
        {
            int workItemId = 42;
            string ruleCode = @"
string message = """";
var parent = self.Parent;
if (parent != null)
{
    message = $""Parent is {parent.Id}"";
}
return message;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Item);
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal("Parent is 1", result);
        }

        [Fact]
        public async void New_Succeeds()
        {
            int workItemId = 1;
            string ruleCode = @"
var wi = store.NewWorkItem(""Task"");
wi.Title = ""Brand new"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Item);
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Null(result);
            Assert.Contains(
                logger.GetMessages(),
                m => m.Message == "Changes saved to Azure DevOps (mode Item): 1 created, 0 updated."
                    && m.Level == "Info");
        }

        [Fact]
        public async void AddChild_Succeeds()
        {
            int workItemId = 1;
            string ruleCode = @"
var parent = self;
var newChild = store.NewWorkItem(""Task"");
newChild.Title = ""Brand new"";
parent.Relations.AddChild(newChild);
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Item);
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Null(result);
            Assert.Contains(
                logger.GetMessages(),
                m => m.Message == "Changes saved to Azure DevOps (mode Item): 1 created, 1 updated."
                    && m.Level == "Info");
        }
    }
}

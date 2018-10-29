using System;
using System.Collections.Generic;
using System.Text;
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
        [Fact]
        public async void HelloWorldRule_Succeeds()
        {
            string collectionUrl = "https://dev.azure.com/fake-organization";
            Guid projectId = Guid.NewGuid();
            var baseUrl = new Uri($"{collectionUrl}");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            int workItemId = 42;
            string ruleCode = @"
return $""Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince());
            string result = await engine.ExecuteAsync(collectionUrl, projectId, workItemId, client);

            Assert.Equal("Hello Bug #42 - Hello!", result);
        }

        [Fact]
        public async void LanguageDirective_Succeeds()
        {
            string collectionUrl = "https://dev.azure.com/fake-organization";
            Guid projectId = Guid.NewGuid();
            var baseUrl = new Uri($"{collectionUrl}");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            int workItemId = 42;
            string ruleCode = @".lang=CS
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince());
            string result = await engine.ExecuteAsync(collectionUrl, projectId, workItemId, client);

            Assert.Equal(EngineState.Success, engine.State);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async void LanguageDirective_Fails()
        {
            string collectionUrl = "https://dev.azure.com/fake-organization";
            Guid projectId = Guid.NewGuid();
            var baseUrl = new Uri($"{collectionUrl}");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            int workItemId = 42;
            string ruleCode = @".lang=WHAT
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince());
            string result = await engine.ExecuteAsync(collectionUrl, projectId, workItemId, client);

            Assert.Equal(EngineState.Error, engine.State);
        }

        [Fact]
        public async void Parent_Succeeds()
        {
            string collectionUrl = "https://dev.azure.com/fake-organization";
            Guid projectId = Guid.NewGuid();
            var baseUrl = new Uri($"{collectionUrl}");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
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

            var engine = new RuleEngine(logger, ruleCode.Mince());
            string result = await engine.ExecuteAsync(collectionUrl, projectId, workItemId, client);

            Assert.Equal("Parent is 1", result);
        }

        [Fact]
        public async void New_Succeeds()
        {
            string collectionUrl = "https://dev.azure.com/fake-organization";
            Guid projectId = Guid.NewGuid();
            var baseUrl = new Uri($"{collectionUrl}");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            int workItemId = 1;
            string ruleCode = @"
var wi = store.NewWorkItem();
wi.Title = ""Brand new"";
var rel = new WorkItemRelationWrapper(wi, CoreRelationRefNames.Parent, self.Url);
self.ChildrenLinks.AddRelation(rel);
";

            var engine = new RuleEngine(logger, ruleCode.Mince());
            string result = await engine.ExecuteAsync(collectionUrl, projectId, workItemId, client);

            Assert.Equal("Children are ,42,99", result);
        }
    }
}

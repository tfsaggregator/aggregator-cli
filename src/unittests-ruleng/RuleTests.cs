using System;
using System.Collections.Generic;
using System.Text;
using aggregator.Engine;
using aggregator.unittests;
using Xunit;

namespace unittests_ruleng
{
    public class RuleTests
    {
        [Fact]
        public async void HelloWorldRule_Succeeds()
        {
            string collectionUrl = "https://dev.azure.com/fake-account";
            var baseUrl = new Uri($"{collectionUrl}/fake-project");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            int workItemId = 42;
            string ruleCode = @"
return $""Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"";
";

            var engine = new RuleEngine(logger, ruleCode);
            string result = await engine.ExecuteAsync(collectionUrl, workItemId, client);

            Assert.Equal("Hello Bug #42 - Hello!", result);
        }

        [Fact]
        public async void Parent_Succeeds()
        {
            string collectionUrl = "https://dev.azure.com/fake-account";
            var baseUrl = new Uri(collectionUrl);
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

            var engine = new RuleEngine(logger, ruleCode);
            string result = await engine.ExecuteAsync(collectionUrl, workItemId, client);

            Assert.Equal("Parent is 1", result);
        }

        [Fact]
        public async void Children_Succeeds()
        {
            string collectionUrl = "https://dev.azure.com/fake-account";
            var baseUrl = new Uri(collectionUrl);
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            int workItemId = 1;
            string ruleCode = @"
string message = ""Children are "";
var parent = self;
var children = parent.Children;
foreach (var child in children) {
    message += $"",{child.Id}"";
}
return message;
";

            var engine = new RuleEngine(logger, ruleCode);
            string result = await engine.ExecuteAsync(collectionUrl, workItemId, client);

            Assert.Equal("Children are ,42,99", result);
        }
    }
}

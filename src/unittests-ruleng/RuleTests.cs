using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using aggregator;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NSubstitute;
using unittests_ruleng.TestData;
using Xunit;

namespace unittests_ruleng
{
    internal static class StringExtensions
    {
        internal static string[] Mince(this string ruleCode)
        {
            return ruleCode.Split(new string[] {"\r\n","\r","\n" }, StringSplitOptions.None);
        }
    }

    public class RuleTests
    {
        private const string CollectionUrl = "https://dev.azure.com/fake-organization";
        private readonly Guid projectId = Guid.NewGuid();
        private const string ProjectName = "test-project";
        private readonly IAggregatorLogger logger = Substitute.For<IAggregatorLogger>();
        private readonly WorkItemTrackingHttpClient client;
        private readonly string workItemsBaseUrl = $"{CollectionUrl}/{ProjectName}/_apis/wit/workItems";


        public RuleTests()
        {
            client = Substitute.For<WorkItemTrackingHttpClient>(new Uri(CollectionUrl), null);
            client.ExecuteBatchRequest(default).ReturnsForAnyArgs(info => new List<WitBatchResponse>());
        }

        [Fact]
        public async Task HelloWorldRule_Succeeds()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
return $""Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Equal("Hello Bug #42 - Hello!", result);
            await client.DidNotReceive().GetWorkItemAsync(Arg.Any<int>(), expand: Arg.Any<WorkItemExpand>());
        }

        [Fact]
        public async Task LanguageDirective_Succeeds()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @".lang=CS
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Equal(EngineState.Success, engine.State);
            Assert.Equal(string.Empty, result);
            await client.DidNotReceive().GetWorkItemAsync(Arg.Any<int>(), expand: Arg.Any<WorkItemExpand>());
        }

        [Fact]
        public async Task LanguageDirective_Fails()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @".lang=WHAT
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Equal(EngineState.Error, engine.State);
        }

        [Fact]
        public async Task Parent_Succeeds()
        {
            int workItemId2 = 2;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId2,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                },
                Relations = new List<WorkItemRelation>
                {
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Reverse",
                        Url = $"{workItemsBaseUrl}/1"
                    }
                },
            };
            client.GetWorkItemAsync(workItemId2, expand: WorkItemExpand.All).Returns(workItem);
            int workItemId1 = 1;
            client.GetWorkItemAsync(1, expand: WorkItemExpand.All).Returns(new WorkItem
            {
                Id = workItemId1,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "User Story" },
                    { "System.TeamProject", ProjectName },
                },
                Relations = new List<WorkItemRelation>
                {
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Forward",
                        Url = FormattableString.Invariant($"{workItemsBaseUrl}/{workItemId2}")
                    }
                },
            });
            string ruleCode = @"
string message = """";
var parent = self.Parent;
if (parent != null)
{
    message = $""Parent is {parent.Id}"";
}
return message;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Equal("Parent is 1", result);
            await client.Received(1).GetWorkItemAsync(Arg.Is(workItemId1), expand: Arg.Is(WorkItemExpand.All));
        }

        [Fact]
        public async Task New_Succeeds()
        {
            int workItemId = 1;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "User Story" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
var wi = store.NewWorkItem(""Task"");
wi.Title = ""Brand new"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Null(result);
            logger.Received().WriteInfo($"Found a request for a new Task workitem in {ProjectName}");
            logger.Received().WriteWarning("Dry-run mode: no updates sent to Azure DevOps.");
            logger.Received().WriteInfo("Changes saved to Azure DevOps (mode Default): 1 created, 0 updated.");
        }

        [Fact]
        public async Task AddChild_Succeeds()
        {
            int workItemId = 1;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "User Story" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
var parent = self;
var newChild = store.NewWorkItem(""Task"");
newChild.Title = ""Brand new"";
parent.Relations.AddChild(newChild);
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Null(result);
            logger.Received().WriteInfo($"Found a request for a new Task workitem in {ProjectName}");
            logger.Received().WriteInfo($"Found a request to update workitem {workItemId} in {ProjectName}");
            logger.Received().WriteWarning("Dry-run mode: no updates sent to Azure DevOps.");
            logger.Received().WriteInfo("Changes saved to Azure DevOps (mode Default): 1 created, 1 updated.");
        }

        [Fact]
        public async Task TouchDescription_Succeeds()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.Description", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
self.Description = self.Description + ""."";
return self.Description;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Equal("Hello.", result);
            logger.Received().WriteInfo($"Found a request to update workitem {workItemId} in {ProjectName}");
            logger.Received().WriteWarning("Dry-run mode: no updates sent to Azure DevOps.");
            logger.Received().WriteInfo("Changes saved to Azure DevOps (mode Default): 0 created, 1 updated.");
        }

        [Fact]
        public async Task ReferenceDirective_Succeeds()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @".r=System.Xml.XDocument
var doc = new System.Xml.Linq.XDocument();
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Equal(EngineState.Success, engine.State);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task ImportDirective_Succeeds()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @".import=System.Diagnostics
Debug.WriteLine(""test"");
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);

            Assert.Equal(EngineState.Success, engine.State);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async Task ImportDirective_Fail()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
Debug.WriteLine(""test"");
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            await Assert.ThrowsAsync<Microsoft.CodeAnalysis.Scripting.CompilationErrorException>(
                () => engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None)
            );
        }

        [Fact]
        public async Task DeleteWorkItem()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
Debug.WriteLine(""test"");
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            await Assert.ThrowsAsync<Microsoft.CodeAnalysis.Scripting.CompilationErrorException>(
                () => engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None)
            );
        }

        [Fact]
        public async Task HelloWorldRuleOnUpdate_Succeeds()
        {
            var workItem = ExampleTestData.Instance.WorkItem;
            var workItemUpdate = ExampleTestData.Instance.WorkItemUpdateFields;

            client.GetWorkItemAsync(workItem.Id.Value, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
return $""Hello #{ selfChanges.WorkItemId } - Update { selfChanges.Id } changed Title from { selfChanges.Fields[""System.Title""].OldValue } to { selfChanges.Fields[""System.Title""].NewValue }!"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, new WorkItemData(workItem, workItemUpdate), client, CancellationToken.None);

            Assert.Equal("Hello #22 - Update 3 changed Title from Initial Title to Hello!", result);
            await client.DidNotReceive().GetWorkItemAsync(Arg.Any<int>(), expand: Arg.Any<WorkItemExpand>());
        }

        [Fact]
        public async Task DocumentationRuleOnUpdateExample_Succeeds()
        {
            var workItem = ExampleTestData.Instance.WorkItem;
            var workItemUpdate = ExampleTestData.Instance.WorkItemUpdateFields;

            client.GetWorkItemAsync(workItem.Id.Value, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
            if (selfChanges.Fields.ContainsKey(""System.Title""))
            {
                var titleUpdate = selfChanges.Fields[""System.Title""];
                return $""Title was changed from '{titleUpdate.OldValue}' to '{titleUpdate.NewValue}'"";
            }
            else
            {
                return ""Title was not updated"";
            }
            ";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, new WorkItemData(workItem, workItemUpdate), client, CancellationToken.None);

            Assert.Equal("Title was changed from 'Initial Title' to 'Hello'", result);
            await client.DidNotReceive().GetWorkItemAsync(Arg.Any<int>(), expand: Arg.Any<WorkItemExpand>());
        }

        [Fact]
        public async Task CustomStringField_HasValue_Succeeds()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                    { "MyOrg.CustomStringField", "some value" },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
var customField = self.GetFieldValue<string>(""MyOrg.CustomStringField"", ""MyDefault"");
return customField;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);
            Assert.Equal("some value", result);
        }

        [Fact]
        public async Task CustomStringField_NoValue_ReturnsDefault()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
var customField = self.GetFieldValue<string>(""MyOrg.CustomStringField"", ""MyDefault"");
return customField;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);
            Assert.Equal("MyDefault", result);
        }

        [Fact]
        public async Task CustomNumericField_HasValue_Succeeds()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                    { "MyOrg.CustomNumericField", 42 },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
var customField = self.GetFieldValue<decimal>(""MyOrg.CustomNumericField"", 3.0m);
return customField.ToString(""N2"");
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);
            Assert.Equal("42.00", result);
        }

        [Fact]
        public async Task CustomNumericField_NoValue_ReturnsDefault()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                    { "System.TeamProject", ProjectName },
                }
            };
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(workItem);
            string ruleCode = @"
var customField = self.GetFieldValue<decimal>(""MyOrg.CustomNumericField"", 3.0m);
return customField.ToString(""N2"");
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default, dryRun: true);
            string result = await engine.ExecuteAsync(projectId, workItem, client, CancellationToken.None);
            Assert.Equal("3.00", result);
        }

    }
}

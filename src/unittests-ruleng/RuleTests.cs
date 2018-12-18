using System;
using System.Collections.Generic;
using System.Text;
using aggregator;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using NSubstitute;
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
        IAggregatorLogger logger = Substitute.For<IAggregatorLogger>();
        WorkItemTrackingHttpClientBase client = Substitute.For<WorkItemTrackingHttpClientBase>(new Uri($"{collectionUrl}"), null);
        string workItemsBaseUrl = $"{collectionUrl}/{projectName}/_apis/wit/workItems";


        [Fact]
        public async void HelloWorldRule_Succeeds()
        {
            int workItemId = 42;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                }
            });
            string ruleCode = @"
return $""Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default)
            {
                DryRun = true
            };
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal("Hello Bug #42 - Hello!", result);
        }

        [Fact]
        public async void LanguageDirective_Succeeds()
        {
            int workItemId = 42;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                }
            });
            string ruleCode = @".lang=CS
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default)
            {
                DryRun = true
            };
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal(EngineState.Success, engine.State);
            Assert.Equal(string.Empty, result);
        }

        [Fact]
        public async void LanguageDirective_Fails()
        {
            int workItemId = 42;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                }
            });
            string ruleCode = @".lang=WHAT
return string.Empty;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default)
            {
                DryRun = true
            };
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal(EngineState.Error, engine.State);
        }

        [Fact]
        public async void Parent_Succeeds()
        {
            int workItemId = 2;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                },
                Relations = new List<WorkItemRelation>()
                {
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Reverse",
                        Url = $"{workItemsBaseUrl}/1"
                    }
                },
            });
            client.GetWorkItemAsync(1, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = 1,
                Fields = new Dictionary<string, object>()
                {
                    { "System.WorkItemType", "User Story" },
                    { "System.TeamProject", projectName },
                },
                Relations = new List<WorkItemRelation>()
                {
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Forward",
                        Url = $"{workItemsBaseUrl}/{workItemId}"
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

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default)
            {
                DryRun = true
            };
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal("Parent is 1", result);
        }

        [Fact]
        public async void New_Succeeds()
        {
            int workItemId = 1;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
                {
                    { "System.WorkItemType", "User Story" },
                    { "System.Title", "Hello" },
                }
            });
            string ruleCode = @"
var wi = store.NewWorkItem(""Task"");
wi.Title = ""Brand new"";
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default)
            {
                DryRun = true
            };
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Null(result);
            logger.Received().WriteInfo($"Found a request for a new Task workitem in {projectName}");
            logger.Received().WriteWarning("Dry-run mode: no updates sent to Azure DevOps.");
            logger.Received().WriteInfo("Changes saved to Azure DevOps (mode Default): 1 created, 0 updated.");
        }

        [Fact]
        public async void AddChild_Succeeds()
        {
            int workItemId = 1;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
                {
                    { "System.WorkItemType", "User Story" },
                    { "System.Title", "Hello" },
                }
            });
            string ruleCode = @"
var parent = self;
var newChild = store.NewWorkItem(""Task"");
newChild.Title = ""Brand new"";
parent.Relations.AddChild(newChild);
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default)
            {
                DryRun = true
            };
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Null(result);
            logger.Received().WriteInfo($"Found a request for a new Task workitem in {projectName}");
            logger.Received().WriteInfo($"Found a request to update workitem {workItemId} in {projectName}");
            logger.Received().WriteWarning("Dry-run mode: no updates sent to Azure DevOps.");
            logger.Received().WriteInfo("Changes saved to Azure DevOps (mode Default): 1 created, 1 updated.");
        }

        [Fact]
        public async void TouchDescription_Succeedes()
        {
            int workItemId = 42;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem()
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
                {
                    { "System.Description", "Hello" },
                }
            });
            string ruleCode = @"
self.Description = self.Description + ""."";
return self.Description;
";

            var engine = new RuleEngine(logger, ruleCode.Mince(), SaveMode.Default)
            {
                DryRun = true
            };
            string result = await engine.ExecuteAsync(collectionUrl, projectId, projectName, personalAccessToken, workItemId, client);

            Assert.Equal("Hello.", result);
            logger.Received().WriteInfo($"Found a request to update workitem {workItemId} in {projectName}");
            logger.Received().WriteWarning("Dry-run mode: no updates sent to Azure DevOps.");
            logger.Received().WriteInfo("Changes saved to Azure DevOps (mode Default): 0 created, 1 updated.");
        }

    }
}

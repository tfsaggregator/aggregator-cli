using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using aggregator;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NSubstitute;
using Xunit;

namespace unittests_ruleng
{
    public class WorkItemStoreTests
    {
        private const string CollectionUrl = "https://dev.azure.com/fake-organization";
        private readonly Guid projectId = Guid.NewGuid();
        private const string ProjectName = "test-project";
        private readonly string workItemsBaseUrl = $"{CollectionUrl}/{ProjectName}/_apis/wit/workItems";

        [Fact]
        public void GetWorkItem_ById_Succeeds()
        {
            var logger = Substitute.For<IAggregatorLogger>();
            var client = Substitute.For<WorkItemTrackingHttpClient>(new Uri(CollectionUrl), null);
            int workItemId = 42;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
            });

            var context = new EngineContext(client, projectId, ProjectName, logger);
            var sut = new WorkItemStore(context);

            var wi = sut.GetWorkItem(workItemId);

            Assert.NotNull(wi);
            Assert.Equal(workItemId, wi.Id.Value);
        }

        [Fact]
        public void GetWorkItems_ByIds_Succeeds()
        {
            var logger = Substitute.For<IAggregatorLogger>();
            var client = Substitute.For<WorkItemTrackingHttpClient>(new Uri(CollectionUrl), null);
            var ids = new [] { 42, 99 };
            client.GetWorkItemsAsync(ids, expand: WorkItemExpand.All)
                .ReturnsForAnyArgs(new List<WorkItem>
                {
                    new WorkItem
                    {
                        Id = ids[0],
                        Fields = new Dictionary<string, object>()
                    },
                    new WorkItem
                    {
                        Id = ids[1],
                        Fields = new Dictionary<string, object>()
                    }
                });

            var context = new EngineContext(client, projectId, ProjectName, logger);
            var sut = new WorkItemStore(context);

            var wis = sut.GetWorkItems(ids);

            Assert.NotEmpty(wis);
            Assert.Equal(2, wis.Count);
            Assert.Contains(wis, (x) => x.Id.Value == 42);
            Assert.Contains(wis, (x) => x.Id.Value == 99);
        }

        [Fact]
        public async Task NewWorkItem_Succeeds()
        {
            var logger = Substitute.For<IAggregatorLogger>();
            var client = Substitute.For<WorkItemTrackingHttpClient>(new Uri(CollectionUrl), null);
            client.ExecuteBatchRequest(default).ReturnsForAnyArgs(info => new List<WitBatchResponse>());
            var context = new EngineContext(client, projectId, ProjectName, logger);
            var sut = new WorkItemStore(context);

            var wi = sut.NewWorkItem("Task");
            wi.Title = "Brand new";
            var save = await sut.SaveChanges(SaveMode.Default, false, CancellationToken.None);

            Assert.NotNull(wi);
            Assert.True(wi.IsNew);
            Assert.Equal(1, save.created);
            Assert.Equal(0, save.updated);
            Assert.Equal(-1, wi.Id.Value);
        }

        [Fact]
        public void AddChild_Succeeds()
        {
            var logger = Substitute.For<IAggregatorLogger>();
            var client = Substitute.For<WorkItemTrackingHttpClient>(new Uri(CollectionUrl), null);
            var context = new EngineContext(client, projectId, ProjectName, logger);
            int workItemId = 1;
            client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>(),
                Relations = new List<WorkItemRelation>
                {
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Forward",
                        Url = $"{workItemsBaseUrl}/42"
                    },
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Forward",
                        Url = $"{workItemsBaseUrl}/99"
                    }
                },
            });

            var sut = new WorkItemStore(context);

            var parent = sut.GetWorkItem(1);
            Assert.Equal(2, parent.Relations.Count);

            var newChild = sut.NewWorkItem("Task");
            newChild.Title = "Brand new";
            parent.Relations.AddChild(newChild);

            Assert.NotNull(newChild);
            Assert.True(newChild.IsNew);
            Assert.Equal(-1, newChild.Id.Value);
            Assert.Equal(3, parent.Relations.Count);
        }
    }
}

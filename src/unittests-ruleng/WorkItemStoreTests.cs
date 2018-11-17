using System;
using System.Linq;
using aggregator;
using aggregator.Engine;
using aggregator.unittests;
using Xunit;

namespace unittests_ruleng
{
    public class WorkItemStoreTests
    {
        const string collectionUrl = "https://dev.azure.com/fake-organization";
        Guid projectId = Guid.NewGuid();
        const string projectName = "test-project";
        const string personalAccessToken = "***personalAccessToken***";
        FakeWorkItemTrackingHttpClient client = new FakeWorkItemTrackingHttpClient(new Uri($"{collectionUrl}"), null);
        MockAggregatorLogger logger = new MockAggregatorLogger();

        [Fact]
        public void GetWorkItem_ById_Succeeds()
        {
            var context = new EngineContext(client, projectId, projectName, personalAccessToken, logger);
            var sut = new WorkItemStore(context);

            var wi = sut.GetWorkItem(42);

            Assert.NotNull(wi);
            Assert.Equal(42, wi.Id.Value);
        }

        [Fact]
        public void GetWorkItems_ByIds_Succeeds()
        {
            var context = new EngineContext(client, projectId, projectName, personalAccessToken, logger);
            var sut = new WorkItemStore(context);

            var wis = sut.GetWorkItems(new int[] { 42, 99 });

            Assert.NotEmpty(wis);
            Assert.Equal(2, wis.Count);
            Assert.Contains(wis, (x) => x.Id.Value == 42);
            Assert.Contains(wis, (x) => x.Id.Value == 99);
        }

        [Fact]
        public void NewWorkItem_Succeeds()
        {
            var context = new EngineContext(client, projectId, projectName, personalAccessToken, logger);
            var sut = new WorkItemStore(context);

            var wi = sut.NewWorkItem("Task");
            wi.Title = "Brand new";
            var save = sut.SaveChanges(SaveMode.Item, true).Result;

            Assert.NotNull(wi);
            Assert.True(wi.IsNew);
            Assert.Equal(1, save.created);
            Assert.Equal(0, save.updated);
            Assert.Equal(-1, wi.Id.Value);
        }

        [Fact]
        public void AddChild_Succeeds()
        {
            var context = new EngineContext(client, projectId, projectName, personalAccessToken, logger);
            var sut = new WorkItemStore(context);

            var parent = sut.GetWorkItem(1);
            Assert.Equal(2, parent.Relations.Count());

            var newChild = sut.NewWorkItem("Task");
            newChild.Title = "Brand new";
            parent.Relations.AddChild(newChild);

            Assert.NotNull(newChild);
            Assert.True(newChild.IsNew);
            Assert.Equal(-1, newChild.Id.Value);
            Assert.Equal(3, parent.Relations.Count());
        }
    }
}

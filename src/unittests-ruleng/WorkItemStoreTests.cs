using System;
using aggregator.Engine;
using aggregator.unittests;
using Xunit;

namespace unittests_ruleng
{
    public class WorkItemStoreTests
    {
        [Fact]
        public void GetWorkItem_ById_Succeeds()
        {
            var baseUrl = new Uri("https://dev.azure.com/fake-account");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            var context = new EngineContext(client, logger);
            var sut = new WorkItemStore(context);

            var wi = sut.GetWorkItem(42);

            Assert.NotNull(wi);
            Assert.Equal(42, wi.Id.Value);
        }

        [Fact]
        public void GetWorkItems_ByIds_Succeeds()
        {
            var baseUrl = new Uri("https://dev.azure.com/fake-account");
            var client = new FakeWorkItemTrackingHttpClient(baseUrl, null);
            var logger = new MockAggregatorLogger();
            var context = new EngineContext(client, logger);
            var sut = new WorkItemStore(context);

            var wis = sut.GetWorkItems(new int[] { 42, 99 });

            Assert.NotEmpty(wis);
            Assert.Equal(2, wis.Count);
            Assert.Contains(wis, (x) => x.Id.Value == 42);
            Assert.Contains(wis, (x) => x.Id.Value == 99);
        }
    }
}

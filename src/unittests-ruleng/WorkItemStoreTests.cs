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
            const string baseUrl = "https://dev.azure.com/fake-account/fake-project";
            var client = new FakeWorkItemTrackingHttpClient(new Uri(baseUrl), null);
            var logger = new MockAggregatorLogger();
            var context = new EngineContext(client, logger);
            var sut = new WorkItemStore(context);

            var wi = sut.GetWorkItem(42);

            Assert.NotNull(wi);
            Assert.Equal(42, wi.Id.Value);
        }
    }
}

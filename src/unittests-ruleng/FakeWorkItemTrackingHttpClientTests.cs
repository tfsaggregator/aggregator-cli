using System;
using aggregator.unittests;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Xunit;

namespace unittests_ruleng
{
    public class FakeWorkItemTrackingHttpClientTests
    {
        const string baseUrl = "https://dev.azure.com/fake-account/fake-project";

        [Fact]
        public void GetWorkItem_ById_Succeeds()
        {
            var sut = new FakeWorkItemTrackingHttpClient(new Uri(baseUrl), null);

            var wi = sut.GetWorkItemAsync(42, expand: WorkItemExpand.All).Result;

            Assert.NotNull(wi);
            Assert.Equal(42, wi.Id);
        }
    }
}

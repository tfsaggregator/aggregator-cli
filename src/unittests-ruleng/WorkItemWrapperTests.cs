using System;
using System.Collections.Generic;
using aggregator;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using NSubstitute;
using Xunit;

namespace unittests_ruleng
{
    public class WorkItemWrapperTests
    {

        private const string CollectionUrl = "https://dev.azure.com/fake-organization";
        private readonly Guid projectId = Guid.NewGuid();
        private const string ProjectName = "test-project";
        private readonly IAggregatorLogger logger = Substitute.For<IAggregatorLogger>();
        private readonly WorkItemTrackingHttpClient client;
        private readonly string workItemsBaseUrl = $"{CollectionUrl}/_apis/wit";
        private EngineContext context;


        public WorkItemWrapperTests()
        {
            client = Substitute.For<WorkItemTrackingHttpClient>(new Uri(CollectionUrl), null);
            context = new EngineContext(client, projectId, ProjectName, logger);
        }

        [Fact]
        public void IdentifyDeletedWorkItem_Success()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                },
                Url = $"{workItemsBaseUrl}/recyclebin/{workItemId}"
            };

            var wrapper = new WorkItemWrapper(context, workItem);

            Assert.True(wrapper.IsDeleted);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);
        }

        [Fact]
        public void IdentifyNonDeletedWorkItem_Success()
        {
            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                {
                    { "System.WorkItemType", "Bug" },
                    { "System.Title", "Hello" },
                },
                Url = $"{workItemsBaseUrl}/workItems/{workItemId}"
            };

            var wrapper = new WorkItemWrapper(context, workItem);

            Assert.False(wrapper.IsDeleted);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);
        }
    }
}
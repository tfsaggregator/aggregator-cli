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
        private readonly WorkItemTrackingHttpClient witClient;
        private readonly TestClientsContext clientsContext;
        private readonly EngineContext context;

        public WorkItemWrapperTests()
        {
            var logger = Substitute.For<IAggregatorLogger>();

            clientsContext = new TestClientsContext();

            witClient = clientsContext.WitClient;
            witClient.ExecuteBatchRequest(default).ReturnsForAnyArgs(info => new List<WitBatchResponse>());

            context = new EngineContext(clientsContext, clientsContext.ProjectId, clientsContext.ProjectName, logger);
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
                Url = $"{clientsContext.RecycleBinBaseUrl}/{workItemId}"
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
                Url = $"{clientsContext.WorkItemsBaseUrl}/{workItemId}"
            };

            var wrapper = new WorkItemWrapper(context, workItem);

            Assert.False(wrapper.IsDeleted);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);
        }
    }
}
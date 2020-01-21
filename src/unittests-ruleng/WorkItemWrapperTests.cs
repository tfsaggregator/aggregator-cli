using System;
using System.Collections.Generic;
using aggregator;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;

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

        [Fact]
        public void AssignFieldWithAlreadyNullValueShouldNotThrowException()
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

            wrapper.AssignedTo = null;

            var idRef = new IdentityRef();
            wrapper.AssignedTo = idRef;

            Assert.Equal(idRef, wrapper.AssignedTo);
        }


        public static IEnumerable<object[]> AssignSameValueShouldNotInvalidateIsDirty_AdditionalTestData
        {
            get
            {
                yield return new object[] { DateTime.Now };
            }
        }

        [Theory]
        [InlineData("Hello")]  // string
        [InlineData(13)]       // int
        [InlineData(13.6)]     // double
        [InlineData(true)]     // bool
        [MemberData(nameof(AssignSameValueShouldNotInvalidateIsDirty_AdditionalTestData))] // DateTime
        public void AssignSameValueShouldNotInvalidateIsDirty_Success(object testValue)
        {
            var testKey = "testKey";
            WorkItem workItem = new WorkItem
                                {
                                    Id = 11,
                                    Fields = new Dictionary<string, object>
                                             {
                                                 { "System.WorkItemType", "Bug" },
                                                 { "System.Title", "Hello" },
                                                 { testKey, testValue },
                                             },
                                    Url = $"{clientsContext.WorkItemsBaseUrl}/11"
                                };

            var wrapper = new WorkItemWrapper(context, workItem);


            Assert.False(wrapper.IsDirty);

            wrapper[testKey] = testValue;
            Assert.False(wrapper.IsDirty);
        }
    }
}
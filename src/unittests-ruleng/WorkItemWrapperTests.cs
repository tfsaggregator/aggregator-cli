﻿using System;
using System.Collections.Generic;
using aggregator;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
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

            context = new EngineContext(clientsContext, clientsContext.ProjectId, clientsContext.ProjectName, logger, new RuleSettings());
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


        [Fact]
        public void ChangingAFieldWithEnableRevisionCheckOnAddsTestOperation()
        {
            var logger = Substitute.For<IAggregatorLogger>();
            var ruleSettings = new RuleSettings { EnableRevisionCheck = true };
            var context = new EngineContext(clientsContext, clientsContext.ProjectId, clientsContext.ProjectName, logger, ruleSettings);

            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                                             {
                                                 { "System.WorkItemType", "Bug" },
                                                 { "System.Title", "Hello" },
                                             },
                Rev = 3,
                Url = $"{clientsContext.WorkItemsBaseUrl}/{workItemId}"
            };

            var wrapper = new WorkItemWrapper(context, workItem);
            wrapper.Title = "Replaced title";

            var expected = new JsonPatchOperation
            {
                Operation = Operation.Test,
                Path = "/rev",
                Value = 3
            };
            Assert.Equal(2, wrapper.Changes.Count);
            var actual = wrapper.Changes[0];
            Assert.True(expected.Operation == actual.Operation && expected.Path == actual.Path && expected.Value.ToString() == actual.Value.ToString() && expected.From == actual.From );
        }

        [Fact]
        public void ChangingAFieldWithEnableRevisionCheckOffHasNoTestOperation()
        {
            var logger = Substitute.For<IAggregatorLogger>();
            var ruleSettings = new RuleSettings { EnableRevisionCheck = false };
            var context = new EngineContext(clientsContext, clientsContext.ProjectId, clientsContext.ProjectName, logger, ruleSettings);

            int workItemId = 42;
            WorkItem workItem = new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>
                                             {
                                                 { "System.WorkItemType", "Bug" },
                                                 { "System.Title", "Hello" },
                                             },
                Rev = 3,
                Url = $"{clientsContext.WorkItemsBaseUrl}/{workItemId}"
            };

            var wrapper = new WorkItemWrapper(context, workItem);
            wrapper.Title = "Replaced title";

            Assert.Single(wrapper.Changes);
            var actual = wrapper.Changes[0];
            Assert.Equal(Operation.Replace, actual.Operation);
            Assert.Equal("/fields/System.Title", actual.Path);
            Assert.Equal("Replaced title", actual.Value);
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using aggregator;
using aggregator.Engine;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using NSubstitute;

using unittests_ruleng.TestData;

using Xunit;

namespace unittests_ruleng
{
    public class WorkItemStoreTests
    {
        private readonly IAggregatorLogger logger;
        private readonly WorkItemTrackingHttpClient witClient;
        private readonly TestClientsContext clientsContext;
        private readonly EngineContext engineDefaultContext;

        public WorkItemStoreTests()
        {
            logger = Substitute.For<IAggregatorLogger>();

            clientsContext = new TestClientsContext();

            witClient = clientsContext.WitClient;
            witClient.ExecuteBatchRequest(default).ReturnsForAnyArgs(info => new List<WitBatchResponse>());

            engineDefaultContext = new EngineContext(clientsContext, clientsContext.ProjectId, clientsContext.ProjectName, logger, new RuleSettings(), false, default);
        }


        [Fact]
        public void GetWorkItem_ById_Succeeds()
        {
            int workItemId = 42;
            witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>()
            });

            var context = engineDefaultContext;
            var sut = new WorkItemStore(context);

            var wi = sut.GetWorkItem(workItemId);

            Assert.NotNull(wi);
            Assert.Equal(workItemId, wi.Id.Value);
        }

        [Fact]
        public void GetWorkItems_ByIds_Succeeds()
        {
            var ids = new[] { 42, 99 };
            witClient.GetWorkItemsAsync(ids, expand: WorkItemExpand.All)
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

            var context = engineDefaultContext;
            var sut = new WorkItemStore(context);

            var wis = sut.GetWorkItems(ids);

            Assert.NotEmpty(wis);
            Assert.Equal(2, wis.Count);
            Assert.Contains(wis, (x) => x.Id.Value == 42);
            Assert.Contains(wis, (x) => x.Id.Value == 99);
        }

        static List<WorkItem> GenerateWorkItems(int startId, int count = 200)
        {
            return Enumerable
                .Range(startId, count)
                .Select(i => new WorkItem
                {
                    Id = i,
                    Fields = new Dictionary<string, object>()
                }).ToList();
        }

        [Fact]
        public void GetWorkItems_ByIds_LessThan200_Succeeds()
        {
            witClient.GetWorkItemsAsync(Arg.Any<IEnumerable<int>>(), expand: WorkItemExpand.All)
                .Returns(
                    GenerateWorkItems(1, 199)
                    );
            var ids = Enumerable.Range(1, 199).ToArray();

            var context = engineDefaultContext;
            var sut = new WorkItemStore(context);

            var wis = sut.GetWorkItems(ids);

            Assert.NotEmpty(wis);
            Assert.Equal(ids.Length, wis.Count);
            Assert.Contains(wis, (x) => x.Id.Value == 42);
            Assert.Contains(wis, (x) => x.Id.Value == 199);
        }

        [Fact]
        public void GetWorkItems_ByIds_MoreThan200_Succeeds()
        {
            witClient.GetWorkItemsAsync(Arg.Any<IEnumerable<int>>(), expand: WorkItemExpand.All)
                .Returns(
                    GenerateWorkItems(1, 200),
                    GenerateWorkItems(201, 150)
                    );
            var ids = Enumerable.Range(1, 350).ToArray();

            var context = engineDefaultContext;
            var sut = new WorkItemStore(context);

            var wis = sut.GetWorkItems(ids);

            Assert.NotEmpty(wis);
            Assert.Equal(ids.Length, wis.Count);
            Assert.Contains(wis, (x) => x.Id.Value == 42);
            Assert.Contains(wis, (x) => x.Id.Value == 299);
        }

        [Fact]
        public void GetWorkItems_ByIds_MoreThan400_Succeeds()
        {
            witClient.GetWorkItemsAsync(Arg.Any<IEnumerable<int>>(), expand: WorkItemExpand.All)
                .Returns(
                    GenerateWorkItems(1, 200),
                    GenerateWorkItems(201, 200),
                    GenerateWorkItems(401, 33)
                    );
            var ids = Enumerable.Range(1, 433).ToArray();

            var context = engineDefaultContext;
            var sut = new WorkItemStore(context);

            var wis = sut.GetWorkItems(ids);

            Assert.NotEmpty(wis);
            Assert.Equal(ids.Length, wis.Count);
            Assert.Contains(wis, (x) => x.Id.Value == 42);
            Assert.Contains(wis, (x) => x.Id.Value == 299);
            Assert.Contains(wis, (x) => x.Id.Value == 410);
        }

        [Fact]
        public async Task NewWorkItem_Succeeds()
        {
            witClient.ExecuteBatchRequest(default).ReturnsForAnyArgs(info => new List<WitBatchResponse>());
            var context = engineDefaultContext;
            var sut = new WorkItemStore(context);

            var wi = sut.NewWorkItem("Task");
            wi.Title = "Brand new";
            var (created, updated) = await sut.SaveChanges(SaveMode.Default, false, false, false, CancellationToken.None);

            Assert.NotNull(wi);
            Assert.True(wi.IsNew);
            Assert.Equal(1, created);
            Assert.Equal(0, updated);
            Assert.Equal(-1, wi.Id.Value);
        }

        [Fact]
        public void AddChild_Succeeds()
        {
            var context = engineDefaultContext;
            int workItemId = 1;
            witClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Returns(new WorkItem
            {
                Id = workItemId,
                Fields = new Dictionary<string, object>(),
                Relations = new List<WorkItemRelation>
                {
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Forward",
                        Url = $"{clientsContext.WorkItemsBaseUrl}/42"
                    },
                    new WorkItemRelation
                    {
                        Rel = "System.LinkTypes.Hierarchy-Forward",
                        Url = $"{clientsContext.WorkItemsBaseUrl}/99"
                    }
                },
            });

            var sut = new WorkItemStore(context);

            var parent = sut.GetWorkItem(workItemId);
            Assert.Equal(2, parent.Relations.Count);

            var newChild = sut.NewWorkItem("Task");
            newChild.Title = "Brand new";
            parent.Relations.AddChild(newChild);

            Assert.NotNull(newChild);
            Assert.True(newChild.IsNew);
            Assert.Equal(-1, newChild.Id.Value);
            Assert.Equal(3, parent.Relations.Count);
        }

        [Fact]
        public void DeleteWorkItem_Succeeds()
        {
            var context = engineDefaultContext;
            var workItem = ExampleTestData.WorkItem;
            int workItemId = workItem.Id.Value;

            var sut = new WorkItemStore(context, workItem);

            var wrapper = sut.GetWorkItem(workItemId);
            Assert.False(wrapper.IsDeleted);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);

            sut.DeleteWorkItem(wrapper);
            Assert.True(wrapper.IsDirty);
            Assert.Equal(RecycleStatus.ToDelete, wrapper.RecycleStatus);

            var (Created, Updated, Deleted, Restored) = context.Tracker.GetChangedWorkItems();
            Assert.Single(Deleted);
            Assert.Empty(Created);
            Assert.Empty(Updated);
            Assert.Empty(Restored);
        }

        [Fact]
        public void DeleteAlreadyDeletedWorkItem_NoChange()
        {
            var context = engineDefaultContext;
            var workItem = ExampleTestData.DeltedWorkItem;
            int workItemId = workItem.Id.Value;

            var sut = new WorkItemStore(context, workItem);

            var wrapper = sut.GetWorkItem(workItemId);
            Assert.True(wrapper.IsDeleted);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);

            sut.DeleteWorkItem(wrapper);
            Assert.False(wrapper.IsDirty);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);

            var (Created, Updated, Deleted, Restored) = context.Tracker.GetChangedWorkItems();
            Assert.Empty(Deleted);
            Assert.Empty(Created);
            Assert.Empty(Updated);
            Assert.Empty(Restored);
        }

        [Fact]
        public void RestoreNotDeletedWorkItem_NoChange()
        {
            var context = engineDefaultContext;
            var workItem = ExampleTestData.WorkItem;
            int workItemId = workItem.Id.Value;

            var sut = new WorkItemStore(context, workItem);

            var wrapper = sut.GetWorkItem(workItemId);
            Assert.False(wrapper.IsDeleted);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);

            sut.RestoreWorkItem(wrapper);
            Assert.False(wrapper.IsDirty);
            Assert.Equal(RecycleStatus.NoChange, wrapper.RecycleStatus);

            var (Created, Updated, Deleted, Restored) = context.Tracker.GetChangedWorkItems();
            Assert.Empty(Deleted);
            Assert.Empty(Created);
            Assert.Empty(Updated);
            Assert.Empty(Restored);
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1117:Local variables should not shadow class fields", Justification = "<Pending>")]
        public async Task UpdateWorkItem_WithRevisionCheck_Enabled_Succeeds()
        {
            var witClient = clientsContext.WitClient;
            witClient
                .When(x => x.CreateWorkItemBatchRequest(
                    Arg.Any<int>(), Arg.Any<JsonPatchDocument>(), Arg.Any<bool>(), Arg.Any<bool>()))
                .CallBase();
            witClient
                .ExecuteBatchRequest(default)
                .ReturnsForAnyArgs(
                    info =>
                    {
                        var requests = info.Arg<IEnumerable<WitBatchRequest>>();
                        return requests.Aggregate(
                            new List<WitBatchResponse>(),
                            (acc, req) =>
                            {
                                acc.Add(new WitBatchResponse { Body = req.Body });
                                return acc;
                            }
                        );
                    });
            var ruleSettings = new RuleSettings { EnableRevisionCheck = true };
            var context = new EngineContext(clientsContext, clientsContext.ProjectId, clientsContext.ProjectName, logger, ruleSettings, false, default);
            var workItem = ExampleTestData.WorkItem;
            int workItemId = workItem.Id.Value;

            var sut = new WorkItemStore(context, workItem);

            var wrapper = sut.GetWorkItem(workItemId);
            wrapper.Title = "Replaced title";

            var (created, updated) = await sut.SaveChanges(SaveMode.Default, commit: true, impersonate: false, bypassrules: false, default);

            Assert.Equal(0, created);
            Assert.Equal(1, updated);
            logger.Received().WriteVerbose(@"[{""Body"":""[{\""op\"":5,\""path\"":\""/rev\"",\""from\"":null,\""value\"":2},{\""op\"":2,\""path\"":\""/fields/System.Title\"",\""from\"":null,\""value\"":\""Replaced title\""}]""}]");
        }

        [Fact]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1117:Local variables should not shadow class fields", Justification = "<Pending>")]
        public async Task UpdateWorkItem_WithRevisionCheck_Disabled_Succeeds()
        {
            var witClient = clientsContext.WitClient;
            witClient
                .When(x => x.CreateWorkItemBatchRequest(
                    Arg.Any<int>(), Arg.Any<JsonPatchDocument>(), Arg.Any<bool>(), Arg.Any<bool>()))
                .CallBase();
            witClient
                .ExecuteBatchRequest(default)
                .ReturnsForAnyArgs(
                    info =>
                    {
                        var requests = info.Arg<IEnumerable<WitBatchRequest>>();
                        return requests.Aggregate(
                            new List<WitBatchResponse>(),
                            (acc, req) =>
                            {
                                acc.Add(new WitBatchResponse { Body = req.Body });
                                return acc;
                            }
                        );
                    });
            var ruleSettings = new RuleSettings { EnableRevisionCheck = false };
            var context = new EngineContext(clientsContext, clientsContext.ProjectId, clientsContext.ProjectName, logger, ruleSettings, false, default);
            var workItem = ExampleTestData.WorkItem;
            int workItemId = workItem.Id.Value;

            var sut = new WorkItemStore(context, workItem);

            var wrapper = sut.GetWorkItem(workItemId);
            wrapper.Title = "Replaced title";

            var (created, updated) = await sut.SaveChanges(SaveMode.Default, commit: true, impersonate: false, bypassrules: false, default);

            Assert.Equal(0, created);
            Assert.Equal(1, updated);
            await witClient.Received().ExecuteBatchRequest(Arg.Is<IEnumerable<WitBatchRequest>>(l => l.Count() == 1));
            logger.Received().WriteVerbose(@"[{""Body"":""[{\""op\"":2,\""path\"":\""/fields/System.Title\"",\""from\"":null,\""value\"":\""Replaced title\""}]""}]");
        }
    }
}

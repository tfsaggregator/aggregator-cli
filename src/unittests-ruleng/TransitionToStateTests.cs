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
using Xunit;

namespace unittests_ruleng;

public class TransitionToStateTests
{
    private readonly IAggregatorLogger Logger;
    private readonly WorkItemTrackingHttpClient WitClient;
    private readonly TestClientsContext DefaultClientsContext;
    private readonly EngineContext DefaultEngineContext;
    private readonly WorkItemType TaskWorkItemType;

    public TransitionToStateTests()
    {
        Logger = Substitute.For<IAggregatorLogger>();

        DefaultClientsContext = new TestClientsContext();

        WitClient = DefaultClientsContext.WitClient;
        WitClient.ExecuteBatchRequest(default).ReturnsForAnyArgs(info => new List<WitBatchResponse>());

        DefaultEngineContext = new EngineContext(DefaultClientsContext, DefaultClientsContext.ProjectId, DefaultClientsContext.ProjectName, Logger, new RuleSettings(), false, default);

        TaskWorkItemType = new WorkItemType
        {
            Name = "Task",
            ReferenceName = "Microsoft.VSTS.WorkItemTypes.Task",
            IsDisabled = false,
            Color = "A4880A",
            States = new WorkItemStateColor[]
            {
                new WorkItemStateColor { Name = "Proposed",  Category = "Proposed" },
                new WorkItemStateColor { Name = "Active",  Category = "InProgress" },
                new WorkItemStateColor { Name = "Resolved",  Category = "InProgress" },
                new WorkItemStateColor { Name = "Closed",  Category = "Completed" },
            },
            Transitions = new Dictionary<string, WorkItemStateTransition[]>
            {
                { "Active",new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Active" },
                    new WorkItemStateTransition { To = "Closed" },
                    new WorkItemStateTransition { To = "Resolved" },
                    new WorkItemStateTransition { To = "Proposed" },
                }},
                { "Closed",new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Closed" },
                    new WorkItemStateTransition { To = "Active" },
                    new WorkItemStateTransition { To = "Proposed" },
                }},
                { "Proposed",new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Proposed" },
                    new WorkItemStateTransition { To = "Closed" },
                    new WorkItemStateTransition { To = "Active" },
                }},
                { "Resolved",new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Resolved" },
                    new WorkItemStateTransition { To = "Closed" },
                    new WorkItemStateTransition { To = "Active" },
                }},
                { "", new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Proposed" },
                }},
            }
        };
        WitClient.GetWorkItemTypeAsync(DefaultClientsContext.ProjectName, TaskWorkItemType.Name).Returns(TaskWorkItemType);
    }

    [Fact]
    public async Task WorkItemStateWorkflow_CanLoad_Task()
    {
        string workItemType = "Task";
        var stateInfo = new WorkItemStateWorkflow(workItemType);

        bool ok = await stateInfo.LoadAsync(DefaultEngineContext);

        Assert.True(ok);
    }

    [Fact]
    public async Task WorkItemStateWorkflow_Task_HasExpectedStates()
    {
        var stateInfo = new WorkItemStateWorkflow(TaskWorkItemType.Name);
        
        await stateInfo.LoadAsync(DefaultEngineContext);

        Assert.True(stateInfo.HasState("Proposed"));
        Assert.True(stateInfo.HasState("Active"));
        Assert.True(stateInfo.HasState("Resolved"));
        Assert.True(stateInfo.HasState("Closed"));
    }

    [Fact]
    public async Task WorkItemStateWorkflow_Task_CanTransitionFromActiveToResolved()
    {
        var stateInfo = new WorkItemStateWorkflow(TaskWorkItemType.Name);

        await stateInfo.LoadAsync(DefaultEngineContext);
        var path = stateInfo.GetTransitionPath("Active", "Resolved")?.ToArray();

        Assert.NotNull(path);
        Assert.Single(path);
        Assert.Contains("Resolved", path);
    }

    [Fact]
    public async Task WorkItemStateWorkflow_Task_CanTransitionFromProposedToResolved()
    {
        var stateInfo = new WorkItemStateWorkflow(TaskWorkItemType.Name);

        await stateInfo.LoadAsync(DefaultEngineContext);
        var path = stateInfo.GetTransitionPath("Proposed", "Resolved")?.ToArray();

        Assert.NotNull(path);
        Assert.Equal(2, path.Length);
        Assert.Contains("Active", path);
        Assert.Contains("Resolved", path);
    }

    // HAPPY PATH #1
    [Fact]
    public async Task TransitionToState_Task_TransitionFromActiveToResolved_NoSave()
    {
        var workItem = new WorkItem
        {
            Id = 42,
            Fields = new Dictionary<string, object>()
            {
                { CoreFieldRefNames.State, "Active" },
                { CoreFieldRefNames.WorkItemType, TaskWorkItemType.Name },
            }
        };
        var workItemWrap = new WorkItemWrapper(DefaultEngineContext, workItem);
        var sut = new WorkItemStore(DefaultEngineContext);

        bool ok = await sut.TransitionToState(workItemWrap, "Resolved", false, "Some comment");

        Assert.True(ok);
        await WitClient.DidNotReceiveWithAnyArgs()
            .UpdateWorkItemAsync(Arg.Any<JsonPatchDocument>(), Arg.Any<int>());
        Logger.Received()
            .WriteInfo("WorkItem #42 state will change from 'Active' to 'Resolved' when Rule exits");
        // TODO Assert.Empty(workItemWrap.Changes);
    }

    // HAPPY PATH #2
    [Fact]
    public async Task TransitionToState_Task_TransitionFromProposedToResolved_SaveTwice()
    {
        var workItem = new WorkItem
        {
            Id = 42,
            Fields = new Dictionary<string, object>()
            {
                { CoreFieldRefNames.State, "Proposed" },
                { CoreFieldRefNames.WorkItemType, TaskWorkItemType.Name },
            }
        };
        var workItemWrap = new WorkItemWrapper(DefaultEngineContext, workItem);
        var sut = new WorkItemStore(DefaultEngineContext);

        bool ok = await sut.TransitionToState(workItemWrap, "Resolved", false, "Some comment");

        Assert.True(ok);
        await WitClient.Received()
            .UpdateWorkItemAsync(Arg.Any<JsonPatchDocument>(), 42, null, false);
        Logger.Received()
            .WriteVerbose("Transitioning from 'Proposed' to 'Active'");
        Logger.Received()
            .WriteInfo("Transitioning WorkItem #42 from 'Proposed' to 'Active' succeeded");
        Logger.Received()
            .WriteVerbose("Transitioning from 'Active' to 'Resolved'");
        Logger.Received()
            .WriteInfo("Transitioning WorkItem #42 from 'Active' to 'Resolved' succeeded");
    }

    // TODO argument checking
    [Fact]
    public async Task TransitionToState_Task_InvalidCurrentState_Fails()
    {
        var workItem = new WorkItem
        {
            Id = 42,
            Fields = new Dictionary<string, object>()
            {
                { CoreFieldRefNames.State, "DoesntExist" },
                { CoreFieldRefNames.WorkItemType, TaskWorkItemType.Name },
            }
        };
        var workItemWrap = new WorkItemWrapper(DefaultEngineContext, workItem);
        var sut = new WorkItemStore(DefaultEngineContext);

        bool ok = await sut.TransitionToState(workItemWrap, "Resolved", false, "Some comment");

        Assert.False(ok);
        Logger.Received()
            .WriteError($"Current state 'DoesntExist' is not valid for work item type '{TaskWorkItemType.Name}'");
    }

    [Fact]
    public async Task TransitionToState_Task_InvalidTargetState_Fails()
    {
        var workItem = new WorkItem
        {
            Id = 42,
            Fields = new Dictionary<string, object>()
            {
                { CoreFieldRefNames.State, "Closed" },
                { CoreFieldRefNames.WorkItemType, TaskWorkItemType.Name },
            }
        };
        var workItemWrap = new WorkItemWrapper(DefaultEngineContext, workItem);
        var sut = new WorkItemStore(DefaultEngineContext);

        bool ok = await sut.TransitionToState(workItemWrap, "DoesntExist", false, "Some comment");

        Assert.False(ok);
        Logger.Received()
            .WriteError($"Target state 'DoesntExist' is not valid for work item type '{TaskWorkItemType.Name}'");
    }

    [Fact]
    public async Task TransitionToState_NewWorkItem_Fails()
    {
        var sut = new WorkItemStore(DefaultEngineContext);
        var wi = sut.NewWorkItem("Task");
        wi.Title = "Brand new";

        bool ok = await sut.TransitionToState(wi, "Resolved", false, "Some comment");

        Assert.False(ok);
        Logger.Received()
            .WriteError("WorkItem is new: TransitionToState works only for existing WorkItems");
    }

    [Fact]
    public async Task TransitionToState_DeletedWorkItem_Fails()
    {
        var workItem = new WorkItem
        {
            Id = 42,
            Url = "/recyclebin/42",
            Fields = new Dictionary<string, object>()
            {
                { CoreFieldRefNames.State, "Closed" },
                { CoreFieldRefNames.WorkItemType, TaskWorkItemType.Name },
            }
        };
        var workItemWrap = new WorkItemWrapper(DefaultEngineContext, workItem);
        var sut = new WorkItemStore(DefaultEngineContext);

        bool ok = await sut.TransitionToState(workItemWrap, "Resolved", false, "Some comment");

        Assert.False(ok);
        Logger.Received()
            .WriteError("WorkItem #42 is deleted: TransitionToState works only for existing WorkItems");
    }

    [Fact]
    public async Task TransitionToState_StateChangedWorkItem_Fails()
    {
        var workItem = new WorkItem
        {
            Id = 42,
            Fields = new Dictionary<string, object>()
            {
                { CoreFieldRefNames.State, "Closed" },
                { CoreFieldRefNames.WorkItemType, TaskWorkItemType.Name },
            }
        };
        var workItemWrap = new WorkItemWrapper(DefaultEngineContext, workItem)
        {
            State = "Active"
        };
        var sut = new WorkItemStore(DefaultEngineContext);

        bool ok = await sut.TransitionToState(workItemWrap, "Resolved", false, "Some comment");

        Assert.False(ok);
        Logger.Received()
            .WriteError("WorkItem #42 state has already changed: cannot use TransitionToState");
    }

    [Fact]
    public async Task TransitionToState_NoPossibleTransition_Fails()
    {
        var workItemType = new WorkItemType
        {
            Name = "CustomMade",
            ReferenceName = "Acme.WorkItemTypes.CustomMade",
            IsDisabled = false,
            Color = "A4880A",
            States = new WorkItemStateColor[]
            {
                new WorkItemStateColor { Name = "Proposed",  Category = "Proposed" },
                new WorkItemStateColor { Name = "Active",  Category = "InProgress" },
                new WorkItemStateColor { Name = "Closed",  Category = "Completed" },
            },
            Transitions = new Dictionary<string, WorkItemStateTransition[]>
            {
                { "Active",new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Active" },
                    new WorkItemStateTransition { To = "Closed" },
                }},
                { "Closed",new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Closed" },
                }},
                { "Proposed",new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Proposed" },
                    new WorkItemStateTransition { To = "Active" },
                }},
                { "", new WorkItemStateTransition[] {
                    new WorkItemStateTransition { To = "Proposed" },
                }},
            }
        };
        WitClient.GetWorkItemTypeAsync(DefaultClientsContext.ProjectName, workItemType.Name).Returns(workItemType);
        var workItem = new WorkItem
        {
            Id = 42,
            Fields = new Dictionary<string, object>()
            {
                { CoreFieldRefNames.State, "Closed" },
                { CoreFieldRefNames.WorkItemType, workItemType.Name },
            }
        };
        var workItemWrap = new WorkItemWrapper(DefaultEngineContext, workItem);
        var sut = new WorkItemStore(DefaultEngineContext);

        bool ok = await sut.TransitionToState(workItemWrap, "Proposed", false, "Some comment");

        Assert.False(ok);
        await WitClient.DidNotReceiveWithAnyArgs()
            .UpdateWorkItemAsync(Arg.Any<JsonPatchDocument>(), Arg.Any<int>());
        Logger.Received()
            .WriteError($"Target state 'Proposed' cannot be reached from 'Closed' for work item type '{workItemType.Name}'");
    }

}

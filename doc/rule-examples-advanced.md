# Advanced Rule examples

## Backlog work items: Auto-activate parent

This is a more advanced version which has no hard coded work item type names.
It moves a parent work item to active state (activates parent), if a child gets activated and both parent and child are backlog work items


```csharp
//Method to check if a 'workItem' is in a 'Progress' state
bool IsInProgress(WorkItemWrapper workItem, BacklogWorkItemTypeStates workItemType)
{
    var concreteStateNames = workItemType?.StateCategoryStateNames
                                            .Where(category => string.Equals("InProgress", category.Key, StringComparison.OrdinalIgnoreCase))
                                            .SelectMany(category => category.Value);

    return concreteStateNames?.Contains(workItem.State) ?? false;
}

// First simple check if there is a parent
var parentWorkItem = self.Parent;
if (parentWorkItem == null)
{
    return "No Parent";
}

// now get the backlog work item types with their state to category mapping
var backlogWorkItems = await store.GetBacklogWorkItemTypesAndStates();
var backlogWorkItemsLookup = backlogWorkItems.ToDictionary(itemType => itemType.Name, itemType => itemType);

// Check if we are a back log work item and we are in an InProgress state
var workItemType = backlogWorkItemsLookup.GetValueOrDefault(self.WorkItemType);
if (!IsInProgress(self, workItemType))
{
    return workItemType == null ? "No Backlog work item type" : $"work item not <InProgress> (State={self.State})";
}

// Check if parent already in progress state and a back log work
var parentWorkItemType = backlogWorkItemsLookup.GetValueOrDefault(parentWorkItem.WorkItemType);
if (IsInProgress(parentWorkItem, parentWorkItemType))
{
    return parentWorkItemType == null ? "No Backlog work item type" : $"work item already <InProgress> (State={parentWorkItem.State})";
}

// Now set the parent to a state of the InProgress category
parentWorkItem.State = parentWorkItemType.StateCategoryStateNames["InProgress"].First();
return $"updated Parent {parentWorkItem.WorkItemType} #{parentWorkItem.Id} to State='{parentWorkItem.State}'";
```

## Backlog work items: Auto-Resolve parent

This is more similar to classic TFS Aggregator.
It moves a parent work item to Closed state, if all children are closed.
The major difference is the navigation: `Parent` and `Children` properties do not returns work items but relation. You have to explicitly query Azure DevOps to retrieve the referenced work items.

```csharp
//base method to check state
bool IsBacklogWorkItemInState(WorkItemWrapper workItem, BacklogWorkItemTypeStates workItemType, IEnumerable<string> expectedStateCategories)
{
    bool IsInExpectedStateCategory(KeyValuePair<string, string[]> category)
    {
        return expectedStateCategories.Any(expectedStateCategroy => string.Equals(expectedStateCategroy, category.Key, StringComparison.OrdinalIgnoreCase));
    }

    var concreteStateNames = workItemType?.StateCategoryStateNames
                                            .Where(IsInExpectedStateCategory)
                                            .SelectMany(category => category.Value);


    return concreteStateNames?.Contains(workItem.State) ?? false;
}

//Method to check if a 'workItem' is in a 'Removed' or 'Completed' state
bool IsRemovedOrCompleted(WorkItemWrapper workItem, BacklogWorkItemTypeStates workItemType)
{
    var expectedStateCategories = new string[]
                                {
                                    "Completed",
                                    "Removed",
                                };

    return IsBacklogWorkItemInState(workItem, workItemType, expectedStateCategories);
}

//Method to check if a 'workItem' is in a 'Resolved' state
bool IsResolved(WorkItemWrapper workItem, BacklogWorkItemTypeStates workItemType)
{
    var expectedStateCategories = new string[]
                            {
                                "Resolved",
                            };

    return IsBacklogWorkItemInState(workItem, workItemType, expectedStateCategories);
}


var parentWorkItem = workItem.Parent;
if (parentWorkItem == null)
{
    return "No Parent";
}

var backlogWorkItems = await store.GetBacklogWorkItemTypesAndStates();
var backlogWorkItemsLookup = backlogWorkItems.ToDictionary(itemType => itemType.Name, itemType => itemType);

var workItemType = backlogWorkItemsLookup.GetValueOrDefault(workItem.WorkItemType);
if (!IsRemovedOrCompleted(workItem, workItemType))
{
    return workItemType == null ? "No Backlog work item type" : $"work item not <Removed> or <Completed> (State={workItem.State})";
}

var parentWorkItemType = backlogWorkItemsLookup.GetValueOrDefault(parentWorkItem.WorkItemType);
if (parentWorkItem.IsResolved(parentWorkItemType))
{
    return parentWorkItem == null ? "No Backlog work item type" : $"work item already <Resolved> (State={parentWorkItem.State})";
}

if (!parentWorkItem.Children.All(item => IsRemovedOrCompleted(item, backlogWorkItemsLookup.GetValueOrDefault(item.WorkItemType))))
{
    return $"No all child work items <Removed> or <Completed>: {string.Join(",", parentWorkItem.Children.Select(item => $"#{item.Id}={item.State}"))}";
}

var resolvedStates = parentWorkItemType.StateCategoryStateNames[BacklogWorkItemStateCategory.Resolved];
parentWorkItem.State = resolvedStates.First();
return $"updated Parent #{parentWorkItem.Id} to State='{parentWorkItem.State}'";
```


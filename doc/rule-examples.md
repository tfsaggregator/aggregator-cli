# Rule examples


## Hello World

A trivial rule that returns some core fields of the work item which triggered the rule.

```
$"Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"
```


## Auto-close parent

This is more similar to classic TFS Aggregator.
It moves a parent work item to Closed state, if all children are closed.
The major difference is the navigation: `Parent` and `Children` properties do not returns work items but relation. You have to explicitly query Azure DevOps to retrieve the referenced work items.

```
string message = "";
var parent = self.Parent;
if (parent != null)
{
    var children = parent.Children;
    if (children.All(c => c.State == "Closed"))
    {
        parent.State = "Closed";
        message = "Parent was closed";
    }
    else
    {
        message = "Parent was not closed";
    }
    parent.Description = parent.Description + " aggregator was here.";
}
return message;
```


## Work item update

Check if a work item was updated and execute actions based on the changes, e.g. if work item Title was updated.

```
if (selfChanges.Fields.ContainsKey("System.Title"))
{
    var titleUpdate = selfChanges.Fields["System.Title"];
    return $"Title was changed from '{titleUpdate.OldValue}' to '{titleUpdate.NewValue}'";
}
else
{
    return "Title was not updated";
}
```


## History

`PreviousRevision` is different because retrieves a read-only version of the work item.

```
return self.PreviousRevision.PreviousRevision.Description;
```


# Create new Work Item
```
var parent = self;

// test to avoid infinite loop
if (parent.WorkItemType == "Task") {
    return "No root type";
}

var children = parent.Children;
// test to avoid infinite loop
if (!children.Any(c => c.Title == "Brand new child"))
{
    var newChild = store.NewWorkItem("Task");
    newChild.Title = "Brand new child";
    parent.Relations.AddChild(newChild);

    return "Item added";
}

return parent.Title;
```

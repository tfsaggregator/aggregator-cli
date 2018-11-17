# Rule examples

## Hello World

A trivial rule that returns some core fields of the work item which triggered the rule.

```
$"Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"
```

## Auto-close parent

This is more similar to classic TFS Aggregator.
It move a parent work item to Closed state, if all children are closed.
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

## History

`PreviousRevision` is different because retrieves a read-only version of the work item.

```
return self.PreviousRevision.PreviousRevision.Description;
```

# Create new Work Item
```
var parent = self;
var newChild = store.NewWorkItem("Task");
newChild.Title = "Brand new child";
parent.Relations.AddChild(newChild);

return parent.Title;
```

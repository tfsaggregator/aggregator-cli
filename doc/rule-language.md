# Directives

The directives must appear in the first lines of a Rule file.
They are parsed by Aggregator and removed before compiling the code.

`.lang=C#`
`.language=Csharp`

Currently the only supported language is C#. You can use the `.lang` directive to specify the programming language used by the rule.



# WorkItem Object
The initial WorkItem object is contained in the `self` variable.


## Revisions
Navigate to previous versions of the work item.

`WorkItem PreviousRevision`
Returns a read-only copy of the previous revision of this work item.

`IEnumerable<WorkItem> Revisions`
Returns a read-only copy of all revisions of this work item.


## Relations
Navigate to related work items.

`IEnumerable<WorkItemRelation> RelationLinks`
Returns all relations as `WorkItemRelation`.

`WorkItemRelationCollection Relations`
Returns a collection to navigate and modify relations.

`IEnumerable<WorkItemRelation> ChildrenLinks`
Returns the children links in Hierarchy relation, i.e. `System.LinkTypes.Hierarchy-Forward`.

`IEnumerable<WorkItem> Children`
Returns the children work items in Hierarchy relation, i.e. `System.LinkTypes.Hierarchy-Forward`. E.g. a _Task_ can be a child of a _User Story_.

`WorkItemRelation ParentLink`
Returns the parent link in Hierarchy relation, i.e. `System.LinkTypes.Hierarchy-Reverse`.

`WorkItem Parent`
Returns the parent work item in Hierarchy relation, i.e. `System.LinkTypes.Hierarchy-Reverse`. E.g. a _User Story_ is the parent of a _Task_.


## Links
Navigate links to non-work-item objects.

`IEnumerable<WorkItemRelation> RelatedLinks`
Returns related work items as `WorkItemRelation`.

`IEnumerable<WorkItemRelation> Hyperlinks`
Returns hyperlinks.

`int ExternalLinkCount`
Returns the number of links to external objects.

`int HyperLinkCount`
Returns the number of hyperlinks.

`int RelatedLinkCount`
Returns the number of related work items.


## Core Fields helpers
Data fields of the work item. See [Work item field index](https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/work-item-field?view=vsts) for a complete description.

`WorkItemId<int> Id` Read-only. Negative when `IsNew` equals `true`.

`int Rev` Read-only.

`string Url` Read-only.

`string WorkItemType` Read-only.

`string State`

`int AreaId`

`string AreaPath`

`IdentityRef AssignedTo`

`IdentityRef AuthorizedAs`

`IdentityRef ChangedBy`

`DateTime? ChangedDate`

`IdentityRef CreatedBy`

`DateTime? CreatedDate`

`string Description`

`string History`

`int IterationId`

`string IterationPath`

`string Reason`

`DateTime? RevisedDate`

`DateTime? AuthorizedDate`

`string TeamProject`

`string Tags`

`string Title`

`double Watermark` Read-only.


## Fields
`object this[string field]`
Read-write access to non-core fields.
Prefer using reference name, like _System.Title_, instead of language specific, like _Titolo_ or _Titel_.


## Status properties
`bool IsDeleted` Read-only.

`bool IsReadOnly` Read-only, returns `true` if work item cannot be modified.

`bool IsNew` Read-only, returns `true` if work item is new.

`bool IsDirty` Read-only, returns `true` if work item changed after retrieval.


## Attachments
`int AttachedFileCount`
Returns the number of attached files.



# WorkItemStore Object
The WorkItemStore object allows retrieval, creation and removal of work items.
This object is contained in the `store` variable.

`WorkItem GetWorkItem(int id)`
Returns a single work item.

`WorkItem GetWorkItem(WorkItemRelation item)`
Returns a single work item following the relation.

`IList<WorkItem> GetWorkItems(IEnumerable<int> ids)`
Returns a list of work items.

`IList<WorkItem> GetWorkItems(IEnumerable<WorkItemRelation> collection)`
Returns a list of work items following the relation.

`WorkItemWrapper NewWorkItem(string workItemType)`
Returns a new work item with a temporary Id. The work item is created when the rules ends.
`IsNew` returns `true`.



# WorkItemRelationCollection type
Navigate and modify related objects.

`IEnumerator<WorkItemRelation> GetEnumerator()`
Returns an enumerator on relations to use in `foreach` loops.

`Add(WorkItemRelation item)`
Adds the element to the collection.

`AddChild(WorkItemWrapper child)`
Adds a child work item.

`AddParent(WorkItemWrapper parent)`
Adds a parent work item.

`AddLink(string type, string url, string comment)`
Adds an element to the collection.

`AddHyperlink(string url, string comment = null)`
Adds a hyperlink to the collection.

`AddRelatedLink(WorkItem item, string comment = null)`
Adds a related work item to the collection.

`AddRelatedLink(string url, string comment = null)`
Adds a related work item to the collection.

`Clear()`
Removes all elements from the collection.

`bool Contains(WorkItemRelation item)`
Returns `true` if the element is present in the collection.

`bool Remove(WorkItemRelation item)`
Removes the element from the collection.

`int Count`
Returns the number of elements in the work item collection.

`bool IsReadOnly`
Returns `true` is collection is read-only.



# WorkItemRelation type

`string Title`
Read-only, returns the title property of the relation.

`string Rel`
Read-only, returns the type of the relation, e.g. `System.LinkTypes.Hierarchy-Reverse`.
See [Link type reference](https://docs.microsoft.com/en-us/azure/devops/boards/queries/link-type-reference).

`string Url`
Read-only, returns the URL to the target object.

`IDictionary<string, object> Attributes`
To manipulate the possible attributes of the relation. Currently Azure DevOps uses only the `comment` attribute.



# IdentityRef type
Represents a User identity. Use mostly as a read-only object. Use the `DisplayName` property to assign a user.

`string DirectoryAlias` 

`string DisplayName` Read-write, use this property to set an identity Field like `AssignedTo`.

`string Id` Read-only; Unique Id.

`string ImageUrl` Read-only; 

`bool Inactive` Read-only; `true` if 

`bool IsAadIdentity`

`bool IsContainer` Read-only; `true` for groups, `false` for users.

`string ProfileUrl`

`string UniqueName`

`string Url`

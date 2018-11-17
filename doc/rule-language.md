# Directives

`.lang=C#`
`.language=Csharp`



# WorkItem Object

## Revisions
Navigate to previous versions of the work item.
`WorkItem PreviousRevision`
`IEnumerable<WorkItem> Revisions`

## Relations
Navigate to related work items.
`IEnumerable<WorkItemRelation> RelationLinks`
`WorkItemRelationCollection Relations`
`IEnumerable<WorkItemRelation> ChildrenLinks`
`IEnumerable<WorkItem> Children`
`WorkItemRelation ParentLink`
`WorkItem Parent`

## Links
Navigate links to non-workitem objects.
`IEnumerable<WorkItemRelation> RelatedLinks`
`IEnumerable<WorkItemRelation> Hyperlinks`
`int ExternalLinkCount`
`int HyperLinkCount`
`int RelatedLinkCount`

## Fields
Data fields of the work item.
`WorkItemId<int> Id` Read-only.
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
`bool IsDeleted` Read-only.
`bool IsReadOnly` Read-only, returns `true` if work item cannot be modified.
`bool IsNew` Read-only.
`bool IsDirty` Read-only, returns `true` if work item changed after retrieval.
`object this[string field]` access to non-core fields.

## Attachments
`int AttachedFileCount`



# WorkItemStore Object
Retrival, creation and removal of work items.

`WorkItem GetWorkItem(int id)`
`WorkItem GetWorkItem(WorkItemRelation item)`

`IList<WorkItem> GetWorkItems(IEnumerable<int> ids)`
`IList<WorkItem> GetWorkItems(IEnumerable<WorkItemRelation> collection)`

`WorkItemWrapper NewWorkItem(string workItemType)`



# WorkItemRelationCollection
Navigate and modify related objects.

`IEnumerator<WorkItemRelation> GetEnumerator()`
`Add(WorkItemRelation item)`
`AddChild(WorkItemWrapper child)`
`AddParent(WorkItemWrapper parent)`
`AddLink(string type, string url, string comment)`
`AddHyperlink(string url, string comment = null)`
`AddRelatedLink(WorkItem item, string comment = null)`
`AddRelatedLink(string url, string comment = null)`
`Clear()`
`bool Contains(WorkItemRelation item)`
`bool Remove(WorkItemRelation item)`
`int Count`
`bool IsReadOnly`



# WorkItemRelation

`string Title`
`string Rel`
`string Url`
`IDictionary<string, object> Attributes`



# IdentityRef
Represents a User identity.

`string DirectoryAlias`
`string DisplayName`
`string Id`
`string ImageUrl`
`bool Inactive`
`bool IsAadIdentity`
`bool IsContainer`
`string ProfileUrl`
`string UniqueName`
`string Url`

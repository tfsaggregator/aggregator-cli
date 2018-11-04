# Directives

`.lang=C#`
`.language=Csharp`



# WorkItem Object

## Revisions
WorkItem PreviousRevision
IEnumerable<WorkItem> Revisions

## Relations
IEnumerable<WorkItemRelation> RelationLinks
WorkItemRelationCollection Relations
IEnumerable<WorkItemRelation> ChildrenLinks
IEnumerable<WorkItem> Children
WorkItemRelation ParentLink
WorkItem Parent

## Links
IEnumerable<WorkItemRelation> RelatedLinks
IEnumerable<WorkItemRelation> Hyperlinks
int ExternalLinkCount
int HyperLinkCount
int RelatedLinkCount

## Fields
WorkItemId<int> Id
int Rev
string Url
string WorkItemType
string State
int AreaId
string AreaPath
string AssignedTo
string AuthorizedAs
string ChangedBy
DateTime? ChangedDate
string CreatedBy
DateTime? CreatedDate
string Description
string History
int IterationId
string IterationPath
string Reason
DateTime? RevisedDate
DateTime? AuthorizedDate
string TeamProject
string Tags
string Title
double Watermark
bool IsDeleted
bool IsReadOnly
bool IsNew
bool IsDirty
object this[string field]

## Attachments
int AttachedFileCount



# WorkItemStore Object

WorkItem GetWorkItem(int id)
WorkItem GetWorkItem(WorkItemRelation item)

IList<WorkItem> GetWorkItems(IEnumerable<int> ids)
IList<WorkItem> GetWorkItems(IEnumerable<WorkItemRelation> collection)

WorkItemWrapper NewWorkItem(string workItemType)



# WorkItemRelationCollection

IEnumerator<WorkItemRelation> GetEnumerator()
Add(WorkItemRelation item)
AddChild(WorkItemWrapper child)
AddParent(WorkItemWrapper parent)
AddLink(string type, string url, string comment)
AddHyperlink(string url, string comment = null)
AddRelatedLink(WorkItem item, string comment = null)
AddRelatedLink(string url, string comment = null)
Clear()
bool Contains(WorkItemRelation item)
bool Remove(WorkItemRelation item)
int Count
bool IsReadOnly



# WorkItemRelation

string Title
string Rel
string Url
IDictionary<string, object> Attributes
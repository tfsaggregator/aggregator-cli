# Directives

`.lang=C#`
`.language=Csharp`

# WorkItem Object

WorkItem PreviousRevision
IEnumerable<WorkItem> Revisions
IEnumerable<WorkItemRelation> Relations
IEnumerable<WorkItemRelation> ChildrenLinks
IEnumerable<WorkItem> Children
IEnumerable<WorkItemRelation> RelatedLinks
IEnumerable<WorkItemRelation> Hyperlinks
WorkItemRelation ParentLink
WorkItem Parent
WorkItemId<int> Id
int Rev
string Url
string WorkItemType
string State
int AreaId
string AreaPath
string AssignedTo
int AttachedFileCount
string AuthorizedAs
string ChangedBy
DateTime? ChangedDate
string CreatedBy
DateTime? CreatedDate
string Description
int ExternalLinkCount
string History
int HyperLinkCount
int IterationId
string IterationPath
string Reason
int RelatedLinkCount
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

# WorkItemStore Object

WorkItem GetWorkItem(int id)
WorkItem GetWorkItem(WorkItemRelation item)

IList<WorkItem> GetWorkItems(IEnumerable<int> ids)
IList<WorkItem> GetWorkItems(IEnumerable<WorkItemRelation> collection)

# WorkItemRelationCollection

IEnumerator<WorkItemRelation> GetEnumerator()
Add(WorkItemRelation item)
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
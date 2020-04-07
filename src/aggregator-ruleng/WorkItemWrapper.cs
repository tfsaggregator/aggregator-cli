using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class WorkItemWrapper
    {
        private readonly EngineContext _context;
        private readonly WorkItem _item;

        internal WorkItemWrapper(EngineContext context, WorkItem item)
        {
            _context = context;
            _item = item;
            Relations = new WorkItemRelationWrapperCollection(this, _item.Relations);

            if (item.Id.HasValue)
            {
                Id = new PermanentWorkItemId(item.Id.Value);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Test,
                    Path = "/rev",
                    Value = item.Rev
                });
                //for simplify testing: item.Url can be null
                IsDeleted = item.Url?.EndsWith($"/recyclebin/{item.Id.Value}", StringComparison.OrdinalIgnoreCase) ?? false;

                IsReadOnly = false;
                _context.Tracker.TrackExisting(this);
            }
            else
            {
                Id = new TemporaryWorkItemId(_context.Tracker);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/id",
                    Value = Id.Value
                });

                _context.Tracker.TrackNew(this);
            }
        }


        internal WorkItemWrapper(EngineContext context, WorkItem item, bool isReadOnly)
        // we cannot reuse the code, because tracking is different
        //: this(context, item)
        {
            _context = context;
            _item = item;
            Relations = new WorkItemRelationWrapperCollection(this, _item.Relations);

            Id = new PermanentWorkItemId(item.Id.Value);
            Changes.Add(new JsonPatchOperation()
            {
                Operation = Operation.Test,
                Path = "/rev",
                Value = item.Rev
            });
            IsDeleted = item.Url?.EndsWith($"/recyclebin/{item.Id}", StringComparison.OrdinalIgnoreCase) ?? false;

            IsReadOnly = isReadOnly;
            _context.Tracker.TrackRevision(this);
        }

        public WorkItemWrapper PreviousRevision
        {
            get
            {
                if (Rev > 0)
                {
                    // TODO we shouldn't use the client in this class, move to WorkItemStore.GetRevisionAsync, workitemstore should check tracker if already loaded
                    // TODO think about passing workitemstore into workitemwrapper constructor, instead of engineContext, workitemstore is used several times, see also Property Children/Parent
                    var previousRevision = _context.Clients.WitClient.GetRevisionAsync(this.Id, this.Rev - 1, expand: WorkItemExpand.All).Result;
                    return new WorkItemWrapper(_context, previousRevision, true);
                }

                return null;
            }
        }

        public IEnumerable<WorkItemWrapper> Revisions
        {
            get
            {
                // TODO load a few revisions at a time
                //var all = _context.Clients.WitClient.GetRevisionsAsync(this.Id, expand: WorkItemExpand.All).Result;
                var revision = this;
                while ((revision = revision.PreviousRevision) != null)
                {
                    yield return revision;
                }
            }
        }

        public IEnumerable<WorkItemRelationWrapper> RelationLinks => Relations;

        public WorkItemRelationWrapperCollection Relations { get; }

        public IEnumerable<WorkItemRelationWrapper> ChildrenLinks
        {
            get
            {
                return Relations
                    .Where(rel => rel.Rel == CoreRelationRefNames.Children);
            }
        }

        public IEnumerable<WorkItemWrapper> Children
        {
            get
            {
                if (ChildrenLinks != null && ChildrenLinks.Any())
                {
                    var store = new WorkItemStore(_context);
                    return store.GetWorkItems(ChildrenLinks);
                }
                else
                    return new WorkItemWrapper[0];
            }
        }

        public IEnumerable<WorkItemRelationWrapper> RelatedLinks
        {
            get
            {
                return Relations
                    .Where(rel => rel.Rel == CoreRelationRefNames.Related);
            }
        }

        public IEnumerable<WorkItemRelationWrapper> Hyperlinks
        {
            get
            {
                return Relations
                    .Where(rel => rel.Rel == CoreRelationRefNames.Hyperlink);
            }
        }

        public WorkItemRelationWrapper ParentLink
        {
            get
            {
                return Relations
                    .SingleOrDefault(rel => rel.Rel == CoreRelationRefNames.Parent);
            }
        }

        public WorkItemWrapper Parent
        {
            get
            {
                if (ParentLink != null)
                {
                    var store = new WorkItemStore(_context);
                    return store.GetWorkItem(ParentLink);
                }
                else
                    return null;
            }
        }

        public WorkItemId Id
        {
            get;
            private set;
        }

        public int Rev => _item.Rev.Value;

        public string Url => _item.Url;

        public string WorkItemType
        {
            get => (string)_item.Fields[CoreFieldRefNames.WorkItemType];
            private set => SetFieldValue(CoreFieldRefNames.WorkItemType, value);
        }

        public string State
        {
            get => GetFieldValue<string>(CoreFieldRefNames.State);
            set => SetFieldValue(CoreFieldRefNames.State, value);
        }

        public int AreaId
        {
            get => GetFieldValue<int>(CoreFieldRefNames.AreaId);
            set => SetFieldValue(CoreFieldRefNames.AreaId, value);
        }

        public string AreaPath
        {
            get => GetFieldValue<string>(CoreFieldRefNames.AreaPath);
            set => SetFieldValue(CoreFieldRefNames.AreaPath, value);
        }

        public IdentityRef AssignedTo
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.AssignedTo);
            set => SetFieldValue(CoreFieldRefNames.AssignedTo, value);
        }

        public int AttachedFileCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.AttachedFileCount);
            set => SetFieldValue(CoreFieldRefNames.AttachedFileCount, value);
        }

        public IdentityRef AuthorizedAs
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.AuthorizedAs);
            set => SetFieldValue(CoreFieldRefNames.AuthorizedAs, value);
        }

        public IdentityRef ChangedBy
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.ChangedBy);
            set => SetFieldValue(CoreFieldRefNames.ChangedBy, value);
        }

        public DateTime? ChangedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.ChangedDate);
            set => SetFieldValue(CoreFieldRefNames.ChangedDate, value);
        }

        public IdentityRef CreatedBy
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.CreatedBy);
            set => SetFieldValue(CoreFieldRefNames.CreatedBy, value);
        }

        public DateTime? CreatedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.CreatedDate);
            set => SetFieldValue(CoreFieldRefNames.CreatedDate, value);
        }

        public string Description
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Description);
            set => SetFieldValue(CoreFieldRefNames.Description, value);
        }

        public int ExternalLinkCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.ExternalLinkCount);
            set => SetFieldValue(CoreFieldRefNames.ExternalLinkCount, value);
        }

        public string History
        {
            get => GetFieldValue<string>(CoreFieldRefNames.History);
            set => SetFieldValue(CoreFieldRefNames.History, value);
        }

        public int HyperLinkCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.HyperLinkCount);
            set => SetFieldValue(CoreFieldRefNames.HyperLinkCount, value);
        }

        public int IterationId
        {
            get => GetFieldValue<int>(CoreFieldRefNames.IterationId);
            set => SetFieldValue(CoreFieldRefNames.IterationId, value);
        }

        public string IterationPath
        {
            get => GetFieldValue<string>(CoreFieldRefNames.IterationPath);
            set => SetFieldValue(CoreFieldRefNames.IterationPath, value);
        }

        public string Reason
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Reason);
            set => SetFieldValue(CoreFieldRefNames.Reason, value);
        }

        public int RelatedLinkCount
        {
            get => GetFieldValue<int>(CoreFieldRefNames.RelatedLinkCount);
            set => SetFieldValue(CoreFieldRefNames.RelatedLinkCount, value);
        }

        public IdentityRef RevisedBy
        {
            get => GetFieldValue<IdentityRef>(CoreFieldRefNames.RevisedBy);
            set => SetFieldValue(CoreFieldRefNames.RevisedBy, value);
        }

        public DateTime? RevisedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.RevisedDate);
            set => SetFieldValue(CoreFieldRefNames.RevisedDate, value);
        }

        public DateTime? AuthorizedDate
        {
            get => GetFieldValue<DateTime?>(CoreFieldRefNames.AuthorizedDate);
            set => SetFieldValue(CoreFieldRefNames.AuthorizedDate, value);
        }

        public string TeamProject
        {
            get => GetFieldValue<string>(CoreFieldRefNames.TeamProject);
            set => SetFieldValue(CoreFieldRefNames.TeamProject, value);
        }

        public string Tags
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Tags);
            set => SetFieldValue(CoreFieldRefNames.Tags, value);
        }

        public string Title
        {
            get => GetFieldValue<string>(CoreFieldRefNames.Title);
            set => SetFieldValue(CoreFieldRefNames.Title, value);
        }

        public double Watermark
        {
            get => GetFieldValue<double>(CoreFieldRefNames.Watermark);
            set => SetFieldValue(CoreFieldRefNames.Watermark, value);
        }

        public bool IsDeleted { get; }

        public bool IsReadOnly { get; } = false;

        public bool IsNew => Id is TemporaryWorkItemId;

        public bool IsDirty { get; internal set; }

        internal RecycleStatus RecycleStatus { get; set; } = RecycleStatus.NoChange;

        internal JsonPatchDocument Changes { get; } = new JsonPatchDocument();

        public object this[string field]
        {
            get => GetFieldValue<object>(field);
            set => SetFieldValue(field, value);
        }

        private void SetFieldValue(string field, object value)
        {
            if (IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            if (_item.Fields.ContainsKey(field))
            {
                if (_item.Fields[field].Equals(value))
                {
                    // if new value does not differ from existing value, just ignore change
                    return;
                }

                _item.Fields[field] = value;
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Replace,
                    Path = "/fields/" + field,
                    Value = TranslateValue(value)
                });
            }
            else
            {
                _item.Fields.Add(field, value);
                Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/" + field,
                    Value = TranslateValue(value)
                });
            }

            IsDirty = true;
        }

        private static object TranslateValue(object value)
        {
            switch (value)
            {
                case IdentityRef id:
                {
                    return id.DisplayName;
                }
                case WorkItemId id:
                {
                    return id.Value;
                }
                default:
                {
                    return value;
                }
            }
        }

        private T GetFieldValue<T>(string field)
        {
            return _item.Fields.TryGetValue(field, out var value)
                ? (T)value
                : default;
        }

        public T GetFieldValue<T>(string field, T defaultValue)
        {
            return _item.Fields.TryGetValue(field, out var value)
                ? (T)Convert.ChangeType(value, typeof(T))
                : defaultValue;
        }

        internal void ReplaceIdAndResetChanges(int oldId, int newId)
        {
            if (oldId >= 0) throw new ArgumentOutOfRangeException(nameof(oldId));

            Id = new PermanentWorkItemId(newId);

            var candidates = Changes.Where(op => op.Path == "/relations/-");
            foreach (var patch in candidates.Select(op => op.Value as RelationPatch).Where(WorkItemRelationWrapper.IsWorkItemRelation))
            {
                var url = new Uri(patch.url);
                var idName = url.Segments.Last();
                var relId = int.TryParse(idName, out var i) ? i : (int?)null;
                if (relId.HasValue && relId.Value == oldId)
                {
                    //last element is Id
                    var newUrl = new Uri(url, $"../{newId}");
                    patch.url = newUrl.ToString();
                    break;
                }
            }

            Changes.RemoveAll(op => op.Path.StartsWith("/fields/", StringComparison.OrdinalIgnoreCase) || op.Path == "/id");
        }

        internal void RemapIdReferences(IDictionary<int, int> realIds)
        {
            var candidates = Changes.Where(op => op.Path == "/relations/-");
            foreach (var patch in candidates.Select(op => op.Value as RelationPatch).Where(WorkItemRelationWrapper.IsWorkItemRelation))
            {
                var url = new Uri(patch.url);
                var idName = url.Segments.Last();
                var relId = int.TryParse(idName, out var i) ? i : (int?)null;
                if (relId.HasValue && realIds.TryGetValue(relId.Value, out var newId))
                {
                    //last element is Id
                    var newUrl = new Uri(url, $"../{newId}");
                    patch.url = newUrl.ToString();
                }
            }
        }
    }

    internal enum RecycleStatus
    {
        NoChange,
        ToDelete,
        ToRestore,
    }
}

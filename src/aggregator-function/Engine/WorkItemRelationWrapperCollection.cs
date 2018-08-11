using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapperCollection : ICollection<WorkItemRelationWrapper>
    {
        private WorkItemWrapper _pivotWorkItem;
        private IList<WorkItemRelationWrapper> _original;
        private IList<WorkItemRelationWrapper> _current;

        internal WorkItemRelationWrapperCollection(WorkItemWrapper workItem, IList<WorkItemRelation> relations)
        {
            _pivotWorkItem = workItem;
            _original = relations == null
                ? new List<WorkItemRelationWrapper>()
                : new List<WorkItemRelationWrapper>(relations.Select(relation =>
                    new WorkItemRelationWrapper(_pivotWorkItem, relation)));
            // do we need deep cloning?
            _current = new List<WorkItemRelationWrapper>(_original);
        }

        private void AddRelation(WorkItemRelationWrapper item)
        {
            if (_pivotWorkItem.IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            _current.Add(item);

            _pivotWorkItem.Changes.Add(new JsonPatchOperation()
            {
                Operation = Operation.Add,
                Path = "/relations/-",
                Value = new
                {
                    rel = item.Rel,
                    url = item.Url,
                    attributes = new
                    {
                        comment = item.Attributes["comment"]
                    }
                }
            });

            _pivotWorkItem.IsDirty = true;
        }

        private bool RemoveRelation(WorkItemRelationWrapper item)
        {
            if (_pivotWorkItem.IsReadOnly)
            {
                throw new InvalidOperationException("Work item is read-only.");
            }

            if (_current.Remove(item))
            {
                _pivotWorkItem.Changes.Add(new JsonPatchOperation()
                {
                    Operation = Operation.Remove,
                    Path = "/relations/-",
                    Value = _original.IndexOf(item)
                });
                _pivotWorkItem.IsDirty = true;
                return true;
            }

            return false;
        }

        public IEnumerator<WorkItemRelationWrapper> GetEnumerator()
        {
            return _current.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable)_current).GetEnumerator();
        }

        public void Add(WorkItemRelationWrapper item)
        {
            AddRelation(item);
        }

        public void AddLink(string type, string url, string comment)
        {
            AddRelation(new WorkItemRelationWrapper(
                _pivotWorkItem,
                type,
                url,
                comment
            ));
        }


        public void AddHyperlink(string url, string comment = null)
        {
            AddLink(
                "Hyperlink",
                url,
                comment
            );
        }

        public void AddRelatedLink(WorkItemWrapper item, string comment = null)
        {
            AddRelatedLink(item.Url, comment);
        }


        public void AddRelatedLink(string url, string comment = null)
        {
            AddLink(
                "System.LinkTypes.Related",
                url,
                comment
            );
        }

        public void Clear()
        {
            foreach (var item in _current.ToArray())
            {
                RemoveRelation(item);
            }
        }

        public bool Contains(WorkItemRelationWrapper item)
        {
            return _current.Contains(item);
        }

        public void CopyTo(WorkItemRelationWrapper[] array, int arrayIndex)
        {
            _current.CopyTo(array, arrayIndex);
        }

        public bool Remove(WorkItemRelationWrapper item)
        {
            return RemoveRelation(item);
        }

        public int Count => _current.Count;

        public bool IsReadOnly => _pivotWorkItem.IsReadOnly;
    }
}

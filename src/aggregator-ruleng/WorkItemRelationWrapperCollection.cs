﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace aggregator.Engine
{
    public class WorkItemRelationWrapperCollection : ICollection<WorkItemRelationWrapper>
    {
        private readonly WorkItemWrapper _pivotWorkItem;
        private readonly IList<WorkItemRelationWrapper> _original;
        private readonly IList<WorkItemRelationWrapper> _current;

        internal WorkItemRelationWrapperCollection(WorkItemWrapper workItem, IList<WorkItemRelation> relations)
        {
            _pivotWorkItem = workItem;
            _original = relations == null
                ? new List<WorkItemRelationWrapper>()
                : relations.Where(WorkItemRelationWrapper.IsWorkItemRelation)
                            .Select(relation => new WorkItemRelationWrapper(relation))
                           .ToList();

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
                Value = new RelationPatch
                {
                    rel = item.Rel,
                    url = item.Url,
                    attributes = item.Attributes != null && item.Attributes.TryGetValue("comment", out object value)
                        ? new { comment = value }
                        : null
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

        public void AddChild(WorkItemWrapper child)
        {
            var r = new WorkItemRelationWrapper(CoreRelationRefNames.Children, child.Url, string.Empty);
            AddRelation(r);
        }

        public void AddParent(WorkItemWrapper parent)
        {
            var r = new WorkItemRelationWrapper(CoreRelationRefNames.Parent, parent.Url, string.Empty);
            AddRelation(r);
        }

        public void AddLink(string type, string url, string comment)
        {
            AddRelation(new WorkItemRelationWrapper(
                type,
                url,
                comment
            ));
        }


        public void AddHyperlink(string url, string comment = null)
        {
            AddLink(
                CoreRelationRefNames.Hyperlink,
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
                CoreRelationRefNames.Related,
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

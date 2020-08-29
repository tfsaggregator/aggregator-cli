﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    class Tracker
    {
        class TrackedWrapper
        {
            internal int Id { get; }

            internal WorkItemWrapper Current { get; }

            internal IDictionary<int, WorkItemWrapper> Revisions = new Dictionary<int, WorkItemWrapper>();

            internal TrackedWrapper(int id, WorkItemWrapper wrapper)
            {
                Id = id;
                Current = wrapper;
            }
        }

        private int watermark = -1;
        IDictionary<WorkItemId, TrackedWrapper> tracked = new Dictionary<WorkItemId, TrackedWrapper>();

        internal Tracker()
        {
        }

        internal int GetNextWatermark()
        {
            return watermark--;
        }

        internal void TrackExisting(WorkItemWrapper workItemWrapper)
        {
            if (IsTracked(workItemWrapper))
            {
                throw new InvalidOperationException($"Work item {workItemWrapper.Id} is already tracked");
            }
            TrackNew(workItemWrapper);
        }

        internal void TrackNew(WorkItemWrapper workItemWrapper)
        {
            var t = new TrackedWrapper(workItemWrapper.Id, workItemWrapper);
            tracked.Add(workItemWrapper.Id, t);
        }

        internal void TrackRevision(WorkItemWrapper workItemWrapper)
        {
            if (tracked.TryGetValue(workItemWrapper.Id, out var value))
            {
                value.Revisions.Add(workItemWrapper.Rev, workItemWrapper);
            }
            else
            {
                // should never happen...
                throw new InvalidOperationException($"Work item {workItemWrapper.Id} was never loaded");
            }
        }

        internal WorkItemWrapper LoadWorkItem(int id, Func<int, WorkItemWrapper> loader)
        {
            var key = new PermanentWorkItemId(id);
            return tracked.TryGetValue(key, out var value)
                ? value.Current
                : loader(id);
        }

        internal IList<WorkItemWrapper> LoadWorkItems(
                IEnumerable<int> ids,
                Func<IEnumerable<int>, IList<WorkItemWrapper>> loader)
        {
            var groups = ids
                .Select(id => new PermanentWorkItemId(id))
                .GroupBy(k => tracked.ContainsKey(k))
                .ToDictionary(g => g.Key, g => g.ToList());
            // groups[true] is the set of IDs already tracked
            // groups[false] is the set of new IDs

            var inMemory = tracked
                .Where(w => groups.ContainsKey(true) && groups[true].Contains(w.Key))
                .Select(w => w.Value.Current);

            if (groups.ContainsKey(false))
            {
                var loaded = loader(groups[false].Select(k => k.Value));

                return inMemory.Union(loaded).ToList();
            }
            else
            {
                return inMemory.ToList();
            }
        }

        internal bool IsTracked(WorkItemWrapper workItemWrapper)
        {
            return tracked.ContainsKey(workItemWrapper.Id);
        }

        internal (WorkItemWrapper[] Created, WorkItemWrapper[] Updated, WorkItemWrapper[] Deleted, WorkItemWrapper[] Restored) GetChangedWorkItems()
        {
            var trackedChanged = tracked
                .Where(w => !w.Value.Current.IsReadOnly && w.Value.Current.IsDirty && !w.Value.Current.IsNew)
                .ToList();

            var @new = tracked.Where(w => !w.Value.Current.IsReadOnly && w.Value.Current.IsNew).Select(w => w.Value.Current).ToArray();
            var updated = trackedChanged.Where(w => w.Value.Current.RecycleStatus == RecycleStatus.NoChange).Select(w => w.Value.Current).ToArray();
            var deleted = trackedChanged.Where(w => w.Value.Current.RecycleStatus == RecycleStatus.ToDelete).Select(w => w.Value.Current).ToArray();
            var restored = trackedChanged.Where(w => w.Value.Current.RecycleStatus == RecycleStatus.ToRestore).Select(w => w.Value.Current).ToArray();
            return (@new, updated, deleted, restored);
        }
    }
}

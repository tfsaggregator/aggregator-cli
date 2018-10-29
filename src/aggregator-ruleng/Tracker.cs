using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
        IDictionary<WorkItemId<int>, TrackedWrapper> tracked = new Dictionary<WorkItemId<int>, TrackedWrapper>();

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
            var t = new TrackedWrapper(workItemWrapper.Id.Value, workItemWrapper);
            tracked.Add(workItemWrapper.Id, t);
        }

        internal void TrackNew(WorkItemWrapper workItemWrapper)
        {
            var t = new TrackedWrapper(workItemWrapper.Id.Value, workItemWrapper);
            tracked.Add(workItemWrapper.Id, t);
        }

        internal void TrackRevision(WorkItemWrapper workItemWrapper)
        {
            if (!tracked.ContainsKey(workItemWrapper.Id))
            {
                // should never happen...
                throw new InvalidOperationException($"Work item {workItemWrapper.Id} was never loaded");
            }
            tracked[workItemWrapper.Id].Revisions.Add(workItemWrapper.Rev, workItemWrapper);
        }

        internal WorkItemWrapper LoadWorkItem(int id, Func<int, WorkItemWrapper> loader)
        {
            var key = new PermanentWorkItemId(id);
            return tracked.ContainsKey(key)
                ? tracked[key].Current
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

            var inMemory = tracked
                .Where(w => groups.ContainsKey(true)
                        ? groups[true].Contains(w.Key)
                        : false)
                .Select(w => w.Value.Current);
            var loaded = loader(groups[false].Select(k => k.Value));

            return inMemory.Union(loaded).ToList();
        }

        internal bool IsTracked(WorkItemWrapper workItemWrapper)
        {
            return tracked.ContainsKey(workItemWrapper.Id);
        }

        internal IEnumerable<WorkItemWrapper> NewWorkItems
            => tracked
            .Where(w => !w.Value.Current.IsReadOnly && w.Value.Current.IsNew)
            .Select(w=>w.Value.Current);

        internal IEnumerable<WorkItemWrapper> ChangedWorkItems
            => tracked
            .Where(w => !w.Value.Current.IsReadOnly && w.Value.Current.IsDirty && !w.Value.Current.IsNew)
            .Select(w => w.Value.Current);
    }
}

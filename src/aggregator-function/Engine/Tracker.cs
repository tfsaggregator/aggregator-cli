using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace aggregator.Engine
{
    class Tracker
    {
        IDictionary<WorkItemId<int>, WorkItemWrapper> wrappers = new Dictionary<WorkItemId<int>, WorkItemWrapper>();

        internal Tracker()
        {
        }

        internal void Track(WorkItemWrapper workItemWrapper)
        {
            if (IsTracked(workItemWrapper))
            {
                throw new InvalidOperationException($"Work item {workItemWrapper.Id} is already tracked");
            }
            wrappers.Add(workItemWrapper.Id, workItemWrapper);
        }

        internal WorkItemWrapper LoadWorkItem(int id, Func<int, WorkItemWrapper> loader)
        {
            var key = new PermanentWorkItemId(id);
            return wrappers.ContainsKey(key)
                ? wrappers[key]
                : loader(id);
        }

        internal IList<WorkItemWrapper> LoadWorkItems(
                IEnumerable<int> ids,
                Func<IEnumerable<int>, IList<WorkItemWrapper>> loader)
        {
            var groups = ids
                .Select(id => new PermanentWorkItemId(id))
                .GroupBy(k => wrappers.ContainsKey(k))
                .ToDictionary(g => g.Key, g => g.ToList());

            var inMemory = wrappers.Where(w => groups[true].Contains(w.Key)).Select(w => w.Value);
            var loaded = loader(groups[false].Select(k => k.Value));

            return inMemory.Union(loaded).ToList();
        }

        internal bool IsTracked(WorkItemWrapper workItemWrapper)
        {
            return wrappers.ContainsKey(workItemWrapper.Id);
        }

        internal IEnumerable<WorkItemWrapper> NewWorkItems
            => wrappers
            .Where(w => !w.Value.IsReadOnly && w.Value.IsDirty && w.Value.IsNew)
            .Select(w=>w.Value);

        internal IEnumerable<WorkItemWrapper> ChangedWorkItems
            => wrappers
            .Where(w => !w.Value.IsReadOnly && w.Value.IsDirty && !w.Value.IsNew)
            .Select(w => w.Value);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;


namespace aggregator.Engine
{
    public class WorkItemUpdateWrapper
    {
        private static readonly WorkItemUpdate _noUpdate = new();
        private readonly WorkItemUpdate _workItemUpdate;

        public WorkItemUpdateWrapper(WorkItemUpdate workItemUpdate)
        {
            _workItemUpdate = workItemUpdate ?? _noUpdate;

            Relations = new WorkItemRelationUpdatesWrapper()
            {
                Added = _workItemUpdate.Relations?.Added?.Where(WorkItemRelationWrapper.IsWorkItemRelation).Select(relation => new WorkItemRelationWrapper(relation)).ToList() ?? new List<WorkItemRelationWrapper>(),
                Removed = _workItemUpdate.Relations?.Removed?.Where(WorkItemRelationWrapper.IsWorkItemRelation).Select(relation => new WorkItemRelationWrapper(relation)).ToList() ?? new List<WorkItemRelationWrapper>(),
                Updated = _workItemUpdate.Relations?.Updated?.Where(WorkItemRelationWrapper.IsWorkItemRelation).Select(relation => new WorkItemRelationWrapper(relation)).ToList() ?? new List<WorkItemRelationWrapper>(),
            };

            Fields = _workItemUpdate.Fields?
                                    .ToDictionary(kvp => kvp.Key,
                                                  kvp => new WorkItemFieldUpdateWrapper()
                                                  {
                                                      NewValue = kvp.Value.NewValue,
                                                      OldValue = kvp.Value.OldValue,
                                                  })
                                    ?? new Dictionary<string, WorkItemFieldUpdateWrapper>();
        }

        /// <summary>ID of update.</summary>
        public int Id => _workItemUpdate.Id;

        /// <summary>The work item ID.</summary>
        public int WorkItemId => _workItemUpdate.WorkItemId;

        /// <summary>The revision number of work item update.</summary>
        public int Rev => _workItemUpdate.Rev;

        /// <summary>Identity for the work item update.</summary>
        public IdentityRef RevisedBy => _workItemUpdate.RevisedBy;

        /// <summary>The work item updates revision date.</summary>
        public DateTime RevisedDate => _workItemUpdate.RevisedDate;

        /// <summary>List of updates to fields.</summary>
        public IDictionary<string, WorkItemFieldUpdateWrapper> Fields { get; }

        /// <summary>List of updates to relations.</summary>
        public WorkItemRelationUpdatesWrapper Relations { get; }

        public string Url => _workItemUpdate.Url;
    }

    public class WorkItemFieldUpdateWrapper
    {
        /// <summary> The old value of the field.</summary>
        public object OldValue { get; set; }

        /// <summary> The new value of the field.</summary>
        public object NewValue { get; set; }
    }

    public class WorkItemRelationUpdatesWrapper
    {
        public WorkItemRelationUpdatesWrapper()
        {
            Added = new List<WorkItemRelationWrapper>();
            Removed = new List<WorkItemRelationWrapper>();
            Updated = new List<WorkItemRelationWrapper>();
        }

        /// <summary>
        /// List of newly added relations.
        /// </summary>
        public IReadOnlyCollection<WorkItemRelationWrapper> Added { get; set; }

        /// <summary>
        /// List of removed relations.
        /// </summary>
        public IReadOnlyCollection<WorkItemRelationWrapper> Removed { get; set; }

        /// <summary>
        /// List of updated relations.
        /// </summary>
        public IReadOnlyCollection<WorkItemRelationWrapper> Updated { get; set; }
    }
}

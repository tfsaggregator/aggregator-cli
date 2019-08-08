using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;


namespace aggregator.Engine
{
    public class WorkItemUpdateWrapper
    {
        private WorkItemUpdate _workItemUpdate;

        public WorkItemUpdateWrapper(WorkItemUpdate workItemUpdate)
        {
            _workItemUpdate = workItemUpdate ?? new WorkItemUpdate();

            Relations = new WorkItemRelationUpdatesWrapper()
                        {
                            Added = workItemUpdate.Relations?.Added?.Select(relation => new WorkItemRelationWrapper(relation)).ToList() ?? Enumerable.Empty<WorkItemRelationWrapper>(),
                            Removed = workItemUpdate.Relations?.Removed?.Select(relation => new WorkItemRelationWrapper(relation)).ToList() ?? Enumerable.Empty<WorkItemRelationWrapper>(),
                            Updated = workItemUpdate.Relations?.Updated?.Select(relation => new WorkItemRelationWrapper(relation)).ToList() ?? Enumerable.Empty<WorkItemRelationWrapper>(),
                        };

            Fields = workItemUpdate.Fields.ToDictionary(kvp => kvp.Key,
                                                        kvp => new WorkItemFieldUpdateWrapper()
                                                               {
                                                                   NewValue = kvp.Value.NewValue,
                                                                   OldValue = kvp.Value.OldValue,
                                                               });
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
        public object OldValue
        {
            get;
            set;
        }

        public object NewValue
        {
            get;
            set;
        }
    }

    public class WorkItemRelationUpdatesWrapper
    {
        public IEnumerable<WorkItemRelationWrapper> Added
        {
            get;
            set;
        }

        public IEnumerable<WorkItemRelationWrapper> Removed
        {
            get;
            set;
        }

        public IEnumerable<WorkItemRelationWrapper> Updated
        {
            get;
            set;
        }
    }
}
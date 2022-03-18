using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;


namespace aggregator.Engine
{
    public class WorkItemData
    {

        public WorkItemData(WorkItem workItem, WorkItemUpdate workItemUpdate = null)
        {
            WorkItem = workItem;
            WorkItemUpdate = workItemUpdate ?? new WorkItemUpdate();
        }

        public WorkItem WorkItem { get; }

        public WorkItemUpdate WorkItemUpdate { get; }

        public static implicit operator WorkItemData(WorkItem workItem) => new(workItem);
    }
}

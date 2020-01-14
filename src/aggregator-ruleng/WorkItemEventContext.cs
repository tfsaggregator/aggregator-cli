using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace aggregator.Engine
{

    public class WorkItemEventContext
    {
        public WorkItemEventContext(Guid projectId, Uri collectionUri, WorkItem workItem, WorkItemUpdate workItemUpdate = null)
        {
            ProjectId = projectId;
            CollectionUri = collectionUri;
            WorkItemPayload = new WorkItemData(workItem, workItemUpdate);
        }

        public WorkItemData WorkItemPayload { get; }
        public Guid ProjectId { get; }
        public Uri CollectionUri { get; }
    }

    public static class WorkItemEventContextExtension
    {
        public static bool IsTestEvent(this WorkItemEventContext eventContext)
        {
            const string TEST_EVENT_COLLECTION_URL = "http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/";

            var workItem = eventContext.WorkItemPayload.WorkItem;
            return workItem.Url.StartsWith(TEST_EVENT_COLLECTION_URL, StringComparison.OrdinalIgnoreCase);
        }
    }
}

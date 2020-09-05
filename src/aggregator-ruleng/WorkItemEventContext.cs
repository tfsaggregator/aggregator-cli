using System;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

namespace aggregator.Engine
{

    public class WorkItemEventContext
    {
        public WorkItemEventContext(Guid projectId, Uri collectionUri, WorkItem workItem, string eventType, WorkItemUpdate workItemUpdate = null)
        {
            ProjectId = projectId;
            CollectionUri = collectionUri;
            WorkItemPayload = new WorkItemData(workItem, workItemUpdate);
            EventType = eventType;
        }

        public WorkItemData WorkItemPayload { get; }
        public Guid ProjectId { get; }
        public Uri CollectionUri { get; }
        public string EventType { get; }
    }

    public static class WorkItemEventContextExtension
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "In this case it is bogus data")]
        public static bool IsTestEvent(this WorkItemEventContext eventContext)
        {
            const string TEST_EVENT_COLLECTION_URL = "http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/";

            var workItem = eventContext.WorkItemPayload.WorkItem;
            return workItem.Url.StartsWith(TEST_EVENT_COLLECTION_URL, StringComparison.OrdinalIgnoreCase);
        }
    }
}

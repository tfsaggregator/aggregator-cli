using System;
using System.Collections.Generic;
using System.Text;

using aggregator.Engine;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;


namespace aggregator
{

    //TODO BobSilent Find better name
    //TODO BobSilent Move to shared??
    internal class WorkItemEventContext
    {
        public WorkItemEventContext(Guid projectId, Uri collectionUri, WorkItem workItem, WorkItemUpdate workItemUpdate = null)
        {
            ProjectId = projectId;
            CollectionUri = collectionUri;
            WorkItemPayload = new WorkItemData(workItem, workItemUpdate);
        }

        internal WorkItemData WorkItemPayload { get; }
        internal Guid ProjectId { get; }
        internal Uri CollectionUri { get; }
    }

    internal static class WorkItemEventContextExtension
    {
        public static bool IsTestEvent(this WorkItemEventContext eventContext)
        {
            const string TEST_EVENT_COLLECTION_URL = "http://fabrikam-fiber-inc.visualstudio.com/DefaultCollection/";

            var workItem = eventContext.WorkItemPayload.WorkItem;
            return workItem.Url.StartsWith(TEST_EVENT_COLLECTION_URL, StringComparison.OrdinalIgnoreCase);
        }
    }
}

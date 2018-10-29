using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class EngineContext
    {
        public EngineContext(WorkItemTrackingHttpClientBase client, Guid projectId, IAggregatorLogger logger)
        {
            Client = client;
            Logger = logger;
            Tracker = new Tracker();
            ProjectId = projectId;
        }

        public Guid ProjectId { get; internal set; }
        internal WorkItemTrackingHttpClientBase Client { get; }
        internal IAggregatorLogger Logger { get; }
        internal Tracker Tracker { get; }
    }
}

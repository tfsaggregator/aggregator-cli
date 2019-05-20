using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class EngineContext
    {
        public EngineContext(WorkItemTrackingHttpClient client, string projectName, IAggregatorLogger logger)
        {
            Client = client;
            Logger = logger;
            Tracker = new Tracker();
            ProjectName = projectName;
        }

        public string ProjectName { get; internal set; }
        internal WorkItemTrackingHttpClient Client { get; }
        internal IAggregatorLogger Logger { get; }
        internal Tracker Tracker { get; }
    }
}

using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class EngineContext
    {
        internal EngineContext(WorkItemTrackingHttpClient client, ILogger logger)
        {
            Client = client;
            Logger = logger;
            Tracker = new Tracker();
        }

        internal WorkItemTrackingHttpClient Client { get; }
        internal ILogger Logger { get; }
        internal Tracker Tracker { get; }
    }
}

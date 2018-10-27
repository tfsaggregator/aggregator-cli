using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class EngineContext
    {
        public EngineContext(WorkItemTrackingHttpClientBase client, IAggregatorLogger logger)
        {
            Client = client;
            Logger = logger;
            Tracker = new Tracker();
        }

        internal WorkItemTrackingHttpClientBase Client { get; }
        internal IAggregatorLogger Logger { get; }
        internal Tracker Tracker { get; }
    }
}

using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace aggregator.Engine
{
    public class EngineContext
    {
        public EngineContext(WorkItemTrackingHttpClientBase client, Guid projectId, string projectName, string personalAccessToken, IAggregatorLogger logger)
        {
            Client = client;
            Logger = logger;
            Tracker = new Tracker();
            ProjectId = projectId;
            ProjectName = projectName;
            PersonalAccessToken = personalAccessToken;
        }

        public Guid ProjectId { get; internal set; }
        public string ProjectName { get; internal set; }
        public string PersonalAccessToken { get; internal set; }
        internal WorkItemTrackingHttpClientBase Client { get; }
        internal IAggregatorLogger Logger { get; }
        internal Tracker Tracker { get; }
    }
}

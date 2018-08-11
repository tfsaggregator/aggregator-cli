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
        }

        internal WorkItemTrackingHttpClient Client { get; }
        internal ILogger Logger { get; }

        List<WorkItemWrapper> wrappers = new List<WorkItemWrapper>();

        internal EngineContext Track(WorkItemWrapper workItemWrapper)
        {
            wrappers.Add(workItemWrapper);
            return this;
        }

        internal void SaveChanges()
        {
            var todo = wrappers.Where(w => !w.IsReadOnly && w.IsDirty);

            var toCreate = todo.Where(w => w.IsNew);
            foreach (var item in toCreate)
            {
                Client.CreateWorkItemAsync(
                    item.Changes,
                    item.TeamProject,
                    item.WorkItemType
                );
            }

            var toUpdate = todo.Where(w => w.IsDirty);
            foreach (var item in toUpdate)
            {
                Client.UpdateWorkItemAsync(
                    item.Changes,
                    item.Id.Value
                );
            }
        }
    }
}

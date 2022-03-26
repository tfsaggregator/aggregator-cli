using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Work.WebApi.Contracts;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;
using Newtonsoft.Json;

namespace aggregator.Engine.Persistance
{
    internal class PersistBatch : PersisterBase
    {
        public PersistBatch(EngineContext context)
            : base(context) { }

        internal async Task<(int created, int updated)> SaveChanges_Batch(bool commit, bool impersonate, bool bypassrules, CancellationToken cancellationToken)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // BUG this code won't work if there is a relation between a new (id<0) work item and an existing one (id>0): it is an API limit

            var (createdWorkItems, updatedWorkItems, deletedWorkItems, restoredWorkItems) = _context.Tracker.GetChangedWorkItems();
            int createdCounter = createdWorkItems.Length;
            int updatedCounter = updatedWorkItems.Length + deletedWorkItems.Length + restoredWorkItems.Length;

            List<WitBatchRequest> batchRequests = new List<WitBatchRequest>();
            foreach (var item in createdWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request for a new {item.WorkItemType} workitem in {item.TeamProject}");

                var request = _clients.WitClient.CreateWorkItemBatchRequest(_context.ProjectName,
                                                                            item.WorkItemType,
                                                                            item.Changes,
                                                                            bypassRules: impersonate,
                                                                            suppressNotifications: false);
                batchRequests.Add(request);
            }

            foreach (var item in updatedWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request to update workitem {item.Id} in {item.TeamProject}");

                var request = _clients.WitClient.CreateWorkItemBatchRequest(item.Id,
                                                                            item.Changes,
                                                                            bypassRules: impersonate || bypassrules,
                                                                            suppressNotifications: false);
                batchRequests.Add(request);
            }

            var converters = new JsonConverter[] { new JsonPatchOperationConverter() };
            string requestBody = JsonConvert.SerializeObject(batchRequests, Formatting.None, converters);
            _context.Logger.WriteVerbose(requestBody);

            if (commit)
            {
                _ = await ExecuteBatchRequest(batchRequests, cancellationToken);
                await RestoreAndDelete(restoredWorkItems, deletedWorkItems, cancellationToken);
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
            }//if

            return (createdCounter, updatedCounter);
        }
    }
}

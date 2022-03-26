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
    internal class PersistTwoPhases : PersisterBase
    {
        public PersistTwoPhases(EngineContext context)
            : base(context) { }

        //TODO no error handling here? SaveChanges_Batch has at least the DryRun support and error handling
        //TODO Improve complex handling with ReplaceIdAndResetChanges and RemapIdReferences
        internal async Task<(int created, int updated)> SaveChanges_TwoPhases(bool commit, bool impersonate, bool bypassrules, CancellationToken cancellationToken)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // The workitembatchupdate API has a huge limit:
            // it fails adding a relation between a new (id<0) work item and an existing one (id>0)

            var (createdWorkItems, updatedWorkItems, deletedWorkItems, restoredWorkItems) = _context.Tracker.GetChangedWorkItems();
            int createdCounter = createdWorkItems.Length;
            int updatedCounter = updatedWorkItems.Length + deletedWorkItems.Length + restoredWorkItems.Length;

            //TODO strange handling, better would be a redesign here: Add links as new Objects and do not create changes when they occur but when accessed to Changes property
            var batchRequests = new List<WitBatchRequest>();
            foreach (var item in createdWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request for a new {item.WorkItemType} workitem in {item.TeamProject}");

                //TODO HACK better something like this: _context.Tracker.NewWorkItems.Where(wi => !wi.Relations.HasAdds(toNewItems: true))
                var changesWithoutRelation = item.Changes
                                                 .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test)
                                                 // remove relations as we might incour in API failure
                                                 .Where(c => !string.Equals(c.Path, "/relations/-", StringComparison.Ordinal));
                var document = new JsonPatchDocument();
                document.AddRange(changesWithoutRelation);

                var request = _clients.WitClient.CreateWorkItemBatchRequest(_context.ProjectName,
                                                                            item.WorkItemType,
                                                                            document,
                                                                            bypassRules: impersonate || bypassrules,
                                                                            suppressNotifications: false);
                batchRequests.Add(request);
            }

            if (commit)
            {
                var batchResponses = await ExecuteBatchRequest(batchRequests, cancellationToken);

                UpdateIdsInRelations(batchResponses);

                await RestoreAndDelete(restoredWorkItems, deletedWorkItems, cancellationToken);
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
            }

            batchRequests.Clear();
            var allWorkItems = createdWorkItems.Concat(updatedWorkItems).Concat(restoredWorkItems);
            foreach (var item in allWorkItems)
            {
                var changes = item.Changes
                                  .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test);
                if (!changes.Any())
                {
                    continue;
                }
                _context.Logger.WriteInfo($"Found a request to update workitem {item.Id} in {_context.ProjectName}");
                _context.Logger.WriteVerbose(JsonConvert.SerializeObject(item.Changes));

                var request = _clients.WitClient.CreateWorkItemBatchRequest(item.Id,
                                                                            item.Changes,
                                                                            bypassRules: impersonate || bypassrules,
                                                                            suppressNotifications: false);
                batchRequests.Add(request);
            }

            if (commit)
            {
                _ = await ExecuteBatchRequest(batchRequests, cancellationToken);
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
            }

            return (createdCounter, updatedCounter);
        }

        private void UpdateIdsInRelations(IEnumerable<WitBatchResponse> batchResponses)
        {
            var (createdWorkItems, updatedWorkItems, deletedWorkItems, restoredWorkItems) = _context.Tracker.GetChangedWorkItems();
            var realIds = createdWorkItems
                                   // the response order matches the request order
                                   .Zip(batchResponses, (item, response) =>
                                   {
                                       int oldId = item.Id;
                                       var newId = response.ParseBody<WorkItem>().Id.Value;

                                       //TODO oldId should be known by item, and not needed to be passed as parameter
                                       item.ReplaceIdAndResetChanges(oldId, newId);
                                       return new { oldId, newId };
                                   })
                                   .ToDictionary(kvp => kvp.oldId, kvp => kvp.newId);

            foreach (var item in updatedWorkItems)
            {
                item.RemapIdReferences(realIds);
            }
        }
    }
}

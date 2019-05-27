using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.WebApi.Patch.Json;


namespace aggregator.Engine
{
    public class WorkItemStore
    {
        private readonly EngineContext _context;

        public WorkItemStore(EngineContext context)
        {
            _context = context;
        }

        public WorkItemWrapper GetWorkItem(int id)
        {
            _context.Logger.WriteVerbose($"Getting workitem {id}");

            return _context.Tracker.LoadWorkItem(id, (workItemId) =>
                {
                    _context.Logger.WriteInfo($"Loading workitem {workItemId}");
                    var item = _context.Client.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Result;
                    return new WorkItemWrapper(_context, item);
                });
        }

        public WorkItemWrapper GetWorkItem(WorkItemRelationWrapper item)
        {
            int id = int.Parse(
                item.Url.Substring(
                    item.Url.LastIndexOf('/') + 1));
            return GetWorkItem(id);
        }

        public IList<WorkItemWrapper> GetWorkItems(IEnumerable<int> ids)
        {
            _context.Logger.WriteVerbose($"Getting workitems {ids.ToSeparatedString()}");
            return _context.Tracker.LoadWorkItems(ids, (workItemIds) =>
            {
                _context.Logger.WriteInfo($"Loading workitems {workItemIds.ToSeparatedString()}");
                var items = _context.Client.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All).Result;
                return items.ConvertAll(i => new WorkItemWrapper(_context, i));
            });
        }

        public IList<WorkItemWrapper> GetWorkItems(IEnumerable<WorkItemRelationWrapper> collection)
        {
            var ids = new List<int>();
            foreach (var item in collection)
            {
                ids.Add(
                    int.Parse(
                        item.Url.Substring(
                            item.Url.LastIndexOf('/') + 1)));
            }

            return GetWorkItems(ids);
        }

        public WorkItemWrapper NewWorkItem(string workItemType, string projectName = null)
        {
            // TODO check workItemType and projectName values by querying AzDO
            var item = new WorkItem
            {
                Fields = new Dictionary<string, object>
                {
                    { CoreFieldRefNames.WorkItemType, workItemType },
                    { CoreFieldRefNames.TeamProject, projectName ?? _context.ProjectName }
                },
                Relations = new List<WorkItemRelation>(),
                Links = new Microsoft.VisualStudio.Services.WebApi.ReferenceLinks()
            };
            var wrapper = new WorkItemWrapper(_context, item);
            _context.Logger.WriteVerbose($"Made new workitem in {wrapper.TeamProject} with temporary id {wrapper.Id.Value}");
            //HACK
            string baseUriString = _context.Client.BaseAddress.AbsoluteUri;
            item.Url = FormattableString.Invariant($"{baseUriString}/_apis/wit/workitems/{wrapper.Id.Value}");
            return wrapper;
        }

        public async Task<(int created, int updated)> SaveChanges(SaveMode mode, bool commit, CancellationToken cancellationToken)
        {
            switch (mode)
            {
                case SaveMode.Default:
                    _context.Logger.WriteVerbose($"No save mode specified, assuming {SaveMode.TwoPhases}.");
                    goto case SaveMode.TwoPhases;
                case SaveMode.Item:
                    var resultItem = await SaveChanges_ByItem(commit, cancellationToken);
                    return resultItem;
                case SaveMode.Batch:
                    var resultBatch = await SaveChanges_Batch(commit, cancellationToken);
                    return resultBatch;
                case SaveMode.TwoPhases:
                    var resultTwoPhases = await SaveChanges_TwoPhases(commit, cancellationToken);
                    return resultTwoPhases;
                default:
                    throw new InvalidOperationException($"Unsupported save mode: {mode}.");
            }
        }

        private async Task<(int created, int updated)> SaveChanges_ByItem(bool commit, CancellationToken cancellationToken)
        {
            int created = 0;
            int updated = 0;
            foreach (var item in _context.Tracker.NewWorkItems)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Creating a {item.WorkItemType} workitem in {item.TeamProject}");
                    _ = await _context.Client.CreateWorkItemAsync(
                        item.Changes,
                        _context.ProjectName,
                        item.WorkItemType,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should create a {item.WorkItemType} workitem in {item.TeamProject}");
                }

                created++;
            }

            foreach (var item in _context.Tracker.ChangedWorkItems)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Updating workitem {item.Id}");
                    _ = await _context.Client.UpdateWorkItemAsync(
                        item.Changes,
                        item.Id.Value,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should update workitem {item.Id} in {item.TeamProject}");
                }

                updated++;
            }

            return (created, updated);
        }

        private async Task<(int created, int updated)> SaveChanges_Batch(bool commit, CancellationToken cancellationToken)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // BUG this code won't work if there is a relation between a new (id<0) work item and an existing one (id>0): it is an API limit

            int created = _context.Tracker.NewWorkItems.Count();
            int updated = _context.Tracker.ChangedWorkItems.Count();

            List<WitBatchRequest> batchRequests = new List<WitBatchRequest>();
            foreach (var item in _context.Tracker.NewWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request for a new {item.WorkItemType} workitem in {item.TeamProject}");

                var request = _context.Client.CreateWorkItemBatchRequest(_context.ProjectName,
                                                                         item.WorkItemType,
                                                                         item.Changes,
                                                                         bypassRules: false,
                                                                         suppressNotifications: false);
                batchRequests.Add(request);
            }

            foreach (var item in _context.Tracker.ChangedWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request to update workitem {item.Id.Value} in {item.TeamProject}");

                var request = _context.Client.CreateWorkItemBatchRequest(item.Id.Value,
                                                                         item.Changes,
                                                                         bypassRules: false,
                                                                         suppressNotifications: false);
                batchRequests.Add(request);
            }

            var converters = new JsonConverter[] { new JsonPatchOperationConverter() };
            string requestBody = JsonConvert.SerializeObject(batchRequests, Formatting.Indented, converters);
            _context.Logger.WriteVerbose(requestBody);

            if (commit)
            {
                var batchResponses = await _context.Client.ExecuteBatchRequest(batchRequests, cancellationToken: cancellationToken);
                var failedResponses = batchResponses.Where(batchResponse => !IsSuccessStatusCode(batchResponse.Code)).ToList();
                var hasFailures = failedResponses.Any();

                if (hasFailures)
                {
                    string stringResponse = JsonConvert.SerializeObject(batchResponses, Formatting.Indented);
                    _context.Logger.WriteVerbose(stringResponse);

                    foreach (var batchResponse in failedResponses)
                    {
                        _context.Logger.WriteError($"Save failed: {batchResponse.Body}");
                    }

                    throw new InvalidOperationException("Save failed.");
                }
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
            }//if

            return (created, updated);
        }
        private static bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }

        //TODO no error handling here? SaveChanges_Batch has at least the DryRun support and error handling
        //TODO Improve complex handling with ReplaceIdAndResetChanges and RemapIdReferences
        private async Task<(int created, int updated)> SaveChanges_TwoPhases(bool commit, CancellationToken cancellationToken)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // The workitembatchupdate API has a huge limit:
            // it fails adding a relation between a new (id<0) work item and an existing one (id>0)

            int created = _context.Tracker.NewWorkItems.Count();
            int updated = _context.Tracker.ChangedWorkItems.Count();

            //TODO strange handling, better would be a redesign here: Add links as new Objects and do not create changes when they occur but when accessed to Changes property
            var batchRequests = new List<WitBatchRequest>();
            foreach (var item in _context.Tracker.NewWorkItems)
            {
                _context.Logger.WriteInfo($"Found a request for a new {item.WorkItemType} workitem in {item.TeamProject}");

                //TODO HACK better something like this: _context.Tracker.NewWorkItems.Where(wi => !wi.Relations.HasAdds(toNewItems: true))
                var changesWithoutRelation = item.Changes
                                                 .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test)
                                                 // remove relations as we might incour in API failure
                                                 .Where(c => !string.Equals(c.Path, "/relations/-", StringComparison.Ordinal));
                var document = new JsonPatchDocument();
                document.AddRange(changesWithoutRelation);

                var request = _context.Client.CreateWorkItemBatchRequest(_context.ProjectName,
                                                                         item.WorkItemType,
                                                                         document,
                                                                         bypassRules: false,
                                                                         suppressNotifications: false);
                batchRequests.Add(request);
            }

            if (commit)
            {
                var batchResponses = await _context.Client.ExecuteBatchRequest(batchRequests, cancellationToken: cancellationToken);

                if (batchResponses.Any())
                {
                    UpdateIdsInRelations(batchResponses);
                }
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
            }

            batchRequests.Clear();
            var allWorkItems = _context.Tracker.NewWorkItems.Concat(_context.Tracker.ChangedWorkItems);
            foreach (var item in allWorkItems)
            {
                var changes = item.Changes
                                  .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test);
                if (!changes.Any())
                {
                    continue;
                }
                _context.Logger.WriteInfo($"Found a request to update workitem {item.Id.Value} in {_context.ProjectName}");

                var request = _context.Client.CreateWorkItemBatchRequest(item.Id.Value,
                                                                         item.Changes,
                                                                         bypassRules: false,
                                                                         suppressNotifications: false);
                batchRequests.Add(request);
            }

            if (commit)
            {
                await _context.Client.ExecuteBatchRequest(batchRequests, cancellationToken: cancellationToken);
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
            }

            return (created, updated);
        }

        private void UpdateIdsInRelations(IEnumerable<WitBatchResponse> batchResponses)
        {
            var realIds = _context.Tracker
                                  .NewWorkItems
                                  // the response order matches the request order
                                  .Zip(batchResponses, (item, response) =>
                                  {
                                      var oldId = item.Id.Value;
                                      var newId = response.ParseBody<WorkItem>().Id.Value;

                                      //TODO oldId should be known by item, and not needed to be passed as parameter
                                      item.ReplaceIdAndResetChanges(oldId, newId);
                                      return new {oldId, newId};
                                  })
                                  .ToDictionary(kvp => kvp.oldId, kvp => kvp.newId);

            foreach (var item in _context.Tracker.ChangedWorkItems)
            {
                item.RemapIdReferences(realIds);
            }
        }
    }
}
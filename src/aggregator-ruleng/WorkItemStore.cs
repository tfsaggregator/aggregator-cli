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


namespace aggregator.Engine
{
    public class WorkItemStore
    {
        private const int VS403474_LIMIT = 200;

        private readonly EngineContext _context;
        private readonly IClientsContext _clients;
        private readonly Lazy<Task<IEnumerable<WorkItemTypeCategory>>> _lazyGetWorkItemCategories;
        private readonly Lazy<Task<IEnumerable<BacklogWorkItemTypeStates>>> _lazyGetBacklogWorkItemTypesAndStates;

        private readonly IdentityRef _triggerIdentity;


        public WorkItemStore(EngineContext context)
        {
            _context = context;
            _clients = _context.Clients;
            _lazyGetWorkItemCategories = new Lazy<Task<IEnumerable<WorkItemTypeCategory>>>(async () => await GetWorkItemCategories_Internal());
            _lazyGetBacklogWorkItemTypesAndStates = new Lazy<Task<IEnumerable<BacklogWorkItemTypeStates>>>(async () => await GetBacklogWorkItemTypesAndStates_Internal());
        }

        public WorkItemStore(EngineContext context, WorkItem workItem) : this(context)
        {
            //initialize tracker with initial work item
            var wrapper = new WorkItemWrapper(_context, workItem);
            //store event initiator identity
            _triggerIdentity = wrapper.ChangedBy;
        }

        public WorkItemWrapper GetWorkItem(int id)
        {
            _context.Logger.WriteVerbose($"Getting workitem {id}");

            return _context.Tracker.LoadWorkItem(id, (workItemId) =>
                {
                    _context.Logger.WriteInfo($"Loading workitem {workItemId}");
                    var item = _clients.WitClient.GetWorkItemAsync(workItemId, expand: WorkItemExpand.All).Result;
                    return new WorkItemWrapper(_context, item);
                });
        }

        public WorkItemWrapper GetWorkItem(WorkItemRelationWrapper item)
        {
            return GetWorkItem(item.LinkedId);
        }

        public IList<WorkItemWrapper> GetWorkItems(IEnumerable<int> ids)
        {
            var accumulator = new List<WorkItemWrapper>();

            // prevent VS403474: You requested nnn work items which exceeds the limit of 200
            foreach (var idBlock in ids.Paginate(VS403474_LIMIT))
            {
                _context.Logger.WriteVerbose($"Getting workitems {idBlock.ToSeparatedString()}");
                var workItemBlock = _context.Tracker.LoadWorkItems(idBlock, (workItemIds) =>
                {
                    _context.Logger.WriteInfo($"Loading workitems {workItemIds.ToSeparatedString()}");
                    var items = _clients.WitClient.GetWorkItemsAsync(workItemIds, expand: WorkItemExpand.All).Result;
                    return items.ConvertAll(i => new WorkItemWrapper(_context, i));
                });

                accumulator.AddRange(workItemBlock);
            }

            return accumulator;
        }

        public IList<WorkItemWrapper> GetWorkItems(IEnumerable<WorkItemRelationWrapper> collection)
        {
            var ids = collection.Select<WorkItemRelationWrapper, int>(relation => relation.LinkedId);

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
            _context.Logger.WriteVerbose($"Made new workitem in {wrapper.TeamProject} with temporary id {wrapper.Id}");
            //HACK
            string baseUriString = _clients.WitClient.BaseAddress.AbsoluteUri;
            item.Url = FormattableString.Invariant($"{baseUriString}/_apis/wit/workitems/{wrapper.Id}");
            return wrapper;
        }

        public bool DeleteWorkItem(WorkItemWrapper workItem)
        {
            _context.Logger.WriteVerbose($"Mark workitem for Delete {workItem.Id}");

            return ChangeRecycleStatus(workItem, RecycleStatus.ToDelete);
        }

        public bool RestoreWorkItem(WorkItemWrapper workItem)
        {
            _context.Logger.WriteVerbose($"Mark workitem for Restire {workItem.Id}");

            return ChangeRecycleStatus(workItem, RecycleStatus.ToRestore);
        }

        private static bool ChangeRecycleStatus(WorkItemWrapper workItem, RecycleStatus toRecycleStatus)
        {
            if ((toRecycleStatus == RecycleStatus.ToDelete && workItem.IsDeleted) ||
                (toRecycleStatus == RecycleStatus.ToRestore && !workItem.IsDeleted))
            {
                return false;
            }

            var previousStatus = workItem.RecycleStatus;
            workItem.RecycleStatus = toRecycleStatus;

            var updated = previousStatus != workItem.RecycleStatus;
            workItem.IsDirty = updated || workItem.IsDirty;
            return updated;
        }

        public async Task<IEnumerable<WorkItemTypeCategory>> GetWorkItemCategories()
        {
            return await _lazyGetWorkItemCategories.Value;
        }

        public async Task<IEnumerable<BacklogWorkItemTypeStates>> GetBacklogWorkItemTypesAndStates()
        {
            return await _lazyGetBacklogWorkItemTypesAndStates.Value;
        }


        private void ImpersonateChanges()
        {
            var (created, updated, _, _) = _context.Tracker.GetChangedWorkItems();

            var changedWorkItems = created.Concat(updated);

            foreach (var workItem in changedWorkItems)
            {
                workItem.ChangedBy = _triggerIdentity;
            }
        }

        public async Task<(int created, int updated)> SaveChanges(SaveMode mode, bool commit, bool impersonate, CancellationToken cancellationToken)
        {
            if (impersonate)
            {
                ImpersonateChanges();
            }

            switch (mode)
            {
                case SaveMode.Default:
                    _context.Logger.WriteVerbose($"No save mode specified, assuming {SaveMode.TwoPhases}.");
                    goto case SaveMode.TwoPhases;
                case SaveMode.Item:
                    var resultItem = await SaveChanges_ByItem(commit, impersonate, cancellationToken);
                    return resultItem;
                case SaveMode.Batch:
                    var resultBatch = await SaveChanges_Batch(commit, impersonate, cancellationToken);
                    return resultBatch;
                case SaveMode.TwoPhases:
                    var resultTwoPhases = await SaveChanges_TwoPhases(commit, impersonate, cancellationToken);
                    return resultTwoPhases;
                default:
                    throw new InvalidOperationException($"Unsupported save mode: {mode}.");
            }
        }

        private async Task<(int created, int updated)> SaveChanges_ByItem(bool commit, bool impersonate, CancellationToken cancellationToken)
        {
            int created = 0;
            int updated = 0;

            var workItems = _context.Tracker.GetChangedWorkItems();
            foreach (var item in workItems.Created)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Creating a {item.WorkItemType} workitem in {item.TeamProject}");
                    _ = await _clients.WitClient.CreateWorkItemAsync(
                        item.Changes,
                        _context.ProjectName,
                        item.WorkItemType,
                        bypassRules: impersonate,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should create a {item.WorkItemType} workitem in {item.TeamProject}");
                }

                created++;
            }

            if (commit)
            {
                await RestoreAndDelete(workItems.Restored, workItems.Deleted, cancellationToken);
            }
            else if (workItems.Deleted.Any() || workItems.Restored.Any())
            {
                string FormatIds(WorkItemWrapper[] items) => string.Join(",", items.Select(item => item.Id));
                var teamProjectName = workItems.Restored.FirstOrDefault()?.TeamProject ??
                                      workItems.Deleted.FirstOrDefault()?.TeamProject;
                _context.Logger.WriteInfo($"Dry-run mode: should restore: {FormatIds(workItems.Restored)} and delete {FormatIds(workItems.Deleted)} workitems");
            }
            updated += workItems.Restored.Length + workItems.Deleted.Length;

            foreach (var item in workItems.Updated.Concat(workItems.Restored))
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Updating workitem {item.Id}");
                    _ = await _clients.WitClient.UpdateWorkItemAsync(
                        item.Changes,
                        item.Id,
                        bypassRules: impersonate,
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

        private async Task<(int created, int updated)> SaveChanges_Batch(bool commit, bool impersonate, CancellationToken cancellationToken)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // BUG this code won't work if there is a relation between a new (id<0) work item and an existing one (id>0): it is an API limit

            var workItems = _context.Tracker.GetChangedWorkItems();
            int created = workItems.Created.Length;
            int updated = workItems.Updated.Length + workItems.Deleted.Length + workItems.Restored.Length;

            List<WitBatchRequest> batchRequests = new List<WitBatchRequest>();
            foreach (var item in workItems.Created)
            {
                _context.Logger.WriteInfo($"Found a request for a new {item.WorkItemType} workitem in {item.TeamProject}");

                var request = _clients.WitClient.CreateWorkItemBatchRequest(_context.ProjectName,
                                                                            item.WorkItemType,
                                                                            item.Changes,
                                                                            bypassRules: impersonate,
                                                                            suppressNotifications: false);
                batchRequests.Add(request);
            }

            foreach (var item in workItems.Updated)
            {
                _context.Logger.WriteInfo($"Found a request to update workitem {item.Id} in {item.TeamProject}");

                var request = _clients.WitClient.CreateWorkItemBatchRequest(item.Id,
                                                                            item.Changes,
                                                                            bypassRules: impersonate,
                                                                            suppressNotifications: false);
                batchRequests.Add(request);
            }

            var converters = new JsonConverter[] { new JsonPatchOperationConverter() };
            string requestBody = JsonConvert.SerializeObject(batchRequests, Formatting.None, converters);
            _context.Logger.WriteVerbose(requestBody);

            if (commit)
            {
                _ = await ExecuteBatchRequest(batchRequests, cancellationToken);
                await RestoreAndDelete(workItems.Restored, workItems.Deleted, cancellationToken);
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
        private async Task<(int created, int updated)> SaveChanges_TwoPhases(bool commit, bool impersonate, CancellationToken cancellationToken)
        {
            // see https://github.com/redarrowlabs/vsts-restapi-samplecode/blob/master/VSTSRestApiSamples/WorkItemTracking/Batch.cs
            // and https://docs.microsoft.com/en-us/rest/api/vsts/wit/workitembatchupdate?view=vsts-rest-4.1
            // The workitembatchupdate API has a huge limit:
            // it fails adding a relation between a new (id<0) work item and an existing one (id>0)

            var workItems = _context.Tracker.GetChangedWorkItems();
            int created = workItems.Created.Length;
            int updated = workItems.Updated.Length + workItems.Deleted.Length + workItems.Restored.Length;

            //TODO strange handling, better would be a redesign here: Add links as new Objects and do not create changes when they occur but when accessed to Changes property
            var batchRequests = new List<WitBatchRequest>();
            foreach (var item in workItems.Created)
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
                                                                            bypassRules: impersonate,
                                                                            suppressNotifications: false);
                batchRequests.Add(request);
            }

            if (commit)
            {
                var batchResponses = await ExecuteBatchRequest(batchRequests, cancellationToken);

                UpdateIdsInRelations(batchResponses);

                await RestoreAndDelete(workItems.Restored, workItems.Deleted, cancellationToken);
            }
            else
            {
                _context.Logger.WriteWarning($"Dry-run mode: no updates sent to Azure DevOps.");
            }

            batchRequests.Clear();
            var allWorkItems = workItems.Created.Concat(workItems.Updated).Concat(workItems.Restored);
            foreach (var item in allWorkItems)
            {
                var changes = item.Changes
                                  .Where(c => c.Operation != Microsoft.VisualStudio.Services.WebApi.Patch.Operation.Test);
                if (!changes.Any())
                {
                    continue;
                }
                _context.Logger.WriteInfo($"Found a request to update workitem {item.Id} in {_context.ProjectName}");

                var request = _clients.WitClient.CreateWorkItemBatchRequest(item.Id,
                                                                            item.Changes,
                                                                            bypassRules: impersonate,
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

            return (created, updated);
        }

        private async Task<IEnumerable<WitBatchResponse>> ExecuteBatchRequest(IList<WitBatchRequest> batchRequests, CancellationToken cancellationToken)
        {
            if (!batchRequests.Any()) return Enumerable.Empty<WitBatchResponse>();

            var batchResponses = await _clients.WitClient.ExecuteBatchRequest(batchRequests, cancellationToken: cancellationToken);

            var failedResponses = batchResponses.Where(batchResponse => !IsSuccessStatusCode(batchResponse.Code)).ToList();
            foreach (var failedResponse in failedResponses)
            {
                string stringResponse = JsonConvert.SerializeObject(batchResponses, Formatting.None);
                _context.Logger.WriteVerbose(stringResponse);
                _context.Logger.WriteError($"Save failed: {failedResponse.Body}");
            }

            //TODO should we throw exception?
            //if (failedResponses.Any())
            //{
            //    throw new InvalidOperationException("Save failed.");
            //}

            return batchResponses;
        }


        private async Task RestoreAndDelete(IEnumerable<WorkItemWrapper> restore, IEnumerable<WorkItemWrapper> delete, CancellationToken cancellationToken = default)
        {
            foreach (var item in delete)
            {
                _context.Logger.WriteInfo($"Deleting workitem {item.Id} in {item.TeamProject}");
                _ = await _clients.WitClient.DeleteWorkItemAsync(item.Id, cancellationToken: cancellationToken);
            }

            foreach (var item in restore)
            {
                _context.Logger.WriteInfo($"Restoring workitem {item.Id} in {item.TeamProject}");
                _ = await _clients.WitClient.RestoreWorkItemAsync(new WorkItemDeleteUpdate() { IsDeleted = false }, item.Id, cancellationToken: cancellationToken);
            }
        }

        private void UpdateIdsInRelations(IEnumerable<WitBatchResponse> batchResponses)
        {
            var workItems = _context.Tracker.GetChangedWorkItems();
            var realIds = workItems.Created
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

            foreach (var item in workItems.Updated)
            {
                item.RemapIdReferences(realIds);
            }
        }

        private async Task<IEnumerable<WorkItemTypeCategory>> GetWorkItemCategories_Internal()
        {
            var workItemTypeCategories = await _clients.WitClient.GetWorkItemTypeCategoriesAsync(_context.ProjectName);
            var result = workItemTypeCategories.Select(workItemTypeCategory => new WorkItemTypeCategory()
            {
                ReferenceName = workItemTypeCategory.ReferenceName,
                Name = workItemTypeCategory.Name,
                WorkItemTypeNames = workItemTypeCategory.WorkItemTypes.Select(wiType => wiType.Name)
            })
                                               .ToList();

            return result;
        }

        private async Task<IEnumerable<BacklogWorkItemTypeStates>> GetBacklogWorkItemTypesAndStates_Internal()
        {
            var processConfiguration = await _clients.WorkClient.GetProcessConfigurationAsync(_context.ProjectName);
            var backlogWorkItemTypes = new List<CategoryConfiguration>(processConfiguration.PortfolioBacklogs)
                                       {
                                           processConfiguration.BugWorkItems,
                                           processConfiguration.RequirementBacklog,
                                           processConfiguration.TaskBacklog,
                                       };

            var workItemCategoryStates = new List<BacklogWorkItemTypeStates>();
            foreach (var backlog in backlogWorkItemTypes)
            {
                var backlogInfo = new BacklogInfo()
                {
                    Name = backlog.Name,
                    ReferenceName = backlog.ReferenceName
                };

                foreach (var workItemType in backlog.WorkItemTypes)
                {
                    var states = await _clients.WitClient.GetWorkItemTypeStatesAsync(_context.ProjectName, workItemType.Name);

                    var itemTypeStates = new BacklogWorkItemTypeStates()
                    {
                        Name = workItemType.Name,
                        Backlog = backlogInfo,
                        StateCategoryStateNames = states.ToLookup(state => state.Category)
                                                        .ToDictionary(kvp => kvp.Key,
                                                                      kvp => kvp.Select(state => state.Name)
                                                                                .ToArray())
                    };

                    workItemCategoryStates.Add(itemTypeStates);
                }

            }

            return workItemCategoryStates;
        }

    }

    public class WorkItemTypeCategory
    {
        /// <summary>
        /// Category ReferenceName, e.g. "Microsoft.EpicCategory"
        /// </summary>
        public string ReferenceName { get; set; }

        /// <summary>
        /// Display Name, e.g. "Epic Category"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// WorkItemTypes in this Category, e.g. "Epic" or "Test Plan"
        /// </summary>
        public IEnumerable<string> WorkItemTypeNames { get; set; }
    }

    public class BacklogWorkItemTypeStates
    {
        /// <summary>
        /// WorkItem Name, e.g. "Epic"
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Backlog Information this WorkItem Type is in
        /// </summary>
        public BacklogInfo Backlog { get; set; }

        /// <summary>
        /// Meta-State to WorkItem state name mapping, e.g.
        /// "InProgress" = "Active", "Resolved"
        /// "Proposed"   = "New"
        /// "Complete"   = "Closed"
        /// "Resolved"
        /// </summary>
        public IDictionary<string, string[]> StateCategoryStateNames { get; set; }
    }

    public class BacklogInfo
    {
        /// <summary>
        /// The Category Reference Name, e.g. "Microsoft.EpicCategory" or "Microsoft.RequirementCategory"
        /// </summary>
        public string ReferenceName { get; set; }
        /// <summary>
        /// The Backlog Level Display Name, e.g. "Epics" or "Stories"
        /// </summary>
        public string Name { get; set; }
    }

}

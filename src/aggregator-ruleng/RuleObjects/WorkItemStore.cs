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
                // emit JsonPatchOperation for this field even if the value is unchanged
                workItem.ChangedBy = null;
                workItem.ChangedBy = _triggerIdentity;
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Sonar Code Smell", "S907:\"goto\" statement should not be used")]
        public async Task<(int created, int updated)> SaveChanges(SaveMode mode, bool commit, bool impersonate, bool bypassrules, CancellationToken cancellationToken)
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
                    var saver = new Persistance.PersistByItem(_context);
                    var resultItem = await saver.SaveChanges_ByItem(commit, impersonate, bypassrules, cancellationToken);
                    return resultItem;
                case SaveMode.Batch:
                    var saver2 = new Persistance.PersistBatch(_context);
                    var resultBatch = await saver2.SaveChanges_Batch(commit, impersonate, bypassrules, cancellationToken);
                    return resultBatch;
                case SaveMode.TwoPhases:
                    var saver3 = new Persistance.PersistTwoPhases(_context);
                    var resultTwoPhases = await saver3.SaveChanges_TwoPhases(commit, impersonate, bypassrules, cancellationToken);
                    return resultTwoPhases;
                default:
                    throw new InvalidOperationException($"Unsupported save mode: {mode}.");
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

                foreach (var workItemTypeName in backlog.WorkItemTypes.Select(wt=>wt.Name))
                {
                    var states = await _clients.WitClient.GetWorkItemTypeStatesAsync(_context.ProjectName, workItemTypeName);

                    var itemTypeStates = new BacklogWorkItemTypeStates()
                    {
                        Name = workItemTypeName,
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

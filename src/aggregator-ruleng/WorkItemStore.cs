using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace aggregator.Engine
{
    public class WorkItemStore
    {
        private EngineContext _context;

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
            string idList = ids.Aggregate("", (s, i) => s += $",{i}");
            _context.Logger.WriteVerbose($"Getting workitems {idList.Substring(1)}");
            return _context.Tracker.LoadWorkItems(ids, (workItemIds) =>
            {
                string idList2 = workItemIds.Aggregate("", (s, i) => s += $",{i}");
                _context.Logger.WriteVerbose($"Loading workitems {idList2.Substring(1)}");
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

        public WorkItemWrapper NewWorkItem(string workItemType)
        {
            var item = new WorkItem()
            {
                Fields = new Dictionary<string, object>() {
                    { CoreFieldRefNames.WorkItemType, workItemType },
                    { CoreFieldRefNames.TeamProject, _context.ProjectId.ToString() }
                },
                Relations = new List<WorkItemRelation>(),
                Links = new Microsoft.VisualStudio.Services.WebApi.ReferenceLinks()
            };
            var wrapper = new WorkItemWrapper(_context, item);
            _context.Logger.WriteVerbose($"Made new workitem with temporary id {wrapper.Id.Value}");
            return wrapper;
        }

        public async Task<(int created, int updated)> SaveChanges(bool commit)
        {
            int created = 0;
            int updated = 0;
            foreach (var item in _context.Tracker.NewWorkItems)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Creating a {item.WorkItemType} workitem in {_context.ProjectId}");
                    var wi = await _context.Client.CreateWorkItemAsync(
                        item.Changes,
                        _context.ProjectId,
                        item.WorkItemType
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should create a {item.WorkItemType} workitem in {_context.ProjectId}");
                }
                created++;
            }

            foreach (var item in _context.Tracker.ChangedWorkItems)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Updating workitem {item.Id}");
                    var wi = await _context.Client.UpdateWorkItemAsync(
                        item.Changes,
                        item.Id.Value
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should update workitem {item.Id} in {_context.ProjectId}");
                }
                updated++;
            }
            return (created, updated);
        }
    }
}

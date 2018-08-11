using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;

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
            _context.Logger.WriteInfo($"Loading workitem {id}");

            var item = _context.Client.GetWorkItemAsync(id, expand: WorkItemExpand.All).Result;
            return new WorkItemWrapper(_context, item);
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
            _context.Logger.WriteInfo($"Loading workitems {idList.Substring(1)}");

            var items = _context.Client.GetWorkItemsAsync(ids).Result;
            return items.ConvertAll(i => new WorkItemWrapper(_context, i));
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
    }
}

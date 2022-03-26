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
    internal class PersistByItem : PersisterBase
    {
        public PersistByItem(EngineContext context)
            : base(context) { }

        internal async Task<(int created, int updated)> SaveChanges_ByItem(bool commit, bool impersonate, bool bypassrules, CancellationToken cancellationToken)
        {
            int createdCounter = 0;
            int updatedCounter = 0;

            var (createdWorkItems, updatedWorkItems, deletedWorkItems, restoredWorkItems) = _context.Tracker.GetChangedWorkItems();
            foreach (var item in createdWorkItems)
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Creating a {item.WorkItemType} workitem in {item.TeamProject}");
                    _ = await _clients.WitClient.CreateWorkItemAsync(
                        item.Changes,
                        _context.ProjectName,
                        item.WorkItemType,
                        bypassRules: impersonate || bypassrules,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should create a {item.WorkItemType} workitem in {item.TeamProject}");
                }

                createdCounter++;
            }

            if (commit)
            {
                await RestoreAndDelete(restoredWorkItems, deletedWorkItems, cancellationToken);
            }
            else if (deletedWorkItems.Any() || restoredWorkItems.Any())
            {
                static string FormatIds(WorkItemWrapper[] items) => string.Join(",", items.Select(item => item.Id));
                var teamProjectName = restoredWorkItems.FirstOrDefault()?.TeamProject ??
                                      deletedWorkItems.FirstOrDefault()?.TeamProject;
                _context.Logger.WriteInfo($"Dry-run mode: should restore: {FormatIds(restoredWorkItems)} and delete {FormatIds(deletedWorkItems)} workitems from {teamProjectName}");
            }
            updatedCounter += restoredWorkItems.Length + deletedWorkItems.Length;

            foreach (var item in updatedWorkItems.Concat(restoredWorkItems))
            {
                if (commit)
                {
                    _context.Logger.WriteInfo($"Updating workitem {item.Id}");
                    _ = await _clients.WitClient.UpdateWorkItemAsync(
                        item.Changes,
                        item.Id,
                        bypassRules: impersonate || bypassrules,
                        cancellationToken: cancellationToken
                    );
                }
                else
                {
                    _context.Logger.WriteInfo($"Dry-run mode: should update workitem {item.Id} in {item.TeamProject}");
                }

                updatedCounter++;
            }

            return (createdCounter, updatedCounter);
        }
    }
}

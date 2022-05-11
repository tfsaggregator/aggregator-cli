using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace aggregator.Engine.Persistance
{
    internal class PersistStateChange : PersisterBase
    {
        public PersistStateChange(EngineContext context)
            : base(context) { }

        internal async Task<bool> PersistAsync(WorkItemWrapper item, string comment, bool commit, bool impersonate, bool bypassrules, CancellationToken cancellationToken)
        {
            if (commit)
            {
                var payload = new JsonPatchDocument
                {
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Replace,
                        Path = "/fields/" + CoreFieldRefNames.State,
                        Value = item.State
                    },
                    new JsonPatchOperation()
                    {
                        Operation = Operation.Add,
                        Path = "/fields/" + CoreFieldRefNames.History,
                        Value = comment
                    }
                };
                _context.Logger.WriteInfo($"Updating workitem {item.Id}");
                _ = await _clients.WitClient.UpdateWorkItemAsync(
                    payload,
                    item.Id,
                    bypassRules: impersonate || bypassrules,
                    cancellationToken: cancellationToken
                );
            }
            else
            {
                _context.Logger.WriteInfo($"Dry-run mode: should update workitem {item.Id} in {item.TeamProject}");
            }

            return true;
        }
    }
}

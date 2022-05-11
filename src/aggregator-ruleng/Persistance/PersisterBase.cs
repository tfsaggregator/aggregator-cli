using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Newtonsoft.Json;

namespace aggregator.Engine.Persistance
{
    internal class PersisterBase
    {
        protected readonly EngineContext _context;
        protected readonly IClientsContext _clients;

        protected PersisterBase(EngineContext context)
        {
            _context = context;
            _clients = _context.Clients;
        }

        protected async Task RestoreAndDelete(IEnumerable<WorkItemWrapper> restore, IEnumerable<WorkItemWrapper> delete, CancellationToken cancellationToken = default)
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

        protected async Task<IEnumerable<WitBatchResponse>> ExecuteBatchRequest(IList<WitBatchRequest> batchRequests, CancellationToken cancellationToken)
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
#pragma warning disable S125 // Sections of code should not be commented out
            //if (failedResponses.Any())
            //{
            //    throw new InvalidOperationException("Save failed.");
            //}
#pragma warning restore S125 // Sections of code should not be commented out
            return batchResponses;
        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return (statusCode >= 200) && (statusCode <= 299);
        }
    }
}

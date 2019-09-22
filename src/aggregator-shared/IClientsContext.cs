using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;

namespace aggregator
{
    public interface IClientsContext
    {
        WorkItemTrackingHttpClient WitClient { get; }
        WorkHttpClient WorkClient { get; }
    }
}
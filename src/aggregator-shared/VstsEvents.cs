using System.Linq;
using Microsoft.VisualStudio.Services.ServiceHooks.WebApi;

namespace aggregator
{
    /// <summary>
    /// This class tracks the VSTS/AzureDevOps Events exposed both in CLI and Rules
    /// </summary>
    public static class DevOpsEvents
    {
        // TODO this table should be visible in the help
        static readonly string[] validValues = new[] {
            ServiceHooksEventTypeConstants.WorkItemCreated,
            ServiceHooksEventTypeConstants.WorkItemDeleted,
            ServiceHooksEventTypeConstants.WorkItemRestored,
            ServiceHooksEventTypeConstants.WorkItemUpdated,
            ServiceHooksEventTypeConstants.WorkItemCommented,
        };

        public static bool IsValidEvent(string @event)
        {
            return validValues.Contains(@event);
        }

        public static string PublisherId => ServiceHooksWebApiConstants.TfsPublisherId;
    }
}

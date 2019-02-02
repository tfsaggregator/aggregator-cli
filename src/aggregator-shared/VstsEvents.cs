using System.Linq;

namespace aggregator
{
    /// <summary>
    /// This class tracks the VSTS/AzureDevOps Events exposed both in CLI and Rules
    /// </summary>
    public static class DevOpsEvents
    {
        // TODO this table should be visible in the help
        static string[] validValues = new[] {
            "workitem.created",
            "workitem.deleted",
            "workitem.restored",
            "workitem.updated",
            "workitem.commented"
        };

        public static bool IsValidEvent(string @event)
        {
            return validValues.Contains(@event);
        }

        public static string PublisherId => "tfs";
    }
}

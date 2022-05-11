using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;
using Microsoft.VisualStudio.Services.Common;


namespace aggregator.Engine
{
    public static class WorkItemExtension
    {
        public static string GetTeamProject(this WorkItem workItem)
        {
            return workItem.Fields.GetCastedValueOrDefault("System.TeamProject", default(string));
        }
    }
}

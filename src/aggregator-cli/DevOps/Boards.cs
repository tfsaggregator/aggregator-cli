using System.Threading;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.VisualStudio.Services.WebApi.Patch;
using Microsoft.VisualStudio.Services.WebApi.Patch.Json;

namespace aggregator.cli
{
    internal class Boards
    {
        private readonly VssConnection devops;
        private readonly ILogger logger;

        public Boards(VssConnection devops, ILogger logger)
        {
            this.devops = devops;
            this.logger = logger;
        }


        internal async Task<int> CreateWorkItemAsync(string projectName, string title, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Reading Azure DevOps project data...");
            var projectClient = devops.GetClient<ProjectHttpClient>();
            var project = await projectClient.GetProject(projectName);
            logger.WriteInfo($"Project {projectName} data read.");

            var witClient = devops.GetClient<WorkItemTrackingHttpClient>();
            JsonPatchDocument patchDocument = new JsonPatchDocument();

            patchDocument.Add(
                new JsonPatchOperation()
                {
                    Operation = Operation.Add,
                    Path = "/fields/System.Title",
                    Value = title
                }
            );

            logger.WriteVerbose($"Creating work item '{title}' in '{project.Name}'");
            var newWorkItem = await witClient.CreateWorkItemAsync(patchDocument, project.Id, "Task", cancellationToken: cancellationToken);
            logger.WriteInfo($"Created work item ID {newWorkItem.Id} '{newWorkItem.Fields["System.Title"]}' in '{project.Name}'");

            return newWorkItem.Id.HasValue ? newWorkItem.Id.Value : 0;
        }

    }
}

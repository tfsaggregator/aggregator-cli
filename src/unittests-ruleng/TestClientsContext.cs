using System;
using aggregator;
using Microsoft.TeamFoundation.Work.WebApi;
using Microsoft.TeamFoundation.WorkItemTracking.WebApi;
using NSubstitute;

namespace unittests_ruleng
{
    class TestClientsContext : IClientsContext
    {
        public const string DEFAULT_COLLECTION_URL = "https://dev.azure.com/fake-organization";
        public const string DEFAULT_PROJECT_NAME = "test-project";

        public TestClientsContext(string collectionUrl = DEFAULT_COLLECTION_URL , string projectName = DEFAULT_PROJECT_NAME)
        {
            CollectionUrl = collectionUrl;
            ProjectName = projectName;
            ProjectId = Guid.NewGuid();

            WitClient = Substitute.For<WorkItemTrackingHttpClient>(new Uri(CollectionUrl), null);
            WorkClient = Substitute.For<WorkHttpClient>(new Uri(CollectionUrl), null);

            WitApiBaseUrl = $"{CollectionUrl}/{ProjectName}/_apis/wit";
            WorkItemsBaseUrl = $"{WitApiBaseUrl}/workItems";
            RecycleBinBaseUrl = $"{WitApiBaseUrl}/recyclebin";

        }

        public string CollectionUrl { get; }
        public string ProjectName { get; }
        public Guid ProjectId { get; set; }

        public string WitApiBaseUrl { get; }
        public string WorkItemsBaseUrl { get; }
        public string RecycleBinBaseUrl { get; }

        public WorkItemTrackingHttpClient WitClient { get; set; }
        public WorkHttpClient WorkClient { get; set; }
    }
}
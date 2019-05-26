using System.IO;

namespace integrationtests.cli
{
    public class TestLogonData
    {
        public TestLogonData(string filename)
        {
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(filename));

            Subscription = data.subscription;
            ClientId = data.client;
            ClientPassword = data.password;
            Tenant=data.tenant;

            Location = data.location;
            ResourceGroup = data.resourceGroup;

            DevOpsUrl = data.devopsUrl;
            ProjectName = data.projectName;
            PAT = data.pat;
        }

        // Azure Service Principal
        public string Subscription { get; private set; }
        public string ClientId { get; private set; }
        public string ClientPassword { get; private set; }
        public string Tenant { get; private set; }
        // Azure Resources
        public string Location { get; private set; }
        public string ResourceGroup { get; private set; }
        // Azure DevOps
        public string DevOpsUrl { get; private set; }
        public string ProjectName { get; private set; }
        public string PAT { get; private set; }
    }
}

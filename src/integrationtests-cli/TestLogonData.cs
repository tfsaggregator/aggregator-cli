using System;
using System.IO;
using System.Linq;

namespace integrationtests.cli
{
    public class TestLogonData
    {
        public TestLogonData(string filename)
        {
            dynamic data = Newtonsoft.Json.JsonConvert.DeserializeObject(File.ReadAllText(filename));

            SubscriptionId = data.subscription;
            ClientId = data.client;
            ClientSecret = data.password;
            TenantId = data.tenant;

            Location = data.location;
            ResourceGroup = data.resourceGroup;

            DevOpsUrl = data.devopsUrl;
            ProjectName = data.projectName;
            PAT = data.pat;

            string uniqueSuffix = data.uniqueSuffix;

            UniqueSuffix = string.IsNullOrEmpty(uniqueSuffix) ? GetRandomString(8) : uniqueSuffix;

            RuntimeSourceUrl = data.runtimeSourceUrl;
        }

        private static string GetRandomString(int size, string allowedChars = "abcdefghijklmnopqrstuvwxyz0123456789")
        {
            var randomGen = new Random((int)DateTime.Now.Ticks);
            return new string(
                Enumerable.Range(0, size)
                .Select(x => allowedChars[randomGen.Next(0, allowedChars.Length)])
                .ToArray()
                );
        }

        // Azure Service Principal
        public string SubscriptionId { get; }
        public string ClientId { get; }
        public string ClientSecret { get; }
        public string TenantId { get; }
        // Azure Resources
        public string Location { get; }
        public string ResourceGroup { get; }
        public string UniqueSuffix { get; }
        // Azure DevOps
        public string DevOpsUrl { get; }
        public string ProjectName { get; }
        public string PAT { get; }
        // Local data
        public string RuntimeSourceUrl { get; }
    }
}

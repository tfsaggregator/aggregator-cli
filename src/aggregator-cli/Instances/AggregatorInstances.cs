using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace aggregator.cli
{
    class AggregatorInstances
    {
        const string InstancePrefix = "aggregator-";
        private readonly IAzure azure;

        public class ProgressEventArgs
        {
            public ProgressEventArgs(string s) { Message = s; }
            public String Message { get; private set; } // readonly
        }

        // Declare the delegate (if using non-generic pattern).
        public delegate void ProgressHandler(object sender, ProgressEventArgs e);

        // Declare the event.
        public event ProgressHandler Progress;

        public AggregatorInstances(IAzure azure)
        {
            this.azure = azure;
        }

        public async Task<IEnumerable<(string name, string region)>> ListAsync()
        {
            var rgs = await azure.ResourceGroups.ListAsync();
            var result = new List<(string name, string region)>();
            foreach (var rg in rgs.Where(rg => rg.Name.StartsWith(InstancePrefix)))
            {
                result.Add((
                    rg.Name.Remove(0, InstancePrefix.Length),
                    rg.RegionName)
                );
            }
            return result;
        }

        internal async Task<bool> Add(string name, string location)
        {
            string rgName = GetResourceGroupName(name);
            if (!await azure.ResourceGroups.ContainAsync(rgName))
            {
                Progress?.Invoke(this, new ProgressEventArgs($"Creating resource group {rgName}"));
                await azure.ResourceGroups
                    .Define(rgName)
                    .WithRegion(location)
                    .CreateAsync();
            }

            // TODO the template must create a Storage account and/or a Key Vault
            var resourceName = "aggregator.cli.Instances.instance-template.json";
            string armTemplateString;
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                armTemplateString = await reader.ReadToEndAsync();
            }

            var parsedTemplate = JObject.Parse(armTemplateString);
            // sanity checks
            if (parsedTemplate.SelectToken("parameters.appName") == null)
            {
                // not good, blah
            }

            var templateParams = new Dictionary<string, Dictionary<string, object>>{
                    {"appName", new Dictionary<string, object>{{"value", name } }}
            };

            string deploymentName = SdkContext.RandomResourceName("aggregator", 24);
            Progress?.Invoke(this, new ProgressEventArgs($"Started deployment {deploymentName}"));
            var deployment = await azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(rgName)
                    .WithTemplate(armTemplateString)
                    .WithParameters(templateParams)
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync();

            // poll
            const int PollIntervalInSeconds = 10;
            int totalDelay = 0;
            while (!(StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Succeeded") ||
                    StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Failed") ||
                    StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Cancelled")))
            {
                SdkContext.DelayProvider.Delay(PollIntervalInSeconds * 1000);
                totalDelay += PollIntervalInSeconds;
                Progress?.Invoke(this, new ProgressEventArgs($"Deployment running ({totalDelay}s)"));
                await deployment.RefreshAsync();
            }
            Progress?.Invoke(this, new ProgressEventArgs($"Deployment {deployment.ProvisioningState}"));
            return true;
        }

        private static string GetResourceGroupName(string instanceName)
        {
            return InstancePrefix + instanceName;
        }

        internal (string username, string password) GetPublishCredentials(string instance)
        {
            var webFunctionApp = azure.AppServices.FunctionApps.GetByResourceGroup(GetResourceGroupName(instance), instance);
            var ftpUsername = webFunctionApp.GetPublishingProfile().FtpUsername;
            var username = ftpUsername.Split('\\').ToList()[1];
            var password = webFunctionApp.GetPublishingProfile().FtpPassword;
            // TODO this should be cached
            return (username: username, password: password);
        }
    }
}

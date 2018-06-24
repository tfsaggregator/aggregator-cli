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

        public IEnumerable<(string name, string region)> List()
        {
            var rgs = azure.ResourceGroups.List().Where(rg => rg.Name.StartsWith(InstancePrefix));
            foreach (var rg in rgs)
            {
                yield return (
                    rg.Name.Remove(0, InstancePrefix.Length),
                    rg.RegionName
                );
            }
        }

        internal bool Add(string name, string location)
        {
            string rgName = GetResourceGroupName(name);
            if (!azure.ResourceGroups.Contain(rgName))
            {
                Progress?.Invoke(this, new ProgressEventArgs($"Creating resource group {rgName}"));
                azure.ResourceGroups
                    .Define(rgName)
                    .WithRegion(location)
                    .Create();
            }

            // TODO the template must create a Storage account and/or a Key Vault
            var resourceName = "aggregator.cli.Instances.instance-template.json";
            string armTemplateString;
            var assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                armTemplateString = reader.ReadToEnd();
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
            azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(rgName)
                    .WithTemplate(armTemplateString)
                    .WithParameters(templateParams)
                    .WithMode(DeploymentMode.Incremental)
                    .BeginCreate();

            // poll
            const int PollIntervalInSeconds = 10;
            int totalDelay = 0;
            var deployment = azure.Deployments.GetByResourceGroup(rgName, deploymentName);
            while (!(StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Succeeded") ||
                    StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Failed") ||
                    StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Cancelled")))
            {
                SdkContext.DelayProvider.Delay(PollIntervalInSeconds * 1000);
                totalDelay += PollIntervalInSeconds;
                Progress?.Invoke(this, new ProgressEventArgs($"Deployment running ({totalDelay}s)"));
                deployment = azure.Deployments.GetByResourceGroup(rgName, deploymentName);
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

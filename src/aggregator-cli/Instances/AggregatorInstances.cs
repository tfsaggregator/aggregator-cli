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
        private readonly ILogger logger;

        public AggregatorInstances(IAzure azure, ILogger logger)
        {
            this.azure = azure;
            this.logger = logger;
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
                logger.WriteVerbose($"Creating resource group {rgName}");
                await azure.ResourceGroups
                    .Define(rgName)
                    .WithRegion(location)
                    .CreateAsync();
                logger.WriteInfo($"Resource group {rgName} created.");
            }

            // TODO the template should create a Storage account and/or a Key Vault
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
            logger.WriteInfo($"Started deployment {deploymentName}");
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
                logger.WriteVerbose($"Deployment running ({totalDelay}s)");
                await deployment.RefreshAsync();
            }
            logger.WriteInfo($"Deployment {deployment.ProvisioningState}");
            return true;
        }

        internal static string GetResourceGroupName(string instanceName)
        {
            return InstancePrefix + instanceName;
        }

        string lastPublishCredentialsInstance = string.Empty;
        internal async Task<(string username, string password)> GetPublishCredentials(string instance)
        {
            var webFunctionApp = await azure.AppServices.FunctionApps.GetByResourceGroupAsync(GetResourceGroupName(instance), instance);
            var ftpUsername = webFunctionApp.GetPublishingProfile().FtpUsername;
            var username = ftpUsername.Split('\\').ToList()[1];
            var password = webFunctionApp.GetPublishingProfile().FtpPassword;
            // TODO this should be cached
            return (username: username, password: password);
        }

        internal async Task<AuthenticationHeaderValue> GetKuduAuthentication(string instance)
        {
            (string username, string password) = await GetPublishCredentials(instance);
            var base64Auth = Convert.ToBase64String(Encoding.Default.GetBytes($"{username}:{password}"));
            return new AuthenticationHeaderValue("Basic", base64Auth);
        }

        internal async Task<string> GetAzureFunctionJWTAsync(string instance)
        {
            var kuduUrl = $"https://{instance}.scm.azurewebsites.net/api";
            string JWT;
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
                client.DefaultRequestHeaders.Authorization = await GetKuduAuthentication(instance);

                var result = await client.GetAsync($"{kuduUrl}/functions/admin/token");
                JWT = await result.Content.ReadAsStringAsync(); //get  JWT for call function key
                JWT = JWT.Trim('"');
            }
            return JWT;
        }

        internal async Task<bool> Remove(string name, string location)
        {
            string rgName = GetResourceGroupName(name);
            if (await azure.ResourceGroups.ContainAsync(rgName))
            {
                await azure.ResourceGroups.DeleteByNameAsync(rgName);
            }
            return true;
        }

        internal async Task<HttpRequestMessage> GetKuduRequestAsync(string instance, HttpMethod method, string restApi)
        {
            var kuduUrl = new Uri($"https://{instance}.scm.azurewebsites.net");
            var request = new HttpRequestMessage(method, $"{kuduUrl}/{restApi}");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
            request.Headers.Authorization = await GetKuduAuthentication(instance);
            return request;
        }
    }
}

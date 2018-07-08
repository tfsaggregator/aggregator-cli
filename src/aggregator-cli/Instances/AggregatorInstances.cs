using Microsoft.Azure.Management.AppService.Fluent;
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
        const string ResourceGroupInstancePrefix = "aggregator-";
        const string FunctionAppInstanceSuffix = "aggregator";
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
            foreach (var rg in rgs.Where(rg => rg.Name.StartsWith(ResourceGroupInstancePrefix)))
            {
                result.Add((
                    rg.Name.Remove(0, ResourceGroupInstancePrefix.Length),
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
                logger.WriteWarning($"Something is wrong with the ARM template");
            }

            string appName = GetFunctionAppName(name);
            var templateParams = new Dictionary<string, Dictionary<string, object>>{
                    {"appName", new Dictionary<string, object>{{"value", appName } }}
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
            const int PollIntervalInSeconds = 3;
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

            // upload
            logger.WriteVerbose($"Uploading runtime package to {name}");
            string zipPath = "function-bin.zip";
            var zipContent = File.ReadAllBytes(zipPath);
            bool ok = await UploadRuntimeZip(name, zipContent);
            if (ok)
            {
                logger.WriteInfo($"Runtime package uploaded to {name}.");
                // TODO requires VSTS logon!!!!!!!!!
                var vstsLogonData = VstsLogon.Load();
                if (vstsLogonData.Mode == VstsLogonMode.PAT)
                {
                    ok = await ChangeAppSettings(name, vstsLogonData.Token, vstsLogonData.Mode.ToString());
                }
                else
                {
                    return false;
                }

            }
            return ok;
        }

        private async Task<bool> UploadRuntimeZip(string name, byte[] zipContent)
        {
            // POST /api/zipdeploy?isAsync=true
            // Deploy from zip asynchronously. The Location header of the response will contain a link to a pollable deployment status.
            var body = new ByteArrayContent(zipContent);
            using (var client = new HttpClient())
            using (var request = await GetKuduRequestAsync(name, HttpMethod.Post, $"api/zipdeploy"))
            {
                request.Content = body;
                using (var response = await client.SendAsync(request))
                {
                    bool ok = response.IsSuccessStatusCode;
                    if (!ok)
                    {
                        logger.WriteError($"Upload failed with {response.ReasonPhrase}");
                    }
                    return ok;
                }
            }
        }

        internal static string GetResourceGroupName(string instanceName)
        {
            return ResourceGroupInstancePrefix + instanceName;
        }

        internal static string GetFunctionAppName(string instanceName)
        {
            return instanceName + FunctionAppInstanceSuffix;
        }

        internal static string GetFunctionAppHostName(string instanceName)
        {
            return $"{AggregatorInstances.GetFunctionAppName(instanceName)}.azurewebsites.net";
        }

        internal static string GetFunctionAppUrl(string instanceName)
        {
            return $"https://{AggregatorInstances.GetFunctionAppHostName(instanceName)}";
        }

        internal static string GetFunctionAppKuduUrl(string instanceName)
        {
            return $"https://{AggregatorInstances.GetFunctionAppName(instanceName)}.scm.azurewebsites.net";
        }


        string lastPublishCredentialsInstance = string.Empty;
        (string username, string password) lastPublishCredentials = default;
        internal async Task<(string username, string password)> GetPublishCredentials(string instance)
        {
            if (lastPublishCredentialsInstance != instance)
            {
                string rg = GetResourceGroupName(instance);
                string fn = GetFunctionAppName(instance);
                var webFunctionApp = await azure.AppServices.FunctionApps.GetByResourceGroupAsync(rg, fn);
                var ftpUsername = webFunctionApp.GetPublishingProfile().FtpUsername;
                var username = ftpUsername.Split('\\').ToList()[1];
                var password = webFunctionApp.GetPublishingProfile().FtpPassword;

                lastPublishCredentials = (username, password);
            }
            return lastPublishCredentials;
        }

        internal async Task<bool> ChangeAppSettings(string instance, string vstsToken, string vstsTokenType)
        {
            var webFunctionApp = await azure
                .AppServices
                .WebApps
                .GetByResourceGroupAsync(
                    GetResourceGroupName(instance),
                    GetFunctionAppName(instance));
            webFunctionApp
                .Update()
                .WithAppSetting("Aggregator_VstsTokenType", vstsTokenType)
                .WithAppSetting("Aggregator_VstsToken", vstsToken)
                .Apply();
            return true;
        }

        internal async Task<AuthenticationHeaderValue> GetKuduAuthentication(string instance)
        {
            (string username, string password) = await GetPublishCredentials(instance);
            var base64Auth = Convert.ToBase64String(Encoding.Default.GetBytes($"{username}:{password}"));
            return new AuthenticationHeaderValue("Basic", base64Auth);
        }

        internal async Task<string> GetAzureFunctionJWTAsync(string instance)
        {
            var kuduUrl = $"{GetFunctionAppKuduUrl(instance)}/api";
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
                logger.WriteVerbose($"Deleting resource group {rgName}");
                await azure.ResourceGroups.DeleteByNameAsync(rgName);
                logger.WriteInfo($"Resource group {rgName} deleted.");
            }
            return true;
        }

        internal async Task<HttpRequestMessage> GetKuduRequestAsync(string instance, HttpMethod method, string restApi)
        {
            var kuduUrl = new Uri(GetFunctionAppKuduUrl(instance));
            var request = new HttpRequestMessage(method, $"{kuduUrl}{restApi}");
            request.Headers.UserAgent.Add(new ProductInfoHeaderValue("aggregator", "3.0"));
            request.Headers.Authorization = await GetKuduAuthentication(instance);
            return request;
        }
    }
}

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
            foreach (var rg in rgs.Where(rg => rg.Name.StartsWith(InstanceName.ResourceGroupInstancePrefix)))
            {
                result.Add((
                    rg.Name.Remove(0, InstanceName.ResourceGroupInstancePrefix.Length),
                    rg.RegionName)
                );
            }
            return result;
        }

        internal async Task<bool> Add(InstanceName instance, string location)
        {
            string rgName = instance.ResourceGroupName;
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

            string appName = instance.FunctionAppName;
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
            logger.WriteVerbose($"Uploading runtime package to {instance.DnsHostName}");
            string zipPath = "function-bin.zip";
            var zipContent = File.ReadAllBytes(zipPath);
            bool ok = await UploadRuntimeZip(instance, zipContent);
            if (ok)
            {
                logger.WriteInfo($"Runtime package uploaded to {instance.PlainName}.");
                // TODO requires VSTS logon!!!!!!!!!
                var vstsLogonData = VstsLogon.Load();
                if (vstsLogonData.Mode == VstsLogonMode.PAT)
                {
                    ok = await ChangeAppSettings(instance, vstsLogonData.Token, vstsLogonData.Mode.ToString());
                }
                else
                {
                    return false;
                }

            }
            return ok;
        }

        private async Task<bool> UploadRuntimeZip(InstanceName instance, byte[] zipContent)
        {
            var kudu = new KuduApi(instance, azure, logger);
            // POST /api/zipdeploy?isAsync=true
            // Deploy from zip asynchronously. The Location header of the response will contain a link to a pollable deployment status.
            var body = new ByteArrayContent(zipContent);
            using (var client = new HttpClient())
            using (var request = await kudu.GetRequestAsync(HttpMethod.Post, $"api/zipdeploy"))
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

        internal async Task<bool> ChangeAppSettings(InstanceName instance, string vstsToken, string vstsTokenType)
        {
            var webFunctionApp = await azure
                .AppServices
                .WebApps
                .GetByResourceGroupAsync(
                    instance.ResourceGroupName,
                    instance.FunctionAppName);
            webFunctionApp
                .Update()
                .WithAppSetting("Aggregator_VstsTokenType", vstsTokenType)
                .WithAppSetting("Aggregator_VstsToken", vstsToken)
                .Apply();
            return true;
        }

        internal async Task<bool> Remove(InstanceName instance, string location)
        {
            string rgName = instance.ResourceGroupName;
            if (await azure.ResourceGroups.ContainAsync(rgName))
            {
                logger.WriteVerbose($"Deleting resource group {rgName}");
                await azure.ResourceGroups.DeleteByNameAsync(rgName);
                logger.WriteInfo($"Resource group {rgName} deleted.");
            }
            return true;
        }
    }
}

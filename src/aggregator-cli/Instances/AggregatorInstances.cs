using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Management.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent;
using Microsoft.Azure.Management.ResourceManager.Fluent.Models;
using Newtonsoft.Json.Linq;

namespace aggregator.cli
{
    class AggregatorInstances : AzureBaseClass
    {
        private readonly INamingTemplates naming;

        public AggregatorInstances(IAzure azure, ILogger logger, INamingTemplates naming)
            : base(azure, logger)
        {
            this.naming = naming;
        }

        public async Task<IEnumerable<ILogDataObject>> ListAllAsync(CancellationToken cancellationToken)
        {
            var runtime = new FunctionRuntimePackage(logger);
            var rgs = await azure.ResourceGroups.ListAsync(cancellationToken: cancellationToken);
            var filter = rgs
                .Where(rg => naming.ResourceGroupMatches(rg));
            var result = new List<InstanceOutputData>();
            foreach (var rg in filter)
            {
                var name = naming.FromResourceGroupName(rg.Name);
                result.Add(new InstanceOutputData(
                    name.PlainName,
                    rg.RegionName,
                    await runtime.GetDeployedRuntimeVersion(name, azure, cancellationToken))
                );
            }
            return result;
        }

        public async Task<IEnumerable<ILogDataObject>> ListByLocationAsync(string location, CancellationToken cancellationToken)
        {
            var runtime = new FunctionRuntimePackage(logger);
            var rgs = await azure.ResourceGroups.ListAsync(cancellationToken: cancellationToken);
            var filter = rgs.Where(
                rg => naming.ResourceGroupMatches(rg)
                && string.Compare(rg.RegionName, location, StringComparison.Ordinal) == 0);
            var result = new List<InstanceOutputData>();
            foreach (var rg in filter)
            {
                var name = naming.FromResourceGroupName(rg.Name);
                result.Add(new InstanceOutputData(
                    name.PlainName,
                    rg.RegionName,
                    await runtime.GetDeployedRuntimeVersion(name, azure, cancellationToken))
                );
            }
            return result;
        }

        internal async Task<IEnumerable<ILogDataObject>> ListInResourceGroupAsync(string resourceGroup, CancellationToken cancellationToken)
        {
            var runtime = new FunctionRuntimePackage(logger);
            string rgName = naming.GetResourceGroupName(resourceGroup);
            var apps = await azure.AppServices.FunctionApps.ListByResourceGroupAsync(rgName, cancellationToken: cancellationToken);

            var result = new List<InstanceOutputData>();
            foreach (var app in apps)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var name = naming.FromFunctionAppName(app.Name, resourceGroup);
                result.Add(new InstanceOutputData(
                    name.PlainName,
                    app.Region.Name,
                    await runtime.GetDeployedRuntimeVersion(name, azure, cancellationToken))
                );
            }
            return result;
        }

        private static T GetCustomAttribute<T>()
            where T : Attribute
        {
            return Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(T), false).FirstOrDefault() as T;
        }

        internal async Task<bool> AddAsync(InstanceCreateNames instance, string location, string requiredVersion, string sourceUrl, InstanceFineTuning tuning, CancellationToken cancellationToken)
        {
            string rgName = instance.ResourceGroupName;
            bool ok = await MakeSureResourceGroupExistsAsync(instance.IsCustom, location, rgName, cancellationToken);
            if (!ok)
                return false;

            ok = await DeployArmTemplateAsync(instance, location, rgName, tuning, cancellationToken);
            if (!ok)
                return false;

            // check runtime package
            var package = new FunctionRuntimePackage(logger);
            ok = await package.UpdateVersionAsync(requiredVersion, sourceUrl, instance, azure, cancellationToken);
            if (ok)
            {
                var devopsLogonData = DevOpsLogon.Load().connection;
                if (devopsLogonData.Mode == DevOpsTokenType.PAT)
                {
                    logger.WriteVerbose($"Saving Azure DevOps token");
                    ok = await ChangeAppSettingsAsync(instance, devopsLogonData, SaveMode.Default, cancellationToken);
                    if (ok)
                    {
                        logger.WriteInfo($"Azure DevOps token saved");
                    }
                    else
                    {
                        logger.WriteError($"Failed to save Azure DevOps token");
                    }
                }
                else
                {
                    logger.WriteWarning($"Azure DevOps token type {devopsLogonData.Mode} is unsupported");
                    ok = false;
                }
            }
            return ok;
        }

        private async Task<bool> MakeSureResourceGroupExistsAsync(bool customInstance, string location, string rgName, CancellationToken cancellationToken)
        {
            logger.WriteVerbose($"Checking if Resource Group {rgName} already exists");
            if (!await azure.ResourceGroups.ContainAsync(rgName, cancellationToken))
            {
                if (customInstance)
                {
                    logger.WriteError($"Resource group {rgName} is custom and cannot be created.");
                    return false;
                }

                logger.WriteVerbose($"Creating resource group {rgName}");
                await azure.ResourceGroups
                    .Define(rgName)
                    .WithRegion(location)
                    .CreateAsync();
                logger.WriteInfo($"Resource group {rgName} created.");
            }
            // success
            return true;
        }

        internal class InstanceFineTuning
        {
            public string HostingPlanSku { get; set; }
            public string HostingPlanTier { get; set; }
            public string AppInsightLocation { get; set; }
        }

        private async Task<bool> DeployArmTemplateAsync(InstanceCreateNames instance, string location, string rgName, InstanceFineTuning tuning, CancellationToken cancellationToken)
        {
            // IDEA the template should create a Storage account and/or a Key Vault for Rules' use
            // TODO https://github.com/gjlumsden/AzureFunctionsSlots suggest that slots must be created in template
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
            if (parsedTemplate.SelectToken("parameters.aggregatorVersion") == null)
            {
                // not good, blah
                logger.WriteWarning($"Something is wrong with the ARM template");
                return false;
            }

            var infoVersion = GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            var templateParams = new Dictionary<string, Dictionary<string, object>>{
                // TODO give use more control by setting more parameters
                {"webLocation", new Dictionary<string, object>{{"value", location } }},
                {"aiLocation", new Dictionary<string, object>{{"value", tuning.AppInsightLocation } }},
                {"storageAccountType", new Dictionary<string, object>{{"value", "Standard_LRS" } }},

                {"functionAppName", new Dictionary<string, object>{{"value", instance.FunctionAppName } }},
                {"storageAccountName", new Dictionary<string, object>{{"value", instance.StorageAccountName } }},
                {"hostingPlanName", new Dictionary<string, object>{{"value", instance.HostingPlanName } }},
                {"appInsightName", new Dictionary<string, object>{{"value", instance.AppInsightName } }},

                {"aggregatorVersion", new Dictionary<string, object>{{"value", infoVersion.InformationalVersion } }},
                {"hostingPlanSkuName", new Dictionary<string, object>{{"value", tuning.HostingPlanSku } }},
                {"hostingPlanSkuTier", new Dictionary<string, object>{{"value", tuning.HostingPlanTier } }},
            };

            string deploymentName = SdkContext.RandomResourceName("aggregator", 24);
            logger.WriteInfo($"Started deployment (id: {deploymentName})");
            var deployment = await azure.Deployments.Define(deploymentName)
                    .WithExistingResourceGroup(rgName)
                    .WithTemplate(armTemplateString)
                    .WithParameters(templateParams)
                    .WithMode(DeploymentMode.Incremental)
                    .CreateAsync(cancellationToken);

            // poll
            const int pollIntervalInSeconds = 3;
            int totalDelay = 0;
            while (!(StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Succeeded") ||
                    StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Failed") ||
                    StringComparer.OrdinalIgnoreCase.Equals(deployment.ProvisioningState, "Cancelled")))
            {
                SdkContext.DelayProvider.Delay(pollIntervalInSeconds * 1000);
                totalDelay += pollIntervalInSeconds;
                logger.WriteVerbose($"Deployment running ({totalDelay}s)");
                await deployment.RefreshAsync(cancellationToken);
            }
            logger.WriteInfo($"Deployment {deployment.ProvisioningState}");

            return deployment.ProvisioningState == "Succeeded";
        }

        internal async Task<bool> ChangeAppSettingsAsync(InstanceName instance, DevOpsLogon devopsLogonData, SaveMode saveMode, CancellationToken cancellationToken)
        {
            var webFunctionApp = await GetWebApp(instance, cancellationToken);
            var configuration = await AggregatorConfiguration.ReadConfiguration(webFunctionApp);

            configuration.DevOpsTokenType = devopsLogonData.Mode;
            configuration.DevOpsToken = devopsLogonData.Token;
            configuration.SaveMode = saveMode;

            configuration.WriteConfiguration(webFunctionApp);
            return true;
        }

        internal async Task<bool> ChangeAppSettingsAsync(InstanceName instance, string _/*location*/, SaveMode saveMode, CancellationToken cancellationToken)
        {
            bool ok;
            var devopsLogonData = DevOpsLogon.Load().connection;
            if (devopsLogonData.Mode == DevOpsTokenType.PAT)
            {
                logger.WriteVerbose($"Saving Azure DevOps token");
                ok = await ChangeAppSettingsAsync(instance, devopsLogonData, saveMode, cancellationToken);
                logger.WriteInfo($"Azure DevOps token saved");
            }
            else
            {
                logger.WriteWarning($"Azure DevOps token type {devopsLogonData.Mode} is unsupported");
                ok = false;
            }

            return ok;
        }

        internal async Task<bool> RemoveAsync(InstanceName instance, string location)
        {
            string rgName = instance.ResourceGroupName;
            logger.WriteVerbose($"Searching instance {instance.PlainName}...");
            bool rgFound = await azure.ResourceGroups.ContainAsync(rgName);
            if (!rgFound)
            {
                logger.WriteWarning($"Resource Group {rgName} not found in {location}.");
                return false;
            }

            var functionApp = await azure.AppServices.FunctionApps.GetByResourceGroupAsync(rgName, instance.FunctionAppName);
            if (functionApp == null)
            {
                logger.WriteWarning($"Instance {instance.FunctionAppName} not found in resource group {rgName}.");
                return false;
            }

            logger.WriteVerbose($"Deleting instance {functionApp.Name} in resource group {rgName}.");
            await azure.AppServices.FunctionApps.DeleteByIdAsync(functionApp.Id);
            logger.WriteInfo($"Instance {functionApp.Name} deleted.");

            // we delete the RG only if was made by us
            logger.WriteVerbose($"Checking if last instance in resource group {rgName}");
            var apps = await azure.AppServices.FunctionApps.ListByResourceGroupAsync(rgName);
            if (apps == null || !apps.Any())
            {
                if (instance.IsCustom)
                {
                    logger.WriteWarning($"Resource group {rgName} is custom and won't be deleted.");
                    return true;
                }

                logger.WriteVerbose($"Deleting empty resource group {rgName}");
                await azure.ResourceGroups.DeleteByNameAsync(rgName);
                logger.WriteInfo($"Resource group {rgName} deleted.");
            }

            return true;
        }

        internal async Task<bool> StreamLogsAsync(InstanceName instance, CancellationToken cancellationToken, string lastLinePattern = "!!THIS STRING NEVER SHOWS!!", TextWriter outStream = null)
        {
            var kudu = GetKudu(instance);
            logger.WriteVerbose($"Connecting to {instance.PlainName}...");

            // Main takes care of resetting color
            Console.ForegroundColor = ConsoleColor.Green;

            await kudu.StreamLogsAsync(outStream ?? Console.Out, lastLinePattern, cancellationToken);

            return true;
        }

        internal async Task<bool> UpdateAsync(InstanceName instance, string requiredVersion, string sourceUrl, CancellationToken cancellationToken)
        {
            // update runtime package
            var package = new FunctionRuntimePackage(_logger);
            bool ok = await package.UpdateVersionAsync(requiredVersion, sourceUrl, instance, _azure, cancellationToken);
            if (!ok)
                return false;

            await ForceFunctionRuntimeVersionAsync(instance, cancellationToken);

            var uploadFiles = await UpdateDefaultFilesAsync(package);

            var rules = new AggregatorRules(_azure, _logger);
            var allRules = await rules.ListAsync(instance, cancellationToken);

            foreach (var ruleName in allRules.Select(r => r.RuleName))
            {
                _logger.WriteInfo($"Updating Rule '{ruleName}'");
                await rules.UploadRuleFilesAsync(instance, ruleName, uploadFiles, cancellationToken);
            }

            return true;
        }

        private static async Task<Dictionary<string, string>> UpdateDefaultFilesAsync(FunctionRuntimePackage package)
        {
            var uploadFiles = new Dictionary<string, string>();
            using (var archive = System.IO.Compression.ZipFile.OpenRead(package.RuntimePackageFile))
            {
                var entry = archive.Entries
                                   .Single(e => string.Equals("aggregator-function.dll", e.Name, StringComparison.OrdinalIgnoreCase));

                using (var assemblyStream = entry.Open())
                {
                    await uploadFiles.AddFunctionDefaultFiles(assemblyStream);
                }
            }
            //TODO handle FileNotFound Exception when trying to get resource content, and resource not found
            return uploadFiles;
        }

        private async Task ForceFunctionRuntimeVersionAsync(InstanceName instance, CancellationToken cancellationToken)
        {
            const string TargetVersion = "~3";
            // Change V2 to V3 FUNCTIONS_EXTENSION_VERSION ~3
            var webFunctionApp = await GetWebApp(instance, cancellationToken);
            var currentAzureRuntimeVersion = webFunctionApp.GetAppSettings()
                                                           .GetValueOrDefault("FUNCTIONS_EXTENSION_VERSION");
            if (currentAzureRuntimeVersion?.Value != TargetVersion)
            {
                webFunctionApp.Update()
                          .WithAppSetting("FUNCTIONS_EXTENSION_VERSION", TargetVersion)
                          .Apply();
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using XUnitPriorityOrderer;

namespace integrationtests.cli
{
    public abstract class Scenario2_Base : End2EndScenarioBase
    {
        protected readonly string instancePrefix = "mintest";
        protected readonly string ruleName = "test4";
        protected readonly string ruleFile = "test4.rule";
        protected readonly string runtimeFilename = "FunctionRuntime.zip";
        protected readonly string instanceName;
        protected readonly string oldVersionDir;
        protected string PreviousVersionRuntimeFile => Path.Combine(oldVersionDir, runtimeFilename);

        protected Scenario2_Base(ITestOutputHelper output)
            : base(output)
        {
            oldVersionDir = Path.Combine(Path.GetTempPath(), TestLogonData.UniqueSuffix);
            instanceName = instancePrefix + TestLogonData.UniqueSuffix;
        }

        protected async Task<(int rc, string output)> RunOldAggregator(string arguments, IEnumerable<(string, string)> env = default)
            => await base.RunAggregatorProcess(oldVersionDir, arguments, env);
    }

    [TestCaseOrderer(CasePriorityOrderer.TypeName, CasePriorityOrderer.AssembyName)]
    public class Scenario2_Upgrade : Scenario2_Base
    {
        public Scenario2_Upgrade(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact, Order(1)]
        async Task DownloadOldVersion()
        {
            string package = Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => "aggregator-cli-win-x64.zip",
                PlatformID.Unix => "aggregator-cli-linux-x64.zip",
                PlatformID.MacOSX => "aggregator-cli-osx-x64.zip",
                _ => throw new Exception()
            };
            string packageURL = $"https://github.com/tfsaggregator/aggregator-cli/releases/download/v{TestLogonData.VersionToUpgrade}/{package}";
            string packageFile = Path.GetTempFileName();
            var cancellationToken = CancellationToken.None;
            await DownloadFile(packageURL, packageFile, cancellationToken);
            System.IO.Compression.ZipFile.ExtractToDirectory(packageFile, oldVersionDir, true);
            File.Delete(packageFile);
            string runtimeURL = $"https://github.com/tfsaggregator/aggregator-cli/releases/download/v{TestLogonData.VersionToUpgrade}/{runtimeFilename}";
            await DownloadFile(runtimeURL, PreviousVersionRuntimeFile, cancellationToken);
        }

        [Fact, Order(5)]
        async Task LogonToOldVersion()
        {
            (int rc, string output) = await RunOldAggregator(
                $"logon.azure --subscription {TestLogonData.SubscriptionId} --client {TestLogonData.ClientId} --password {TestLogonData.ClientSecret} --tenant {TestLogonData.TenantId}");
            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            (int rc2, string output2) = await RunOldAggregator(
                $"logon.ado --url {TestLogonData.DevOpsUrl} --mode PAT --token {TestLogonData.PAT}");
            Assert.Equal(0, rc2);
            Assert.DoesNotContain("] Failed!", output2);
        }

        [Fact, Order(10)]
        async Task InstallOldVersionInstance()
        {
            (int rc, string output) = await RunOldAggregator($"install.instance --verbose --name {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --location {TestLogonData.Location}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {PreviousVersionRuntimeFile}"));

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(20)]
        async Task AddRuleToOldVersion()
        {
            string ruleFullPath = Path.Combine(Directory.GetCurrentDirectory(), ruleFile);
            (int rc, string output) = await RunOldAggregator($"add.rule --verbose --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --name {ruleName} --file {ruleFullPath}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(30)]
        async Task MapRuleToOldVersion()
        {
            (int rc, string output) = await RunOldAggregator($"map.rule --verbose --project \"{TestLogonData.ProjectName}\" --event workitem.created --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup} --rule {ruleName}");

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(40)]
        async Task TriggerRuleInOldVersion()
        {
            (int rc, string output) = await RunOldAggregator($"test.create --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instanceName} --project \"{TestLogonData.ProjectName}\"  --rule {ruleName} ");
            Assert.Equal(0, rc);
            // Sample output from rule:
            //  Returning 'Hello Task #118 from Rule 5!' from 'TestRule5'
            Assert.Contains($"Returning 'Hello Task #", output);
            Assert.Contains($"!' from '{ruleName}'", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(45)]
        async Task LogonToCurrent()
        {
            (int rc, string output) = await RunAggregatorCommand(
                $"logon.azure --subscription {TestLogonData.SubscriptionId} --client {TestLogonData.ClientId} --password {TestLogonData.ClientSecret} --tenant {TestLogonData.TenantId}");
            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
            (int rc2, string output2) = await RunAggregatorCommand(
                $"logon.ado --url {TestLogonData.DevOpsUrl} --mode PAT --token {TestLogonData.PAT}");
            Assert.Equal(0, rc2);
            Assert.DoesNotContain("] Failed!", output2);
        }


        [Fact, Order(50)]
        async Task UpgradeInstanceToLatestVersion()
        {
            (int rc, string output) = await RunAggregatorCommand($"update.instance --verbose --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup}"
                + (string.IsNullOrWhiteSpace(TestLogonData.RuntimeSourceUrl)
                ? string.Empty
                : $" --sourceUrl {TestLogonData.RuntimeSourceUrl}"));

            Assert.Equal(0, rc);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(60)]
        async Task TriggerRuleInUpgradedInstance()
        {
            (int rc, string output) = await RunAggregatorCommand($"test.create --verbose --resourceGroup {TestLogonData.ResourceGroup} --instance {instanceName} --project \"{TestLogonData.ProjectName}\"  --rule {ruleName} ");
            Assert.Equal(0, rc);
            // Sample output from rule:
            //  Returning 'Hello Task #118 from Rule 5!' from 'TestRule5'
            Assert.Contains($"Returning 'Hello Task #", output);
            Assert.Contains($"!' from '{ruleName}'", output);
            Assert.DoesNotContain("] Failed!", output);
        }

        [Fact, Order(99)]
        async Task FinalCleanUp()
        {
            (_, _) = await RunAggregatorCommand($"unmap.rule --verbose --project \"{TestLogonData.ProjectName}\" --event * --rule * --instance {instanceName} --resourceGroup {TestLogonData.ResourceGroup}");
            (int rc, _) = await RunAggregatorCommand($"test.cleanup --verbose --resourceGroup {TestLogonData.ResourceGroup} ");
            WriteLineToOutput($"Deleting {oldVersionDir}");
            Directory.Delete(oldVersionDir, true);
            Assert.Equal(0, rc);
        }
    }
}

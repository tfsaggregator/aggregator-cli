using Xunit;
using aggregator.cli;

namespace unittests_core
{
    public class FileNamingTemplateTests
    {
        [Theory]
        [InlineData("{}", "n", "rg")]
        [InlineData(@"{""ResourceGroupPrefix"":""a""}", "n", "arg")]
        [InlineData(@"{""ResourceGroupSuffix"":""z""}", "n", "rgz")]
        [InlineData(@"{""ResourceGroupPrefix"":""p"",""ResourceGroupSuffix"":""s""}", "n", "prgs")]
        public void ResourceGroupName(string jsonData, string plainName, string resourceGroupName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(resourceGroupName, names.ResourceGroupName);
        }

        [Theory]
        [InlineData("{}", "n", "n")]
        [InlineData(@"{""FunctionAppPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""FunctionAppSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""FunctionAppPrefix"":""p"",""FunctionAppSuffix"":""s""}", "n", "pns")]
        public void FunctionAppName(string jsonData, string plainName, string functionAppName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(functionAppName, names.FunctionAppName);
        }

        [Theory]
        [InlineData("{}", "n", "n")]
        [InlineData(@"{""HostingPlanPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""HostingPlanSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""HostingPlanPrefix"":""p"",""HostingPlanSuffix"":""s""}", "n", "pns")]
        public void HostingPlanName(string jsonData, string plainName, string hostingPlanName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(hostingPlanName, names.HostingPlanName);
        }

        [Theory]
        [InlineData("{}", "n", "n")]
        [InlineData(@"{""AppInsightPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""AppInsightSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""AppInsightPrefix"":""p"",""AppInsightSuffix"":""s""}", "n", "pns")]
        public void AppInsightName(string jsonData, string plainName, string appInsightName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(appInsightName, names.AppInsightName);
        }

        [Theory]
        [InlineData("{}", "n", "n")]
        [InlineData(@"{""StorageAccountPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""StorageAccountSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""StorageAccountPrefix"":""p"",""StorageAccountSuffix"":""s""}", "n", "pns")]
        public void StorageAccountName(string jsonData, string plainName, string storageAccountName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(storageAccountName, names.StorageAccountName);
        }
    }
}
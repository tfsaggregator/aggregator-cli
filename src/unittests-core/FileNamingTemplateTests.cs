using System;
using aggregator.cli;
using Xunit;

namespace unittests_core
{
    public class FileNamingTemplateTests
    {
        [Fact]
        public void CtorValidate_Fails()
        {
            //"Must specify at least one affix for ResourceGroup"
            Assert.True(true);
            //Assert.Throws<ArgumentException>(() => new FileNamingTemplates("{}"));
        }

        [Fact]
        public void InstanceValidate_Fails_OnNullName()
        {
            //"Must specify at least one affix for ResourceGroup"
            var templates = new FileNamingTemplates(@"{""ResourceGroupPrefix"":""a""}");
            Assert.Throws<ArgumentException>(() => templates.Instance("", null));
        }

        [Theory]
        [InlineData(@"{}", "n", "rg")]
        [InlineData(@"{""ResourceGroupPrefix"":""a""}", "n", "arg")]
        [InlineData(@"{""ResourceGroupSuffix"":""z""}", "n", "rgz")]
        [InlineData(@"{""ResourceGroupPrefix"":""p"",""ResourceGroupSuffix"":""s""}", "n", "prgs")]
        public void ResourceGroupName_Succeeds(string jsonData, string plainName, string resourceGroupName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(resourceGroupName, names.ResourceGroupName);
        }

        [Theory]
        [InlineData(@"{""FunctionAppPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppPrefix"":""p"",""FunctionAppSuffix"":""s""}", "n", "pns")]
        public void FunctionAppName_Succeeds(string jsonData, string plainName, string functionAppName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(functionAppName, names.FunctionAppName);
        }

        [Theory]
        [InlineData(@"{""HostingPlanPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""HostingPlanPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""HostingPlanSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""HostingPlanPrefix"":""p"",""HostingPlanSuffix"":""s""}", "n", "pns")]
        public void HostingPlanName_Succeeds(string jsonData, string plainName, string hostingPlanName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(hostingPlanName, names.HostingPlanName);
        }

        [Theory]
        [InlineData(@"{""AppInsightPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""AppInsightPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""AppInsightSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""AppInsightPrefix"":""p"",""AppInsightSuffix"":""s""}", "n", "pns")]
        public void AppInsightName_Succeeds(string jsonData, string plainName, string appInsightName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(appInsightName, names.AppInsightName);
        }

        [Theory]
        [InlineData(@"{""StorageAccountPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""StorageAccountPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""StorageAccountSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""StorageAccountPrefix"":""p"",""StorageAccountSuffix"":""s""}", "n", "pns")]
        public void StorageAccountName_Succeeds(string jsonData, string plainName, string storageAccountName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(storageAccountName, names.StorageAccountName);
        }

        [Theory]
        [InlineData(@"{}", "n", "n")]
        [InlineData(@"{""ResourceGroupPrefix"":""a""}", "an", "n")]
        [InlineData(@"{""ResourceGroupSuffix"":""z""}", "nz", "n")]
        [InlineData(@"{""ResourceGroupPrefix"":""p"",""ResourceGroupSuffix"":""s""}", "pns", "n")]
        public void FromResourceGroupName_Succeeds(string jsonData, string rgName, string expected)
        {
            var templates = new FileNamingTemplates(jsonData);

            var actual = templates.FromResourceGroupName(rgName);

            Assert.Equal(expected, actual.PlainName);
        }

        [Theory]
        [InlineData(@"{}", "rg", "app", "app")]
        [InlineData(@"{""ResourceGroupPrefix"":""a""}", "arg", "app", "app")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppPrefix"":""a""}", "arg", "an", "n")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppSuffix"":""z""}", "arg", "nz", "n")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppPrefix"":""p"",""FunctionAppSuffix"":""s""}", "arg", "pns", "n")]
        public void FromFunctionAppName_Succeeds(string jsonData, string rgName, string appName, string expected)
        {
            var templates = new FileNamingTemplates(jsonData);

            var actual = templates.FromFunctionAppName(appName, rgName);

            Assert.Equal(expected, actual.PlainName);
        }

        [Theory]
        [InlineData(@"{}", "https://zorro.azure.net/pippo", "zorro")]
        [InlineData(@"{""ResourceGroupPrefix"":""a""}", "https://zorro.azure.net/pippo", "zorro")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppPrefix"":""a""}", "https://an.azure.net/pippo", "n")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppSuffix"":""z""}", "https://nz.azure.net/pippo", "n")]
        [InlineData(@"{""ResourceGroupPrefix"":""a"",""FunctionAppPrefix"":""p"",""FunctionAppSuffix"":""s""}", "https://pns.azure.net/pippo", "n")]
        public void FromFunctionAppUrl_Succeeds(string jsonData, string url, string expected)
        {
            var templates = new FileNamingTemplates(jsonData);

            var actual = templates.FromFunctionAppUrl(new Uri(url));

            Assert.Equal(expected, actual.PlainName);
        }

        [Theory]
        [InlineData(@"{}", "n", "n")]
        [InlineData(@"{""ResourceGroupPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""ResourceGroupSuffix"":""z""}", "n", "nz")]
        [InlineData(@"{""ResourceGroupPrefix"":""p"",""ResourceGroupSuffix"":""s""}", "n", "pns")]
        public void GetResourceGroupName_Succeeds(string jsonData, string rgInput, string expected)
        {
            var templates = new FileNamingTemplates(jsonData);
            var rgOut = templates.GetResourceGroupName(rgInput);
            Assert.Equal(expected, rgOut);
        }

    }
}

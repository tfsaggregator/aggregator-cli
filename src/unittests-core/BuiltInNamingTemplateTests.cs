using Xunit;
using aggregator.cli;

namespace unittests_core
{
    public class BuiltInNamingTemplateTests
    {
        [Fact]
        public void CustomResourceGroupName()
        {
            var templates = new BuiltInNamingTemplates();
            var names = templates.GetInstanceCreateNames("n", "rg");
            Assert.Equal("n", names.PlainName);
            Assert.Equal("naggregator", names.FunctionAppName);
            Assert.Equal("rg", names.ResourceGroupName);
            Assert.True(names.IsCustom);
        }

        [Fact]
        public void DefaultResourceGroupName()
        {
            var templates = new BuiltInNamingTemplates();
            var names = templates.GetInstanceCreateNames("n", null);
            Assert.Equal("n", names.PlainName);
            Assert.Equal("naggregator", names.FunctionAppName);
            Assert.Equal("aggregator-n", names.ResourceGroupName);
            Assert.False(names.IsCustom);
        }
    }
}
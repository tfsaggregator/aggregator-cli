using Xunit;
using aggregator.cli;

namespace unittests_core
{
    public class NamingTemplateTests
    {
        [Theory]
        [InlineData("{}", "n","n")]
        [InlineData(@"{""FunctionAppPrefix"":""a""}", "n", "an")]
        [InlineData(@"{""FunctionAppSuffix"":""a""}", "n", "na")]
        [InlineData(@"{""FunctionAppPrefix"":""p"",""FunctionAppSuffix"":""s""}", "n", "pns")]
        public void ReadIt(string jsonData, string plainName, string functionAppName)
        {
            var templates = new FileNamingTemplates(jsonData);
            var names = templates.InstanceExt("n", "rg");
            Assert.Equal(plainName, names.PlainName);
            Assert.Equal(functionAppName, names.FunctionAppName);
        }
    }
}
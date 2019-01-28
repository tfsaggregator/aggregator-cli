using Xunit;
using aggregator.cli;

namespace unittests_core
{
    public class ManifestParserTests
    {
        [Theory]
        [InlineData("version=1.0-RC", "1.0-RC")]
        [InlineData("version=1.0", "1.0")]
        [InlineData("version=1.0\n", "1.0")]
        [InlineData("\n\nversion=3.1\n", "3.1")]
        public void GivenAnCorrectManifestContent_WhenParsingTheManifest_ThenCorrectVersionNumver_ShouldBePresent(string content, string expectedVersionNumber)
        {
            var actualResult = ManifestParser.Parse(content).Version;
            var expectedResult = Semver.SemVersion.Parse(expectedVersionNumber);
            Assert.Equal(expectedResult, actualResult);
        }
    }
}
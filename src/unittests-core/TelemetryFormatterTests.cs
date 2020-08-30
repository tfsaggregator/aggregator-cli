using System.Collections.Generic;
using aggregator.cli;
using Xunit;

namespace unittests_core
{
    public class TelemetryFormatterTests
    {
        [Fact]
        public void GivenNull_WhenUnknownMode_ThenConstant_ShouldBeReturned()
        {
            string expect = "UNSUPPORTED";

            var actual = TelemetryFormatter.Format((TelemetryDisplayMode)42, null);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenNotNull_WhenUnknownMode_ThenConstant_ShouldBeReturned()
        {
            string expect = "UNSUPPORTED";

            var actual = TelemetryFormatter.Format((TelemetryDisplayMode)42, 42);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenNull_WhenModePresence_ThenConstant_ShouldBeReturned()
        {
            string expect = "unset";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.Presence, null);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenNonNull_WhenModePresence_ThenConstant_ShouldBeReturned()
        {
            string expect = "set";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.Presence, 42);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenNull_WhenModeAsIs_ThenEmptyString_ShouldBeReturned()
        {
            string expect = "";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, null);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenAnEmptyString_WhenModeAsIs_ThenEmptyString_ShouldBeReturned()
        {
            string expect = "";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, "");

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenANonEmptyString_WhenModeAsIs_ThenSame_ShouldBeReturned()
        {
            string expect = "some";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, "some");

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenAnInt_WhenModeAsIs_ThenSame_ShouldBeReturned()
        {
            string expect = "42";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, 42);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenABool_WhenModeAsIs_ThenSame_ShouldBeReturned()
        {
            string expect = "True";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, true);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenAnEnum_WhenModeAsIs_ThenSame_ShouldBeReturned()
        {
            string expect = "TwoPhases";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, aggregator.SaveMode.TwoPhases);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenEmptyCollection_WhenModeAsIs_ThenEmptyString_ShouldBeReturned()
        {
            var arg = new List<string>();
            string expect = "";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, arg);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenOneElementCollection_WhenModeAsIs_ThenElement_ShouldBeReturned()
        {
            var arg = new List<string>() { "foo" };
            string expect = "foo";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, arg);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenTwoElementsCollection_WhenModeAsIs_ThenElements_ShouldBeReturned()
        {
            var arg = new List<string>() { "foo", "bar" };
            string expect = "foo;bar";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.AsIs, arg);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenInvalidUrl_WhenModeUrlPublic_ThenEmptyString_ShouldBeReturned()
        {
            var arg = "foobar";
            string expect = "";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.MaskOthersUrl, arg);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenFileUrl_WhenModeUrlPublic_ThenDummy_ShouldBeReturned()
        {
            var arg = "file://C:/temp/FunctionRuntime.zip";
            string expect = "file://***/FunctionRuntime.zip";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.MaskOthersUrl, arg);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenLocalUrl_WhenModeUrlPublic_ThenDummy_ShouldBeReturned()
        {
            var arg = "https://artifactory/FunctionRuntime.zip?foo=bar";
            string expect = "https://***/FunctionRuntime.zip";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.MaskOthersUrl, arg);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenPrivateGitHubUrl_WhenModeUrlPublic_ThenSame_ShouldBeReturned()
        {
            var arg = "https://github.com/myorg/aggregator-cli/releases/download/v0.9.13/FunctionRuntime.zip";
            string expect = "https://***/FunctionRuntime.zip";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.MaskOthersUrl, arg);

            Assert.Equal(expect, actual);
        }

        [Fact]
        public void GivenOurGitHubUrl_WhenModeUrlPublic_ThenSame_ShouldBeReturned()
        {
            var arg = "https://github.com/tfsaggregator/aggregator-cli/releases/download/v0.9.13/FunctionRuntime.zip";
            string expect = "https://github.com/tfsaggregator/aggregator-cli/releases/download/v0.9.13/FunctionRuntime.zip";

            var actual = TelemetryFormatter.Format(TelemetryDisplayMode.MaskOthersUrl, arg);

            Assert.Equal(expect, actual);
        }

    }
}

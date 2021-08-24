using aggregator.Engine;
using Xunit;

namespace unittests_ruleng
{
    public class ScriptedRuleWrapperTests
    {
        [Fact]
        public void GivenAValidRule_WhenTheRuleIsVerified_ThenTheResult_ShouldBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("dummy", new[] { "" });

            //When
            var (success, diagnostics) = rule.Verify();

            //Then
            Assert.True(success);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void GivenAnNotCompilableRule_WhenTheRuleIsVerified_ThenTheResult_ShouldNotBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("dummy", new[] { "(" });

            //When
            var (success, diagnostics) = rule.Verify();

            //Then
            Assert.False(success);
            Assert.NotEmpty(diagnostics);
        }

        [Fact]
        public void GivenAnNotParsableRule_WhenTheRuleIsVerified_ThenTheResult_ShouldNotBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("dummy", new[] { ".invalid=directive" });

            //When
            var (success, diagnostics) = rule.Verify();

            //Then
            Assert.False(success);
            Assert.Empty(diagnostics);
        }
    }
}

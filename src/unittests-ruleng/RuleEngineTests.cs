using aggregator.Engine;
using Xunit;

namespace unittests_ruleng
{
    public class RuleEngineTests
    {
        [Fact]
        public void GivenAnValidRule_WhenTheRuleIsVerified_ThenTheResult_ShouldBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("Test", new[] { "" });

            //When
            var (success, diagnostics) = rule.Verify();

            //Then
            Assert.True(success);
            Assert.Empty(diagnostics);
        }

        [Fact]
        public void GivenAnInvalidRule_WhenTheRuleIsVerified_ThenTheResult_ShouldNotBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("Test", new[] { "(" });

            //When
            var (success, diagnostics) = rule.Verify();

            //Then
            Assert.False(success);
            Assert.NotEmpty(diagnostics);
        }
    }
}

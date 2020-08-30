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
            var result = rule.Verify();

            //Then
            Assert.True(result.success);
            Assert.Empty(result.diagnostics);
        }

        [Fact]
        public void GivenAnInvalidRule_WhenTheRuleIsVerified_ThenTheResult_ShouldNotBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("Test", new[] { "(" });

            //When
            var result = rule.Verify();

            //Then
            Assert.False(result.success);
            Assert.NotEmpty(result.diagnostics);
        }
    }
}
using aggregator.Engine;
using Xunit;

namespace unittests_ruleng
{
    public class ScriptedRuleWrapperTests
    {
        [Fact]
        public void GivenAnValidRule_WhenTheRuleIsVerified_ThenTheResult_ShouldBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("dummy", new[] { "" });


            //When
            var result = rule.Verify();

            //Then
            Assert.True(result.success);
            Assert.Empty(result.diagnostics);
        }

        [Fact]
        public void GivenAnNotCompilableRule_WhenTheRuleIsVerified_ThenTheResult_ShouldNotBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("dummy", new[] { "(" });


            //When
            var result = rule.Verify();

            //Then
            Assert.False(result.success);
            Assert.NotEmpty(result.diagnostics);
        }

        [Fact]
        public void GivenAnNotParsableRule_WhenTheRuleIsVerified_ThenTheResult_ShouldNotBeSuccessfull()
        {
            //Given
            var rule = new ScriptedRuleWrapper("dummy", new[] { ".invalid=directive" });


            //When
            var result = rule.Verify();

            //Then
            Assert.False(result.success);
            Assert.Empty(result.diagnostics);
        }
    }
}
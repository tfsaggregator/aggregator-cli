using aggregator;
using aggregator.Engine;
using NSubstitute;
using Xunit;

namespace unittests_ruleng
{
    public class RuleEngineTests
    {
        [Fact]
        public void GivenAnValidRule_WhenTheRuleIsVerified_ThenTheResult_ShouldBeSuccessfull()
        {
            //Given
            var engine = new RuleEngine(Substitute.For<IAggregatorLogger>(), new [] { "" }, SaveMode.Batch, true);

            //When
            var result = engine.VerifyRule();

            //Then
            Assert.True(result.success);
            Assert.Empty(result.diagnostics);
        }

        [Fact]
        public void GivenAnInvalidRule_WhenTheRuleIsVerified_ThenTheResult_ShouldNotBeSuccessfull()
        {
            //Given
            var engine = new RuleEngine(Substitute.For<IAggregatorLogger>(), new [] { "(" }, SaveMode.Batch, true);

            //When
            var result = engine.VerifyRule();

            //Then
            Assert.False(result.success);
            Assert.NotEmpty(result.diagnostics);
        }
    }
}
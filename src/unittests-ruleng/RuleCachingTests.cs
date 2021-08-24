using aggregator;
using aggregator.Engine;
using NSubstitute;
using Xunit;
using Xunit.Priority;

namespace unittests_ruleng
{
    [TestCaseOrderer(PriorityOrderer.Name, PriorityOrderer.Assembly)]
    public class RuleCachingTests
    {
#if DEBUG
        [Fact, Priority(0)]
        public void GivenTheSameRule_WhenTheRuleIsParsedThrice_ThenTheLogger_LogsThatWasFoundInCache()
        {
            //Given
            var logger = Substitute.For<IAggregatorLogger>();
            string ruleName = "dummy";
            var ruleCode = new[] { "" };

            //When
            _ = new ScriptedRuleWrapper(ruleName, ruleCode, logger);
            _ = new ScriptedRuleWrapper(ruleName, ruleCode, logger);
            _ = new ScriptedRuleWrapper(ruleName, ruleCode, logger);

            //Then
            logger.Received(1).WriteVerbose("Rule dummy was not in cache: compiling");
            logger.Received(2).WriteVerbose("Rule dummy found in cache");
        }

        [Fact, Priority(1)]
        public void GivenARule_WhenTheRuleChanges_ThenTheLogger_LogsThatWasNotFoundInCache()
        {
            //Given
            var logger = Substitute.For<IAggregatorLogger>();
            string ruleName = "dummy";
            var ruleCode1 = new[] { "/* v1 */" };
            var ruleCode2 = new[] { "/* v2 */" };

            //When
            _ = new ScriptedRuleWrapper(ruleName, ruleCode1, logger);
            _ = new ScriptedRuleWrapper(ruleName, ruleCode2, logger);
            _ = new ScriptedRuleWrapper(ruleName, ruleCode2, logger);

            //Then
            logger.Received(2).WriteVerbose("Rule dummy was not in cache: compiling");
            logger.Received(1).WriteVerbose("Rule dummy found in cache");
        }
#endif
    }
}

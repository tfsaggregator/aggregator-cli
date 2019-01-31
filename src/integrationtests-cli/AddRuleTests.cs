using System;

using Xunit;
using Xunit.Abstractions;

namespace integrationtests.cli
{
    public class AddRuleTests : End2EndScenarioBase
    {
        public AddRuleTests(ITestOutputHelper output)
            : base(output)
        {
            // does nothing
        }

        [Fact]
        public void GivenAnInvalidRuleFile_WhenAddingThisRule_ThenTheProcessing_ShouldBeAborted()
        {
            //Given
            const string invalidRuleFileName = "invalid_rule1.rule";

            //When
            (int rc, string output) = RunAggregatorCommand(FormattableString.Invariant($"add.rule --verbose --instance foobar --resourceGroup foobar --name foobar --file {invalidRuleFileName}"));

            //Then
            Assert.Equal(1, rc);
            Assert.Contains(@"Errors in the rule file invalid_rule1.rule:", output);
        }
    }
}
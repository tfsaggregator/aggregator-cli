using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using aggregator;
using aggregator.Engine;
using aggregator.Engine.Language;

using Microsoft.TeamFoundation.WorkItemTracking.WebApi.Models;

using NSubstitute;

using Xunit;


namespace unittests_ruleng
{
    public class RuleFileParserTests
    {
        [Fact]
        public void RuleLanguageDefaultsCSharp_Succeeds()
        {

            string ruleCode = @"
return $""Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"";
";

            (IRuleDirectives directives, bool _, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.Empty(directives.References);
            Assert.Empty(directives.Imports);
            Assert.NotEmpty(directives.RuleCode);
            Assert.Equal(RuleLanguage.Csharp, directives.Language);
        }

        [Theory]
        [InlineData(".language=CSharp", RuleLanguage.Csharp, true)]
        [InlineData(".language=C#", RuleLanguage.Csharp, true)]
        [InlineData(".language=CS", RuleLanguage.Csharp, true)]
        [InlineData(".language=", RuleLanguage.Unknown, false)]

        public void RuleLanguageDirectiveParse_Succeeds(string ruleCode, RuleLanguage expectedLanguage, bool expectedParsingSuccess)
        {
            (IRuleDirectives directives, bool parsingSuccess, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.Equal(expectedLanguage, directives.Language);
            Assert.Equal(expectedParsingSuccess, parsingSuccess);
        }

    }
}

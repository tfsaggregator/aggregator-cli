using aggregator.Engine.Language;

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

            (IRuleDirectives directives, bool parsingSuccess, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.Empty(directives.References);
            Assert.Empty(directives.Imports);
            Assert.False(directives.Impersonate);
            Assert.NotEmpty(directives.RuleCode);
            Assert.Equal(RuleLanguage.Csharp, directives.Language);
            Assert.True(parsingSuccess);
        }

        [Theory]
        [InlineData(".language=CSharp")]
        [InlineData(".language=C#")]
        [InlineData(".language=CS")]
        public void RuleLanguageDirectiveParse_Succeeds(string ruleCode, RuleLanguage expectedLanguage = RuleLanguage.Csharp)
        {
            (IRuleDirectives directives, bool parsingSuccess, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.Equal(expectedLanguage, directives.Language);
        }

        [Theory]
        [InlineData(".language=")]
        [InlineData(".lang=WHAT")]
        [InlineData(".lang=C#\r\n.unrecognized=directive\r\nreturn string.Empty;\r\n", RuleLanguage.Csharp)]
        public void RuleLanguageDirectiveParse_Fails(string ruleCode, RuleLanguage expectedLanguage = RuleLanguage.Unknown)
        {
            (IRuleDirectives directives, bool parsingSuccess, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.False(parsingSuccess);
            Assert.Equal(expectedLanguage, directives.Language);
        }


        [Theory]
        [InlineData(".r=System.Xml.XDocument", 1)]
        [InlineData(".ref=System.Xml.XDocument", 1)]
        [InlineData(".reference=System.Xml.XDocument", 1)]
        public void RuleReferenceDirectiveParse_Succeeds(string ruleCode, int expectedReferenceCount)
        {
            (IRuleDirectives directives, bool parsingSuccess, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.Equal(expectedReferenceCount, directives.References.Count);
        }

        [Theory]
        [InlineData(".import=System.Diagnostics", 1)]
        [InlineData(".imports=System.Diagnostics", 1)]
        [InlineData(".namespace=System.Diagnostics", 1)]
        public void RuleImportDirectiveParse_Succeeds(string ruleCode, int expectedImportCount)
        {
            (IRuleDirectives directives, bool parsingSuccess, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.Equal(expectedImportCount, directives.Imports.Count);
        }

        [Fact]
        public void RuleImpersonateDirectiveParse_Succeeds()
        {
            string ruleCode = @".impersonate=onBehalfOfInitiator 
";

            (IRuleDirectives directives, bool parsingSuccess, string[] __) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.True(directives.Impersonate);
        }
    }
}

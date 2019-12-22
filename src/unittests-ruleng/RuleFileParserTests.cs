using System;
using System.Linq;
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

            (IPreprocessedRule ppRule, bool parsingSuccess) = RuleFileParser.Read(ruleCode.Mince());

            Assert.Empty(ppRule.References);
            Assert.Empty(ppRule.Imports);
            Assert.NotEmpty(ppRule.RuleCode);
            Assert.Equal(RuleLanguage.Csharp, ppRule.Language);
            Assert.True(parsingSuccess);
        }

        [Theory]
        [InlineData(".language=CSharp")]
        [InlineData(".language=C#")]
        [InlineData(".language=CS")]
        public void RuleLanguageDirectiveParse_Succeeds(string ruleCode, RuleLanguage expectedLanguage = RuleLanguage.Csharp)
        {
            (IPreprocessedRule ppRule, bool parsingSuccess) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.Equal(expectedLanguage, ppRule.Language);
        }

        [Theory]
        [InlineData(".language=")]
        [InlineData(".lang=WHAT")]
        [InlineData(".lang=C#\r\n.unrecognized=directive\r\nreturn string.Empty;\r\n", RuleLanguage.Csharp)]
        public void RuleLanguageDirectiveParse_Fails(string ruleCode, RuleLanguage expectedLanguage = RuleLanguage.Unknown)
        {
            (IPreprocessedRule ppRule, bool parsingSuccess) = RuleFileParser.Read(ruleCode.Mince());

            Assert.False(parsingSuccess);
            Assert.Equal(expectedLanguage, ppRule.Language);
        }


        [Theory]
        [InlineData(".r=System.Xml.XDocument", 1)]
        [InlineData(".ref=System.Xml.XDocument", 1)]
        [InlineData(".reference=System.Xml.XDocument", 1)]
        public void RuleReferenceDirectiveParse_Succeeds(string ruleCode, int expectedReferenceCount)
        {
            (IPreprocessedRule ppRule, bool parsingSuccess) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.Equal(expectedReferenceCount, ppRule.References.Count);
        }

        [Theory]
        [InlineData(".import=System.Diagnostics", 1)]
        [InlineData(".imports=System.Diagnostics", 1)]
        [InlineData(".namespace=System.Diagnostics", 1)]
        public void RuleImportDirectiveParse_Succeeds(string ruleCode, int expectedImportCount)
        {
            (IPreprocessedRule ppRule, bool parsingSuccess) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.Equal(expectedImportCount, ppRule.Imports.Count);
        }

        [Fact]
        public void RuleImpersonateDirectiveParse_Succeeds()
        {
            string ruleCode = @".impersonate=onBehalfOfInitiator 
";

            (IPreprocessedRule ppRule, bool parsingSuccess) = RuleFileParser.Read(ruleCode.Mince());

            Assert.True(parsingSuccess);
            Assert.True(ppRule.Impersonate);
        }

        [Fact]
        public void RuleLanguageReadWrite_Succeeds()
        {
            string ruleCode = @".language=C#
.reference=System.Xml.XDocument
.import=System.Diagnostics

return $""Hello { self.WorkItemType } #{ self.Id } - { self.Title }!"";
";

            var mincedCode = ruleCode.Mince();
            (IPreprocessedRule ppRule, _) = RuleFileParser.Read(mincedCode);

            var ruleCode2 = RuleFileParser.Write(ppRule);

            Assert.Equal(mincedCode, ruleCode2, StringComparer.OrdinalIgnoreCase);
        }
    }
}

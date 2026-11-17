/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 *
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>.
 */

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.GrammarGeneration;

namespace Minotaur.Tests.GrammarGeneration;

[TestClass]
public class GrammarProfileTests
{
    [TestMethod]
    public void Analyze_Classifies_RegexAndLiteralRules_AsTokenRules()
    {
        const string grammar = """
            Grammar: Demo
            FormatType: EBNF

            <whitespace> ::= /[ \t\r\n]+/ => { skip }
            <comment> ::= /\x2F\x2F[^\n]*/ => { skip }
            <identifier> ::= /[a-zA-Z_][a-zA-Z0-9_]*/
            <semicolon> ::= ";"
            """;

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(4, stats.TokenRuleCount);
        Assert.AreEqual(0, stats.ProductionRuleCount);
        Assert.AreEqual(4, stats.TotalRuleCount);
    }

    [TestMethod]
    public void Analyze_SameNamedRules_CollapseToOneDistinctTokenType()
    {
        const string grammar = """
            <integer_literal> ::= /[0-9]+/
            <integer_literal> ::= /0[xX][0-9a-fA-F]+/
            """;

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(2, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.DistinctTokenTypeCount);
    }

    [TestMethod]
    public void Analyze_Classifies_RuleReferencesAndKeywords_AsProductionRules()
    {
        const string grammar = """
            <using_directive> ::= using <namespace_name> ;
            <class_type> ::= <type_name> | object | string
            """;

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(0, stats.TokenRuleCount);
        Assert.AreEqual(2, stats.ProductionRuleCount);
    }

    [TestMethod]
    public void Analyze_SplitsMixedGrammar_IntoTokenAndProductionCounts()
    {
        const string grammar = """
            Grammar: Demo
            Keywords: if, else, while

            <whitespace> ::= /[ \t]+/ => { skip }
            <identifier> ::= /[a-z]+/
            <op_equals> ::= "=="
            <statement> ::= <identifier> = <identifier> ;
            <if_statement> ::= if ( <expression> ) <statement>
            """;

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(3, stats.TokenRuleCount);
        Assert.AreEqual(2, stats.ProductionRuleCount);
        Assert.AreEqual(5, stats.TotalRuleCount);
        Assert.AreEqual(3, stats.DistinctTokenTypeCount);
        Assert.AreEqual(3, stats.KeywordCount);
    }

    [TestMethod]
    public void Analyze_Skips_BlockCommentsAndHashComments()
    {
        const string grammar = """
            /*
             * A multi-line block comment mentioning
             * <fake_rule> ::= /not a rule/
             */
            # a whole-line comment with <another_fake> ::= "nope"
            <identifier> ::= /[a-z]+/
            """;

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(1, stats.TokenRuleCount);
        Assert.AreEqual(0, stats.ProductionRuleCount);
    }

    [TestMethod]
    public void Analyze_RegexPatternWithHexEscapedSlash_IsNotTreatedAsCommentOrFragment()
    {
        // The engine forbids a bare '/' inside a /.../ pattern; corpus
        // grammars write it as \x2F (issue #196 finding). The classifier
        // must still see one complete token rule.
        const string grammar = "<comment> ::= /\\x2F\\*([^*]|\\*+[^*\\x2F])*\\*+\\x2F/ => { skip }";

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(1, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.DistinctTokenTypeCount);
    }

    [TestMethod]
    public void Analyze_Strips_TrailingRuleTerminators()
    {
        // Corpus grammars are not uniform: some terminate every rule with
        // a semicolon (SQL.grammar style). The token/production split must
        // not depend on the terminator.
        const string grammar = """
            <auto_identifier_1> ::= /[a-zA-Z_]/ ;
            <statement> ::= <select_stmt> | <insert_stmt> ;
            """;

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(1, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.DistinctTokenTypeCount);
        Assert.AreEqual(1, stats.ProductionRuleCount);
    }

    [TestMethod]
    public void Analyze_EmptyGrammar_ReturnsZeroCounts()
    {
        var stats = GrammarProfile.Analyze(string.Empty);

        Assert.AreEqual(0, stats.TokenRuleCount);
        Assert.AreEqual(0, stats.DistinctTokenTypeCount);
        Assert.AreEqual(0, stats.ProductionRuleCount);
        Assert.AreEqual(0, stats.TotalRuleCount);
        Assert.AreEqual(0, stats.KeywordCount);
    }

    [TestMethod]
    public void Analyze_KeywordsHeader_IgnoresEmptyEntries()
    {
        const string grammar = "Keywords: abstract, as,, base,\n";

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(3, stats.KeywordCount);
    }

    [TestMethod]
    public void Analyze_SingleQuotedLiteral_IsTokenRule()
    {
        // Verified against DevelApp.StepParser 1.75.0: a single-quoted
        // literal is a complete lexer token rule.
        const string grammar = "<lx_p26> ::= '\"'\n";

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(1, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.DistinctTokenTypeCount);
    }

    [TestMethod]
    public void Analyze_QuotedPairWrappingRuleReferences_IsProductionRule()
    {
        // A right-hand side that only starts and ends with a quoted literal
        // but references rules in between (SQL coverage style) is a
        // production rule, not a token rule.
        const string grammar = """
            <dml_with_clause> ::= "WITH" ( "RECURSIVE" )? <cte_definition>
            """;

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(0, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.ProductionRuleCount);
    }

    [TestMethod]
    public void Analyze_RegexAlternation_IsProductionRule()
    {
        // The loader closes a /.../ pattern at the first inner unescaped
        // slash, so an alternation of two patterns is not one complete
        // token rule (verified against the engine).
        const string grammar = "<number> ::= /[0-9]+/ | /[a-z]+/\n";

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(0, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.ProductionRuleCount);
    }

    [TestMethod]
    public void Analyze_LiteralAlternation_IsProductionRule()
    {
        // Quoted-literal alternation is not one complete literal either:
        // quotes inside the right-hand side are literal code points.
        const string grammar = "<boolean> ::= \"true\" | \"false\"\n";

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(0, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.ProductionRuleCount);
    }

    [TestMethod]
    public void Analyze_QuotedLiteralWithSpacesAndBrackets_IsTokenRule()
    {
        // Verified against the engine: a quoted literal's content may
        // contain spaces and angle brackets; the whole content is matched
        // literally. Such corpus rules are token rules.
        const string grammar = "<character_literal> ::= ' <character> '\n";

        var stats = GrammarProfile.Analyze(grammar);

        Assert.AreEqual(1, stats.TokenRuleCount);
        Assert.AreEqual(1, stats.DistinctTokenTypeCount);
    }
}

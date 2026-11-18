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

namespace Minotaur.Benchmarks.ParserRuntime;

/// <summary>
/// GrammarForge grammars for the three case-study DSLs of the issue #127
/// parser-runtime benchmark (configuration, analytics query and
/// multi-language template DSLs).
/// </summary>
/// <remarks>
/// The rules follow the grammar discipline the shipped
/// <c>RealTimeParserSession</c> (DevelApp.StepParser 1.75.0, exploratory
/// lexing mode) imposes, verified empirically during issue #127 and
/// consistent with the constraints documented on the token-only live-editor
/// grammars (issue #196):
/// <list type="number">
/// <item><b>Angle-bracketed references.</b> Production right-hand sides
/// reference other rules as <c>&lt;name&gt;</c>; bare names do not resolve
/// and every path dies at the first token.</item>
/// <item><b>Non-overlapping token rules.</b> Overlapping rules (keyword vs
/// identifier, expression vs block marker) split the token stream
/// multiplicatively and flood the 10-path GLR budget. Whitespace inside
/// tokens is avoided for the same reason: expressions are written
/// <c>{{ name }}</c> so the expression token cannot match the
/// <c>{{sql}}</c> block marker.</item>
/// <item><b>Every character matched by a token rule, punctuation named.</b>
/// Unmatched input silently stops lexing, so brackets, commas and equals
/// are explicit token rules.</item>
/// <item><b>Flat, position-fixed root productions.</b> Recursive or
/// slot-repeated production chains saturate the path budget: right-
/// recursive list rules complete early (the parser stops at the first
/// completed root, so <c>*</c> folds vacuously) and per-slot rule
/// repetition floods near-duplicate paths. The root therefore enumerates
/// the document skeleton once, and each position is a distinct rule with
/// a fixed token shape.</item>
/// </list>
/// This is a bounded-document operating mode: it is how the engine is
/// shipped and used (per-buffer live editing), and the benchmark corpora
/// are collections of such bounded documents.
/// </remarks>
public static class CaseStudyDsls
{
    /// <summary>Configuration DSL grammar (article case study 1 / Appendix A.1).</summary>
    public const string Config =
        """
        Grammar: ConfigCase

        <ws> ::= /\s+/ => { skip }
        <comment> ::= /#[^\n]*/ => { skip }
        <word> ::= /[a-zA-Z_][a-zA-Z0-9_.\-]*/
        <number> ::= /-?[0-9]+(\.[0-9]+)?/
        <string> ::= /"([^"\n]|\\")*"/
        <lbracket> ::= '['
        <rbracket> ::= ']'
        <comma> ::= ','
        <equals> ::= '='
        <section> ::= <lbracket> <word> <rbracket>
        <set_num> ::= <word> <equals> <number>
        <set_word> ::= <word> <equals> <word>
        <set_str> ::= <word> <equals> <string>
        <set_arr> ::= <word> <equals> <lbracket> <number> <comma> <number> <comma> <string> <comma> <word> <rbracket>
        <root> ::= <section> <set_num> <set_word> <set_str> <set_arr> <set_num> <set_word>
        """;

    /// <summary>Analytics query DSL grammar (article case study 2).</summary>
    public const string Query =
        """
        Grammar: QueryCase

        <ws> ::= /\s+/ => { skip }
        <word> ::= /[a-zA-Z_][a-zA-Z0-9_.\-]*/
        <number> ::= /-?[0-9]+(\.[0-9]+)?/
        <string> ::= /"([^"\n]|\\")*"/
        <equals> ::= '='
        <lparen> ::= '('
        <rparen> ::= ')'
        <root> ::= <word> <word> <word> <word> <word> <word> <equals> <string> <word> <number> <word>
        """;

    /// <summary>Multi-language template DSL grammar (article case study 3).</summary>
    /// <remarks>
    /// Embedded blocks are absorbed as single tokens (the greedy block
    /// absorption discipline used by the pinned grammar corpus for prologue-
    /// terminated constructs). The expression token requires a space after
    /// <c>{{</c> so it cannot overlap the <c>{{sql}}</c> marker.
    /// </remarks>
    public const string Template =
        """
        Grammar: TemplateCase

        <ws> ::= /\s+/ => { skip }
        <text> ::= /[a-zA-Z0-9_.!?,-]+/
        <expr> ::= /\{\{ [^}\n]+\}\}/
        <style_block> ::= /<style>[^<]*<\x2Fstyle>/
        <script_block> ::= /<script>[^<]*<\x2Fscript>/
        <sql_block> ::= /\{\{sql\}\}[^{]*\{\{\x2Fsql\}\}/
        <root> ::= <text> <expr> <text> <style_block> <text> <script_block> <sql_block> <text>
        """;
}

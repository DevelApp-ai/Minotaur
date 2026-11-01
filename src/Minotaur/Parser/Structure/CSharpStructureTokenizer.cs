/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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

namespace Minotaur.Parser.Structure;

/// <summary>
/// Deterministic, structure-preserving C# tokenizer.
///
/// Produces the complete token stream of the input - including punctuation
/// tokens (braces, parens, brackets, semicolons, commas) - with exact source
/// spans, never truncating and never silently swallowing malformed input
/// (Minotaur issue #136).
///
/// Rationale: the DevelApp.StepLexer 1.48.0 regex engine cannot express C#
/// string/char literals or operator sets in a single pattern (no grouping,
/// unreliable alternation), and silently stalls when no rule matches. This
/// hand-rolled tokenizer keeps the structure-preserving path fully
/// deterministic and complete while <c>ValidateSourceAsync</c> continues to
/// use the StepParser substrate.
/// </summary>
public static class CSharpStructureTokenizer
{
    private static readonly string[] MultiCharOperators =
    {
        "<=>", "...",
        "<=", ">=", "==", "!=", "&&", "||", "++", "--", "+=", "-=", "*=", "/=", "%=",
        "=>", "??", "?.", "??=", "..", "::",
    };

    private static readonly string[] RawStringDelims = { "\"\"\"\"", "\"\"\"" };

    /// <summary>
    /// Tokenizes the source in a single deterministic pass. Throws
    /// <see cref="StructureParseException"/> on unterminated literals or
    /// comments - truncation is an explicit, reportable error.
    /// </summary>
    public static List<StructureToken> Tokenize(string source)
    {
        ArgumentNullException.ThrowIfNull(source);

        var tokens = new List<StructureToken>();
        int pos = 0;
        int line = 1;
        int col = 1;

        (int, int) Advance(int from, int length)
        {
            var nl = line;
            var cc = col;
            for (int i = 0; i < length; i++)
            {
                var c = source[from + i];
                if (c == '\n') { nl++; cc = 1; }
                else cc++;
            }
            return (nl, cc);
        }

        void Emit(StructureTokenKind kind, int start, int len, int sLine, int sCol)
        {
            tokens.Add(new StructureToken(kind, source.Substring(start, len), start, len, sLine, sCol));
        }

        while (pos < source.Length)
        {
            int start = pos, sLine = line, sCol = col;
            char c = source[pos];

            // Whitespace
            if (char.IsWhiteSpace(c))
            {
                int len = 1;
                while (pos + len < source.Length && char.IsWhiteSpace(source[pos + len])) len++;
                Emit(StructureTokenKind.Whitespace, start, len, sLine, sCol);
                (line, col) = Advance(start, len);
                pos += len;
                continue;
            }

            // Comments
            if (c == '/' && pos + 1 < source.Length)
            {
                if (source[pos + 1] == '/')
                {
                    int len = 2;
                    while (pos + len < source.Length && source[pos + len] != '\n') len++;
                    Emit(StructureTokenKind.LineComment, start, len, sLine, sCol);
                    (line, col) = Advance(start, len);
                    pos += len;
                    continue;
                }
                if (source[pos + 1] == '*')
                {
                    int i = pos + 2;
                    bool closed = false;
                    while (i + 1 < source.Length)
                    {
                        if (source[i] == '*' && source[i + 1] == '/') { closed = true; break; }
                        i++;
                    }
                    if (!closed)
                        throw new StructureParseException("Unterminated block comment", start, sLine, sCol);
                    var len = i + 2 - start;
                    Emit(StructureTokenKind.BlockComment, start, len, sLine, sCol);
                    (line, col) = Advance(start, len);
                    pos += len;
                    continue;
                }
            }

            // Raw string literals (""" or """")
            if (c == '"')
            {
                int? delimLen = null;
                foreach (var d in RawStringDelims)
                {
                    if (pos + d.Length <= source.Length && source.Substring(pos, d.Length) == d)
                    {
                        delimLen = d.Length;
                        break;
                    }
                }
                if (delimLen is int dl)
                {
                    int i = pos + dl;
                    bool closed = false;
                    while (i + dl <= source.Length)
                    {
                        if (source.Substring(i, dl) == new string('"', dl)) { closed = true; break; }
                        i++;
                    }
                    if (!closed)
                        throw new StructureParseException("Unterminated raw string literal", start, sLine, sCol);
                    var len = i + dl - start;
                    Emit(StructureTokenKind.RawStringLiteral, start, len, sLine, sCol);
                    (line, col) = Advance(start, len);
                    pos += len;
                    continue;
                }
            }

            // Verbatim / interpolated-verbatim string literals: $@" or @$"
            if (c is '$' or '@' && pos + 2 < source.Length && source[pos + 1] is '$' or '@' && source[pos + 2] == '"'
                && ((c == '$' && source[pos + 1] == '@') || (c == '@' && source[pos + 1] == '$')))
            {
                int i = pos + 3;
                bool closed = false;
                while (i + 1 < source.Length)
                {
                    if (source[i] == '"')
                    {
                        if (i + 1 >= source.Length) break;
                        if (source[i + 1] == '"') { i += 2; continue; }
                        closed = true;
                        break;
                    }
                    i++;
                }
                if (!closed)
                    throw new StructureParseException("Unterminated verbatim string literal", start, sLine, sCol);
                var len = i + 1 - start;
                Emit(StructureTokenKind.VerbatimStringLiteral, start, len, sLine, sCol);
                (line, col) = Advance(start, len);
                pos += len;
                continue;
            }

            // Plain verbatim string literal: @"
            if (c == '@' && pos + 1 < source.Length && source[pos + 1] == '"')
            {
                int i = pos + 2;
                bool closed = false;
                while (i + 1 < source.Length)
                {
                    if (source[i] == '"')
                    {
                        if (source[i + 1] == '"') { i += 2; continue; }
                        closed = true;
                        break;
                    }
                    i++;
                }
                if (!closed)
                    throw new StructureParseException("Unterminated verbatim string literal", start, sLine, sCol);
                var len = i + 1 - start;
                Emit(StructureTokenKind.VerbatimStringLiteral, start, len, sLine, sCol);
                (line, col) = Advance(start, len);
                pos += len;
                continue;
            }

            // Regular string literal (also covers $"..." interpolated)
            if (c == '"')
            {
                int i = pos + 1;
                bool closed = false;
                while (i < source.Length)
                {
                    if (source[i] == '\\') { i += 2; continue; }
                    if (source[i] == '"') { closed = true; break; }
                    if (source[i] == '\n') break; // literal newlines are not valid in regular strings
                    i++;
                }
                if (!closed)
                    throw new StructureParseException("Unterminated string literal", start, sLine, sCol);
                var len = i + 1 - start;
                Emit(StructureTokenKind.StringLiteral, start, len, sLine, sCol);
                (line, col) = Advance(start, len);
                pos += len;
                continue;
            }

            // Char literal
            if (c == '\'')
            {
                int i = pos + 1;
                bool closed = false;
                while (i < source.Length)
                {
                    if (source[i] == '\\') { i += 2; continue; }
                    if (source[i] == '\'') { closed = true; break; }
                    if (source[i] == '\n') break;
                    i++;
                }
                if (!closed)
                    throw new StructureParseException("Unterminated character literal", start, sLine, sCol);
                var len = i + 1 - start;
                Emit(StructureTokenKind.CharLiteral, start, len, sLine, sCol);
                (line, col) = Advance(start, len);
                pos += len;
                continue;
            }

            // Numbers (hex, real, integer with suffixes)
            if (char.IsDigit(c) || (c == '.' && pos + 1 < source.Length && char.IsDigit(source[pos + 1])))
            {
                int len = 0;
                if (c == '0' && pos + 1 < source.Length && (source[pos + 1] is 'x' or 'X'))
                {
                    len = 2;
                    while (pos + len < source.Length && Uri.IsHexDigit(source[pos + len])) len++;
                    while (pos + len < source.Length && char.IsLetterOrDigit(source[pos + len]) &&
                           source[pos + len] is 'u' or 'U' or 'l' or 'L') len++;
                }
                else
                {
                    while (pos + len < source.Length && char.IsDigit(source[pos + len])) len++;
                    if (pos + len < source.Length && source[pos + len] == '.' &&
                        pos + len + 1 < source.Length && char.IsDigit(source[pos + len + 1]))
                    {
                        len++;
                        while (pos + len < source.Length && char.IsDigit(source[pos + len])) len++;
                    }
                    if (pos + len < source.Length && source[pos + len] is 'e' or 'E')
                    {
                        int save = len;
                        int j = len + 1;
                        if (j < source.Length && source[pos + j] is '+' or '-') j++;
                        if (j < source.Length && char.IsDigit(source[pos + j]))
                        {
                            len = j;
                            while (pos + len < source.Length && char.IsDigit(source[pos + len])) len++;
                        }
                        else len = save;
                    }
                    while (pos + len < source.Length && source[pos + len] is 'f' or 'F' or 'd' or 'D'
                        or 'm' or 'M' or 'u' or 'U' or 'l' or 'L') len++;
                }
                Emit(StructureTokenKind.Number, start, len, sLine, sCol);
                (line, col) = Advance(start, len);
                pos += len;
                continue;
            }

            // Identifiers and keywords (verbatim identifiers included)
            if (char.IsLetter(c) || c == '_' || (c == '@' && pos + 1 < source.Length && (char.IsLetter(source[pos + 1]) || source[pos + 1] == '_')))
            {
                int len = c == '@' ? 2 : 1;
                while (pos + len < source.Length && (char.IsLetterOrDigit(source[pos + len]) || source[pos + len] == '_')) len++;
                Emit(StructureTokenKind.Identifier, start, len, sLine, sCol);
                (line, col) = Advance(start, len);
                pos += len;
                continue;
            }

            // Multi-char operators
            var opLen = 0;
            foreach (var op in MultiCharOperators)
            {
                if (op.Length > opLen && pos + op.Length <= source.Length &&
                    source.Substring(pos, op.Length) == op) opLen = op.Length;
            }
            if (opLen > 0)
            {
                Emit(StructureTokenKind.Operator, start, opLen, sLine, sCol);
                (line, col) = Advance(start, opLen);
                pos += opLen;
                continue;
            }

            // Punctuation with structure meaning
            if (c is '{' or '}' or '(' or ')' or '[' or ']' or ';' or ',' or '.' or ':' or '?')
            {
                Emit(StructureTokenKind.Punctuation, start, 1, sLine, sCol);
                (line, col) = Advance(start, 1);
                pos += 1;
                continue;
            }

            // Single-char operators
            Emit(StructureTokenKind.Operator, start, 1, sLine, sCol);
            (line, col) = Advance(start, 1);
            pos += 1;
        }

        return tokens;
    }
}

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
/// The kind of a structure token produced by <see cref="CSharpStructureTokenizer"/>.
/// </summary>
public enum StructureTokenKind
{
    /// <summary>Whitespace (spaces, tabs, newlines) - skippable.</summary>
    Whitespace,

    /// <summary>A single-line comment (<c>// ...</c>).</summary>
    LineComment,

    /// <summary>A delimited comment (<c>/* ... */</c>).</summary>
    BlockComment,

    /// <summary>A regular string literal (<c>"..."</c>), including interpolated forms.</summary>
    StringLiteral,

    /// <summary>A verbatim string literal (<c>@"..."</c>).</summary>
    VerbatimStringLiteral,

    /// <summary>A raw string literal (<c>"""..."""</c>).</summary>
    RawStringLiteral,

    /// <summary>A character literal (<c>'...'</c>).</summary>
    CharLiteral,

    /// <summary>A numeric literal (integer, real, hex).</summary>
    Number,

    /// <summary>An identifier or keyword (verbatim identifiers included).</summary>
    Identifier,

    /// <summary>Punctuation: braces, parens, brackets, semicolon, comma, dot, colon, question, at.</summary>
    Punctuation,

    /// <summary>An operator symbol.</summary>
    Operator,
}

/// <summary>
/// A single, fully attributed token of source text. Tokens carry exact source
/// spans so downstream consumers can group them into invocations and argument
/// lists without re-scanning the source (Minotaur issue #136).
/// </summary>
public sealed class StructureToken
{
    /// <summary>The token kind.</summary>
    public StructureTokenKind Kind { get; }

    /// <summary>The verbatim source text of the token, exactly as written.</summary>
    public string Text { get; }

    /// <summary>The source start offset (UTF-16 code unit offset) of the token.</summary>
    public int StartPosition { get; }

    /// <summary>The length of the token in source text.</summary>
    public int Length { get; }

    /// <summary>The one-based line where the token starts.</summary>
    public int Line { get; }

    /// <summary>The one-based column where the token starts.</summary>
    public int Column { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StructureToken"/> class.
    /// </summary>
    public StructureToken(StructureTokenKind kind, string text, int startPosition, int length, int line, int column)
    {
        Kind = kind;
        Text = text;
        StartPosition = startPosition;
        Length = length;
        Line = line;
        Column = column;
    }

    /// <summary>The source end offset (exclusive) of the token.</summary>
    public int EndPosition => StartPosition + Length;

    /// <inheritdoc />
    public override string ToString() => $"[{Kind}] '{Text}' @{StartPosition}+{Length}";
}

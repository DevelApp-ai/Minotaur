/*
 * This file is part of Minotaur.
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * Minotaur is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU Affero General Public License for more details.
 * You should have received a copy of the GNU Affero General Public License
 * along with Minotaur. If not, see <https://www.gnu.org/licenses/>. 
 */

using System;
using System.Collections.Generic;

namespace Minotaur.Core.Models.Diff;

/// <summary>
/// Types of diff changes.
/// </summary>
public enum DiffChangeType
{
    /// <summary>No change (equal)</summary>
    Equal,
    /// <summary>Line/text was inserted</summary>
    Inserted,
    /// <summary>Line/text was deleted</summary>
    Deleted,
    /// <summary>Line/text was modified</summary>
    Modified
}

/// <summary>
/// Represents a single change in a diff.
/// </summary>
public class DiffChange
{
    /// <summary>
    /// Gets or sets the type of change.
    /// </summary>
    public DiffChangeType Type { get; set; } = DiffChangeType.Equal;

    /// <summary>
    /// Gets or sets the line number in the old text (1-based, -1 if not applicable).
    /// </summary>
    public int OldLineNumber { get; set; } = -1;

    /// <summary>
    /// Gets or sets the line number in the new text (1-based, -1 if not applicable).
    /// </summary>
    public int NewLineNumber { get; set; } = -1;

    /// <summary>
    /// Gets or sets the text of the change.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token-level changes within this line change.
    /// </summary>
    public List<TokenDiff> TokenChanges { get; set; } = new List<TokenDiff>();

    /// <summary>
    /// Gets whether this change is an insertion.
    /// </summary>
    public bool IsInserted => Type == DiffChangeType.Inserted;

    /// <summary>
    /// Gets whether this change is a deletion.
    /// </summary>
    public bool IsDeleted => Type == DiffChangeType.Deleted;

    /// <summary>
    /// Gets whether this change is a modification.
    /// </summary>
    public bool IsModified => Type == DiffChangeType.Modified;

    /// <summary>
    /// Gets whether this change is equal.
    /// </summary>
    public bool IsEqual => Type == DiffChangeType.Equal;

    /// <summary>
    /// Creates a string representation of this change.
    /// </summary>
    public override string ToString()
    {
        var oldLine = OldLineNumber > 0 ? OldLineNumber.ToString() : "-";
        var newLine = NewLineNumber > 0 ? NewLineNumber.ToString() : "-";
        return $"{Type}: {oldLine} -> {newLine}: {Text}";
    }

    /// <summary>
    /// Creates a copy of this change.
    /// </summary>
    public DiffChange Clone()
    {
        return new DiffChange
        {
            Type = Type,
            OldLineNumber = OldLineNumber,
            NewLineNumber = NewLineNumber,
            Text = Text,
            TokenChanges = new List<TokenDiff>(TokenChanges)
        };
    }
}

/// <summary>
/// Represents a token-level difference.
/// </summary>
public class TokenDiff
{
    /// <summary>
    /// Gets or sets the type of change.
    /// </summary>
    public DiffChangeType Type { get; set; } = DiffChangeType.Equal;

    /// <summary>
    /// Gets or sets the index of the token in the old text (-1 if not applicable).
    /// </summary>
    public int OldTokenIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the index of the token in the new text (-1 if not applicable).
    /// </summary>
    public int NewTokenIndex { get; set; } = -1;

    /// <summary>
    /// Gets or sets the token.
    /// </summary>
    public Token Token { get; set; } = null;

    /// <summary>
    /// Creates a string representation of this token diff.
    /// </summary>
    public override string ToString()
    {
        return $"{Type}: {OldTokenIndex} -> {NewTokenIndex}: {Token?.Value}";
    }

    /// <summary>
    /// Creates a copy of this token diff.
    /// </summary>
    public TokenDiff Clone()
    {
        return new TokenDiff
        {
            Type = Type,
            OldTokenIndex = OldTokenIndex,
            NewTokenIndex = NewTokenIndex,
            Token = Token?.Clone()
        };
    }
}

/// <summary>
/// Represents the result of a diff operation.
/// </summary>
public class DiffResult
{
    /// <summary>
    /// Gets or sets the old text.
    /// </summary>
    public string OldText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the new text.
    /// </summary>
    public string NewText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language ID.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of changes.
    /// </summary>
    public List<DiffChange> Changes { get; set; } = new List<DiffChange>();

    /// <summary>
    /// Gets the number of lines in the old text.
    /// </summary>
    public int OldLineCount { get; set; } = 0;

    /// <summary>
    /// Gets the number of lines in the new text.
    /// </summary>
    public int NewLineCount { get; set; } = 0;

    /// <summary>
    /// Gets the number of insertions.
    /// </summary>
    public int InsertionCount => Changes.Count(c => c.Type == DiffChangeType.Inserted);

    /// <summary>
    /// Gets the number of deletions.
    /// </summary>
    public int DeletionCount => Changes.Count(c => c.Type == DiffChangeType.Deleted);

    /// <summary>
    /// Gets whether there are any changes.
    /// </summary>
    public bool HasChanges => Changes.Any(c => c.Type != DiffChangeType.Equal);

    /// <summary>
    /// Gets the list of added lines.
    /// </summary>
    public List<DiffChange> AddedLines => Changes.FindAll(c => c.Type == DiffChangeType.Inserted);

    /// <summary>
    /// Gets the list of removed lines.
    /// </summary>
    public List<DiffChange> RemovedLines => Changes.FindAll(c => c.Type == DiffChangeType.Deleted);

    /// <summary>
    /// Gets the list of unchanged lines.
    /// </summary>
    public List<DiffChange> UnchangedLines => Changes.FindAll(c => c.Type == DiffChangeType.Equal);

    /// <summary>
    /// Creates a string representation of this result.
    /// </summary>
    public override string ToString()
    {
        return $"DiffResult: {OldLineCount} -> {NewLineCount} lines, {InsertionCount} insertions, {DeletionCount} deletions";
    }

    /// <summary>
    /// Creates a copy of this result.
    /// </summary>
    public DiffResult Clone()
    {
        return new DiffResult
        {
            OldText = OldText,
            NewText = NewText,
            LanguageId = LanguageId,
            Changes = Changes.ConvertAll(c => c.Clone()),
            OldLineCount = OldLineCount,
            NewLineCount = NewLineCount
        };
    }
}

/// <summary>
/// Types of tokens for syntax-aware diffing.
/// </summary>
public enum TokenType
{
    /// <summary>Unknown token type</summary>
    Unknown,
    /// <summary>Whitespace</summary>
    Whitespace,
    /// <summary>Keyword</summary>
    Keyword,
    /// <summary>Identifier</summary>
    Identifier,
    /// <summary>String literal</summary>
    String,
    /// <summary>Character literal</summary>
    Character,
    /// <summary>Numeric literal</summary>
    Number,
    /// <summary>Comment</summary>
    Comment,
    /// <summary>Operator</summary>
    Operator,
    /// <summary>Punctuation</summary>
    Punctuation,
    /// <summary>Template string (JavaScript/TypeScript)</summary>
    TemplateString
}

/// <summary>
/// Represents a token in source code.
/// </summary>
public class Token
{
    /// <summary>
    /// Gets or sets the type of the token.
    /// </summary>
    public TokenType Type { get; set; } = TokenType.Unknown;

    /// <summary>
    /// Gets or sets the value of the token.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the start position of the token in the source text.
    /// </summary>
    public int StartPosition { get; set; } = 0;

    /// <summary>
    /// Gets or sets the length of the token.
    /// </summary>
    public int Length { get; set; } = 0;

    /// <summary>
    /// Gets or sets the line number where the token starts (1-based).
    /// </summary>
    public int Line { get; set; } = 0;

    /// <summary>
    /// Gets or sets the column number where the token starts (1-based).
    /// </summary>
    public int Column { get; set; } = 0;

    /// <summary>
    /// Gets the end position of the token.
    /// </summary>
    public int EndPosition => StartPosition + Length;

    /// <summary>
    /// Gets whether this token is whitespace.
    /// </summary>
    public bool IsWhitespace => Type == TokenType.Whitespace;

    /// <summary>
    /// Creates a string representation of this token.
    /// </summary>
    public override string ToString()
    {
        return $"{Type}: '{Value}' at {Line}:{Column}";
    }

    /// <summary>
    /// Creates a copy of this token.
    /// </summary>
    public Token Clone()
    {
        return new Token
        {
            Type = Type,
            Value = Value,
            StartPosition = StartPosition,
            Length = Length,
            Line = Line,
            Column = Column
        };
    }
}

/// <summary>
/// Pattern for matching tokens.
/// </summary>
public class TokenPattern
{
    /// <summary>
    /// Gets or sets the type of token to match.
    /// </summary>
    public TokenType Type { get; set; } = TokenType.Unknown;

    /// <summary>
    /// Gets or sets the regular expression pattern.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this pattern can match across multiple lines.
    /// </summary>
    public bool IsMultiline { get; set; } = false;

    /// <summary>
    /// Gets or sets the priority of this pattern (higher priority patterns are matched first).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Creates a copy of this pattern.
    /// </summary>
    public TokenPattern Clone()
    {
        return new TokenPattern
        {
            Type = Type,
            Pattern = Pattern,
            IsMultiline = IsMultiline,
            Priority = Priority
        };
    }
}

/// <summary>
/// Tokenizer for splitting source code into tokens.
/// </summary>
public class Tokenizer
{
    private readonly List<TokenPattern> _patterns;

    /// <summary>
    /// Initializes a new instance of the Tokenizer.
    /// </summary>
    public Tokenizer(List<TokenPattern> patterns)
    {
        _patterns = patterns ?? new List<TokenPattern>();
        // Sort by priority (descending)
        _patterns.Sort((a, b) => b.Priority.CompareTo(a.Priority));
    }

    /// <summary>
    /// Tokenizes the given text.
    /// </summary>
    public List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        
        if (string.IsNullOrEmpty(text))
            return tokens;

        var position = 0;
        var line = 1;
        var column = 1;

        while (position < text.Length)
        {
            var matched = false;

            // Try to match each pattern
            foreach (var pattern in _patterns)
            {
                var match = MatchPattern(text, position, pattern);
                if (match != null)
                {
                    // Skip whitespace before the match
                    if (match.Start > position)
                    {
                        tokens.Add(CreateWhitespaceToken(text, position, match.Start - position, line, column));
                        column += match.Start - position;
                        position = match.Start;
                    }

                    tokens.Add(new Token
                    {
                        Type = pattern.Type,
                        Value = match.Value,
                        StartPosition = match.Start,
                        Length = match.Length,
                        Line = line,
                        Column = column
                    });

                    column += match.Length;
                    position = match.Start + match.Length;
                    matched = true;
                    break;
                }
            }

            // If no pattern matched, skip one character
            if (!matched)
            {
                tokens.Add(new Token
                {
                    Type = TokenType.Unknown,
                    Value = text[position].ToString(),
                    StartPosition = position,
                    Length = 1,
                    Line = line,
                    Column = column
                });

                column++;
                position++;
            }

            // Update line and column for newlines
            if (position < text.Length && text[position - 1] == '\n')
            {
                line++;
                column = 1;
            }
        }

        return tokens;
    }

    /// <summary>
    /// Matches a pattern at the given position.
    /// </summary>
    private System.Text.RegularExpressions.Match MatchPattern(string text, int position, TokenPattern pattern)
    {
        if (string.IsNullOrEmpty(pattern.Pattern))
            return null;

        var options = System.Text.RegularExpressions.RegexOptions.None;
        if (pattern.IsMultiline)
            options |= System.Text.RegularExpressions.RegexOptions.Multiline;

        var regex = new System.Text.RegularExpressions.Regex(pattern.Pattern, options);
        
        // Try to match at the current position
        var match = regex.Match(text, position);
        
        if (match.Success && match.Index == position)
            return match;

        return null;
    }

    /// <summary>
    /// Creates a whitespace token.
    /// </summary>
    private Token CreateWhitespaceToken(string text, int start, int length, int line, int column)
    {
        var whitespace = text.Substring(start, length);
        var newlineCount = whitespace.Count(c => c == '\n');
        var lastNewline = whitespace.LastIndexOf('\n');

        return new Token
        {
            Type = TokenType.Whitespace,
            Value = whitespace,
            StartPosition = start,
            Length = length,
            Line = line,
            Column = column
        };
    }

    /// <summary>
    /// Gets the patterns used by this tokenizer.
    /// </summary>
    public List<TokenPattern> GetPatterns()
    {
        return new List<TokenPattern>(_patterns);
    }

    /// <summary>
    /// Adds a pattern to this tokenizer.
    /// </summary>
    public void AddPattern(TokenPattern pattern)
    {
        if (pattern != null)
        {
            _patterns.Add(pattern);
            _patterns.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }
}

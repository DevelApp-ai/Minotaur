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
using System.Text.RegularExpressions;

namespace Minotaur.Core.Models.Editor;

/// <summary>
/// Token types for syntax highlighting.
/// </summary>
public enum TokenType
{
    /// <summary>Default token type</summary>
    Default,
    /// <summary>Language keywords (if, else, for, while, etc.)</summary>
    Keyword,
    /// <summary>Type names (int, string, custom types)</summary>
    Type,
    /// <summary>String literals</summary>
    String,
    /// <summary>Numeric literals</summary>
    Number,
    /// <summary>Comments (single-line and multi-line)</summary>
    Comment,
    /// <summary>Character literals</summary>
    Character,
    /// <summary>Boolean literals (true, false)</summary>
    Boolean,
    /// <summary>Null literal</summary>
    Null,
    /// <summary>Identifiers (variable names, function names, etc.)</summary>
    Identifier,
    /// <summary>Operators (+, -, *, /, etc.)</summary>
    Operator,
    /// <summary>Punctuation (bracers, parentheses, etc.)</summary>
    Punctuation,
    /// <summary>Preprocessor directives</summary>
    Preprocessor,
    /// <summary>Function declarations</summary>
    Function,
    /// <summary>Variable declarations</summary>
    Variable
}

/// <summary>
/// Theme types for syntax highlighting.
/// </summary>
public enum Theme
{
    /// <summary>Light theme (default)</summary>
    Light,
    /// <summary>Dark theme</summary>
    Dark,
    /// <summary>System theme (follows OS)</summary>
    System
}

/// <summary>
/// Color scheme for syntax highlighting.
/// </summary>
public class ColorScheme
{
    // Light theme colors
    public string KeywordColorLight { get; set; } = "#0000FF";
    public string TypeColorLight { get; set; } = "#2B91AF";
    public string StringColorLight { get; set; } = "#A31515";
    public string NumberColorLight { get; set; } = "#098658";
    public string CommentColorLight { get; set; } = "#008000";
    public string CharacterColorLight { get; set; } = "#A31515";
    public string BooleanColorLight { get; set; } = "#0000FF";
    public string NullColorLight { get; set; } = "#000080";
    public string IdentifierColorLight { get; set; } = "#000000";
    public string OperatorColorLight { get; set; } = "#000000";
    public string PunctuationColorLight { get; set; } = "#000000";
    public string PreprocessorColorLight { get; set; } = "#800080";
    public string FunctionColorLight { get; set; } = "#74531F";
    public string VariableColorLight { get; set; } = "#000000";
    public string DefaultColorLight { get; set; } = "#000000";

    // Dark theme colors
    public string KeywordColorDark { get; set; } = "#569CD6";
    public string TypeColorDark { get; set; } = "#4EC9B0";
    public string StringColorDark { get; set; } = "#CE9178";
    public string NumberColorDark { get; set; } = "#B5CEA8";
    public string CommentColorDark { get; set; } = "#6A9955";
    public string CharacterColorDark { get; set; } = "#CE9178";
    public string BooleanColorDark { get; set; } = "#569CD6";
    public string NullColorDark { get; set; } = "#569CD6";
    public string IdentifierColorDark { get; set; } = "#9CDCFE";
    public string OperatorColorDark { get; set; } = "#D4D4D4";
    public string PunctuationColorDark { get; set; } = "#D4D4D4";
    public string PreprocessorColorDark { get; set; } = "#9CDCFE";
    public string FunctionColorDark { get; set; } = "#DCDCAA";
    public string VariableColorDark { get; set; } = "#9CDCFE";
    public string DefaultColorDark { get; set; } = "#D4D4D4";

    /// <summary>
    /// Creates a default color scheme.
    /// </summary>
    public static ColorScheme Default => new ColorScheme();

    /// <summary>
    /// Creates a Visual Studio-like color scheme.
    /// </summary>
    public static ColorScheme VisualStudio => new ColorScheme
    {
        KeywordColorLight = "#0000FF",
        TypeColorLight = "#2B91AF",
        StringColorLight = "#A31515",
        NumberColorLight = "#098658",
        CommentColorLight = "#008000",
        KeywordColorDark = "#569CD6",
        TypeColorDark = "#4EC9B0",
        StringColorDark = "#CE9178",
        NumberColorDark = "#B5CEA8",
        CommentColorDark = "#6A9955"
    };

    /// <summary>
    /// Creates a Monokai color scheme.
    /// </summary>
    public static ColorScheme Monokai => new ColorScheme
    {
        KeywordColorLight = "#F92672",
        TypeColorLight = "#A6E22E",
        StringColorLight = "#E6DB74",
        NumberColorLight = "#AE81FF",
        CommentColorLight = "#75715E",
        KeywordColorDark = "#F92672",
        TypeColorDark = "#A6E22E",
        StringColorDark = "#E6DB74",
        NumberColorDark = "#AE81FF",
        CommentColorDark = "#75715E"
    };

    /// <summary>
    /// Creates a Solarized color scheme.
    /// </summary>
    public static ColorScheme Solarized => new ColorScheme
    {
        KeywordColorLight = "#B58900",
        TypeColorLight = "#268BD2",
        StringColorLight = "#DC322F",
        NumberColorLight = "#D33682",
        CommentColorLight = "#657B83",
        KeywordColorDark = "#B58900",
        TypeColorDark = "#268BD2",
        StringColorDark = "#DC322F",
        NumberColorDark = "#D33682",
        CommentColorDark = "#657B83"
    };
}

/// <summary>
/// Configuration for syntax highlighting.
/// </summary>
public class SyntaxHighlightingConfiguration
{
    /// <summary>
    /// Gets or sets whether syntax highlighting is enabled.
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the theme (Light, Dark, or System).
    /// </summary>
    public Theme Theme { get; set; } = Theme.Light;

    /// <summary>
    /// Gets or sets the color scheme.
    /// </summary>
    public ColorScheme ColorScheme { get; set; } = ColorScheme.Default;

    /// <summary>
    /// Gets or sets the default language ID.
    /// </summary>
    public string DefaultLanguageId { get; set; } = "csharp";

    /// <summary>
    /// Gets or sets whether to use semantic highlighting (if available).
    /// </summary>
    public bool UseSemanticHighlighting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to highlight matching brackets.
    /// </summary>
    public bool HighlightMatchingBrackets { get; set; } = true;

    /// <summary>
    /// Gets or sets the bracket highlighting color.
    /// </summary>
    public string BracketColor { get; set; } = "#FFFF00";

    /// <summary>
    /// Gets or sets whether to highlight the current line.
    /// </summary>
    public bool HighlightCurrentLine { get; set; } = true;

    /// <summary>
    /// Gets or sets the current line background color.
    /// </summary>
    public string CurrentLineBackground { get; set; } = "#FFFFDD";

    /// <summary>
    /// Creates a default configuration.
    /// </summary>
    public static SyntaxHighlightingConfiguration Default => new SyntaxHighlightingConfiguration();

    /// <summary>
    /// Creates a Visual Studio-like configuration.
    /// </summary>
    public static SyntaxHighlightingConfiguration VisualStudio => new SyntaxHighlightingConfiguration
    {
        Theme = Theme.Light,
        ColorScheme = ColorScheme.VisualStudio,
        UseSemanticHighlighting = true,
        HighlightMatchingBrackets = true
    };

    /// <summary>
    /// Creates a dark theme configuration.
    /// </summary>
    public static SyntaxHighlightingConfiguration DarkTheme => new SyntaxHighlightingConfiguration
    {
        Theme = Theme.Dark,
        ColorScheme = ColorScheme.VisualStudio,
        UseSemanticHighlighting = true,
        HighlightMatchingBrackets = true,
        CurrentLineBackground = "#2D2D2D"
    };
}

/// <summary>
/// Rule for highlighting a specific token pattern.
/// </summary>
public class TokenHighlightingRule
{
    /// <summary>
    /// Gets or sets the regular expression pattern to match.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token type to apply.
    /// </summary>
    public TokenType TokenType { get; set; } = TokenType.Default;

    /// <summary>
    /// Gets or sets the priority of this rule (higher priority rules are applied first).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this pattern can match across multiple lines.
    /// </summary>
    public bool IsMultiline { get; set; } = false;

    /// <summary>
    /// Gets or sets the CSS class to apply (overrides token type class).
    /// </summary>
    public string CssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the foreground color (overrides color scheme).
    /// </summary>
    public string ForegroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the background color.
    /// </summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this rule is case-sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; set; } = false;

    /// <summary>
    /// Compiles the pattern into a Regex object.
    /// </summary>
    public Regex CompilePattern()
    {
        var options = RegexOptions.Compiled;
        if (!IsCaseSensitive)
            options |= RegexOptions.IgnoreCase;
        if (IsMultiline)
            options |= RegexOptions.Multiline;

        return new Regex(Pattern, options);
    }
}

/// <summary>
/// Highlighting rules for a specific language.
/// </summary>
public class LanguageHighlightingRules
{
    /// <summary>
    /// Gets or sets the unique identifier for the language.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name for the language.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file extensions associated with this language.
    /// </summary>
    public string[] FileExtensions { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the list of token highlighting rules.
    /// </summary>
    public List<TokenHighlightingRule> TokenRules { get; set; } = new List<TokenHighlightingRule>();

    /// <summary>
    /// Gets or sets the default token type for unmatched text.
    /// </summary>
    public TokenType DefaultTokenType { get; set; } = TokenType.Default;

    /// <summary>
    /// Gets or sets whether this language supports semantic highlighting.
    /// </summary>
    public bool SupportsSemanticHighlighting { get; set; } = false;

    /// <summary>
    /// Adds a token highlighting rule.
    /// </summary>
    public void AddRule(TokenHighlightingRule rule)
    {
        if (rule != null)
            TokenRules.Add(rule);
    }

    /// <summary>
    /// Adds multiple token highlighting rules.
    /// </summary>
    public void AddRules(IEnumerable<TokenHighlightingRule> rules)
    {
        if (rules != null)
        {
            foreach (var rule in rules)
            {
                if (rule != null)
                    TokenRules.Add(rule);
            }
        }
    }

    /// <summary>
    /// Removes a token highlighting rule.
    /// </summary>
    public bool RemoveRule(TokenHighlightingRule rule)
    {
        return TokenRules.Remove(rule);
    }

    /// <summary>
    /// Gets a rule by its pattern.
    /// </summary>
    public TokenHighlightingRule GetRuleByPattern(string pattern)
    {
        return TokenRules.Find(r => r.Pattern == pattern);
    }
}

/// <summary>
/// Represents a span of highlighted text.
/// </summary>
public class HighlightSpan
{
    /// <summary>
    /// Gets or sets the starting position of the span in the text.
    /// </summary>
    public int Start { get; set; }

    /// <summary>
    /// Gets or sets the length of the span.
    /// </summary>
    public int Length { get; set; }

    /// <summary>
    /// Gets or sets the token type of the span.
    /// </summary>
    public TokenType TokenType { get; set; } = TokenType.Default;

    /// <summary>
    /// Gets or sets the language ID.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CSS class for this span.
    /// </summary>
    public string CssClass { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the foreground color for this span.
    /// </summary>
    public string ForegroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the background color for this span.
    /// </summary>
    public string BackgroundColor { get; set; } = string.Empty;

    /// <summary>
    /// Gets the end position of the span.
    /// </summary>
    public int End => Start + Length;

    /// <summary>
    /// Gets the text of the span from the source text.
    /// </summary>
    public string GetText(string sourceText)
    {
        if (string.IsNullOrEmpty(sourceText))
            return string.Empty;

        if (Start < 0 || Start >= sourceText.Length)
            return string.Empty;

        var end = Math.Min(Start + Length, sourceText.Length);
        return sourceText.Substring(Start, end - Start);
    }

    /// <summary>
    /// Creates a copy of this span with updated properties.
    /// </summary>
    public HighlightSpan Clone()
    {
        return new HighlightSpan
        {
            Start = Start,
            Length = Length,
            TokenType = TokenType,
            LanguageId = LanguageId,
            CssClass = CssClass,
            ForegroundColor = ForegroundColor,
            BackgroundColor = BackgroundColor
        };
    }
}

/// <summary>
/// Represents text with syntax highlighting applied.
/// </summary>
public class HighlightedText
{
    /// <summary>
    /// Gets or sets the original source text.
    /// </summary>
    public string SourceText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of highlight spans.
    /// </summary>
    public List<HighlightSpan> Spans { get; set; } = new List<HighlightSpan>();

    /// <summary>
    /// Gets or sets the language ID.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the theme used for highlighting.
    /// </summary>
    public Theme Theme { get; set; } = Theme.Light;

    /// <summary>
    /// Gets the HTML representation of the highlighted text.
    /// </summary>
    public string ToHtml(string cssClassPrefix = "token")
    {
        if (string.IsNullOrEmpty(SourceText))
            return string.Empty;

        var html = new System.Text.StringBuilder();
        var lastPosition = 0;

        // Sort spans by start position
        var sortedSpans = new List<HighlightSpan>(Spans);
        sortedSpans.Sort((a, b) => a.Start.CompareTo(b.Start));

        foreach (var span in sortedSpans)
        {
            // Add text before the span
            if (span.Start > lastPosition)
            {
                var text = SourceText.Substring(lastPosition, span.Start - lastPosition);
                html.Append(EscapeHtml(text));
            }

            // Add the span
            var spanText = span.GetText(SourceText);
            var cssClass = string.IsNullOrEmpty(span.CssClass) ? 
                $"{cssClassPrefix}-{span.TokenType.ToString().ToLower()}" : span.CssClass;
            var style = string.Empty;

            if (!string.IsNullOrEmpty(span.ForegroundColor))
                style += $"color: {span.ForegroundColor}; ";
            if (!string.IsNullOrEmpty(span.BackgroundColor))
                style += $"background-color: {span.BackgroundColor}; ";

            if (!string.IsNullOrEmpty(style))
                style = $" style=\"{style.TrimEnd()}\"";

            html.Append($"<span class=\"{cssClass}\"{style}>{EscapeHtml(spanText)}</span>");
            lastPosition = span.End;
        }

        // Add remaining text
        if (lastPosition < SourceText.Length)
        {
            var text = SourceText.Substring(lastPosition);
            html.Append(EscapeHtml(text));
        }

        return html.ToString();
    }

    /// <summary>
    /// Escapes HTML special characters.
    /// </summary>
    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }

    /// <summary>
    /// Creates a new HighlightedText instance.
    /// </summary>
    public HighlightedText(string sourceText, List<HighlightSpan> spans)
    {
        SourceText = sourceText ?? string.Empty;
        Spans = spans ?? new List<HighlightSpan>();
    }

    /// <summary>
    /// Creates a new HighlightedText instance with language and theme.
    /// </summary>
    public HighlightedText(string sourceText, List<HighlightSpan> spans, string languageId, Theme theme)
        : this(sourceText, spans)
    {
        LanguageId = languageId;
        Theme = theme;
    }
}

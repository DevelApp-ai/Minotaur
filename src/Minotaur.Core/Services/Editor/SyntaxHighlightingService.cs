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
using Minotaur.Core.Models;

namespace Minotaur.Core.Services.Editor;

/// <summary>
/// Service for providing language-aware syntax highlighting.
/// Supports multiple languages with customizable themes and color schemes.
/// </summary>
public class SyntaxHighlightingService : ISyntaxHighlightingService
{
    private readonly Dictionary<string, LanguageHighlightingRules> _languageRules;
    private readonly SyntaxHighlightingConfiguration _configuration;
    private readonly Dictionary<string, TokenTypeInfo> _tokenTypeCache = new();

    /// <summary>
    /// Initializes a new instance of the SyntaxHighlightingService.
    /// </summary>
    public SyntaxHighlightingService()
    {
        _languageRules = new Dictionary<string, LanguageHighlightingRules>(StringComparer.OrdinalIgnoreCase);
        _configuration = new SyntaxHighlightingConfiguration();
        InitializeDefaultRules();
    }

    /// <summary>
    /// Initializes a new instance with custom configuration.
    /// </summary>
    public SyntaxHighlightingService(SyntaxHighlightingConfiguration configuration)
    {
        _languageRules = new Dictionary<string, LanguageHighlightingRules>(StringComparer.OrdinalIgnoreCase);
        _configuration = configuration ?? new SyntaxHighlightingConfiguration();
        InitializeDefaultRules();
    }

    /// <summary>
    /// Initializes default highlighting rules for supported languages.
    /// </summary>
    private void InitializeDefaultRules()
    {
        // C# highlighting rules
        _languageRules["csharp"] = new LanguageHighlightingRules
        {
            LanguageId = "csharp",
            DisplayName = "C#",
            FileExtensions = new[] { ".cs", ".csx" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(abstract|as|base|break|case|catch|checked|class|const|continue|default|delegate|do|else|enum|event|explicit|extern|false|finally|fixed|for|foreach|goto|if|implicit|in|interface|internal|is|lock|namespace|new|null|operator|out|override|params|private|protected|public|readonly|ref|return|sealed|sizeof|stackalloc|static|switch|this|throw|true|try|typeof|unchecked|unsafe|using|virtual|void|while|yield)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(bool|byte|char|decimal|double|float|int|long|object|sbyte|short|string|uint|ulong|ushort)\b",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"//.*",
                    TokenType = TokenType.Comment,
                    Priority = 80 },
                new TokenHighlightingRule { Pattern = @"/\*.*\*/",
                    TokenType = TokenType.Comment,
                    Priority = 80,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"\"[^\"]*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"'\\?.'",
                    TokenType = TokenType.Character,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[fFdDmM]?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 },
                new TokenHighlightingRule { Pattern = @"\b[A-Z][a-zA-Z0-9_]*\b",
                    TokenType = TokenType.Type,
                    Priority = 50 },
                new TokenHighlightingRule { Pattern = @"\b[a-z_][a-zA-Z0-9_]*\b",
                    TokenType = TokenType.Identifier,
                    Priority = 40 }
            }
        };

        // Java highlighting rules
        _languageRules["java"] = new LanguageHighlightingRules
        {
            LanguageId = "java",
            DisplayName = "Java",
            FileExtensions = new[] { ".java" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(abstract|assert|boolean|break|byte|case|catch|char|class|const|continue|default|do|double|else|enum|extends|final|finally|float|for|goto|if|implements|import|instanceof|int|interface|long|native|new|package|private|protected|public|return|short|static|strictfp|super|switch|synchronized|this|throw|throws|transient|try|void|volatile|while)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(Boolean|Byte|Character|Double|Float|Integer|Long|Short|String|Void)\b",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"//.*",
                    TokenType = TokenType.Comment,
                    Priority = 80 },
                new TokenHighlightingRule { Pattern = @"/\*.*\*/",
                    TokenType = TokenType.Comment,
                    Priority = 80,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"\"[^\"]*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"'\\?.'",
                    TokenType = TokenType.Character,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[fFdDlL]?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 }
            }
        };

        // JavaScript/TypeScript highlighting rules
        _languageRules["javascript"] = new LanguageHighlightingRules
        {
            LanguageId = "javascript",
            DisplayName = "JavaScript",
            FileExtensions = new[] { ".js", ".mjs", ".jsx" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(break|case|catch|class|const|continue|debugger|default|delete|do|else|enum|export|extends|false|finally|for|function|if|import|in|instanceof|new|null|return|super|switch|this|throw|true|try|typeof|var|void|while|with|yield)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(let|static|package|private|protected|public|interface|implements)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(array|bigint|boolean|number|object|string|symbol|undefined)\b",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"//.*",
                    TokenType = TokenType.Comment,
                    Priority = 80 },
                new TokenHighlightingRule { Pattern = @"/\*.*\*/",
                    TokenType = TokenType.Comment,
                    Priority = 80,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"`[^`]*`",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\"[^\"]*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"'[^']*'",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 },
                new TokenHighlightingRule { Pattern = @"\btrue\b|\bfalse\b",
                    TokenType = TokenType.Boolean,
                    Priority = 55 },
                new TokenHighlightingRule { Pattern = @"\bnull\b|\bundefined\b",
                    TokenType = TokenType.Null,
                    Priority = 55 }
            }
        };

        _languageRules["typescript"] = new LanguageHighlightingRules
        {
            LanguageId = "typescript",
            DisplayName = "TypeScript",
            FileExtensions = new[] { ".ts", ".tsx" },
            TokenRules = new List<TokenHighlightingRule>(_languageRules["javascript"].TokenRules)
            {
                new TokenHighlightingRule { Pattern = @"\b(any|boolean|never|number|object|string|symbol|undefined|unknown|void)\b",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"\b(interface|type|namespace|module|enum|implements|extends|public|private|protected)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 }
            }
        };

        // Python highlighting rules
        _languageRules["python"] = new LanguageHighlightingRules
        {
            LanguageId = "python",
            DisplayName = "Python",
            FileExtensions = new[] { ".py", ".pyw" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(False|None|True|and|as|assert|async|await|break|class|continue|def|del|elif|else|except|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|raise|return|try|while|with|yield)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"#.*",
                    TokenType = TokenType.Comment,
                    Priority = 80 },
                new TokenHighlightingRule { Pattern = @"\"\"\"[^\"\"\"]*\"\"\"",
                    TokenType = TokenType.String,
                    Priority = 70,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"\"[^\"\\]*(?:\\.[^\"\\]*)*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"'[^'\\]*(?:\\.[^'\\]*)*'",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?j?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 },
                new TokenHighlightingRule { Pattern = @"\b(self|cls)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 95 },
                new TokenHighlightingRule { Pattern = @"\b[A-Z][a-zA-Z0-9_]*\b",
                    TokenType = TokenType.Type,
                    Priority = 50 }
            }
        };

        // COBOL highlighting rules
        _languageRules["cobol"] = new LanguageHighlightingRules
        {
            LanguageId = "cobol",
            DisplayName = "COBOL",
            FileExtensions = new[] { ".cob", ".cbl" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(ACCEPT|ADD|ALTER|CALL|CANCEL|CLOSE|COMPUTE|CONTINUE|DELETE|DISPLAY|DIVIDE|ENTRY|EXIT|GO TO|IF|INITIALIZE|INSPECT|MERGE|MOVE|MULTIPLY|OPEN|PERFORM|READ|RELEASE|RETURN|REWRITE|SEARCH|SET|SORT|START|STOP|STRING|SUBTRACT|UNLOCK|WRITE)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(DATA|ENVIRONMENT|IDENTIFICATION|PROCEDURE)\b\s+DIVISION",
                    TokenType = TokenType.Keyword,
                    Priority = 110 },
                new TokenHighlightingRule { Pattern = @"\b(WORKING-STORAGE|LOCAL-STORAGE|LINKAGE|FILE)\b\s+SECTION",
                    TokenType = TokenType.Keyword,
                    Priority = 105 },
                new TokenHighlightingRule { Pattern = @"\*.*",
                    TokenType = TokenType.Comment,
                    Priority = 80 },
                new TokenHighlightingRule { Pattern = @"/\*.*\*/",
                    TokenType = TokenType.Comment,
                    Priority = 80,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"\"[^\"]*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"PIC\s+[X9SZV]+[(\d+)]*",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 }
            }
        };

        // PL/I highlighting rules
        _languageRules["pli"] = new LanguageHighlightingRules
        {
            LanguageId = "pli",
            DisplayName = "PL/I",
            FileExtensions = new[] { ".pli", ".pl1" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(ALLOCATE|CALL|CLOSE|DEALLOCATE|DECLARE|DO|ELSE|END|ENTRY|EXIT|FREE|GET|GO TO|IF|OPEN|ON|PUT|RETURN|SELECT|SIGNAL|STOP|THEN|WAIT|WHEN|WRITE)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(BINARY|CHARACTER|DECIMAL|FLOAT|FIXED|GRAPHIC|LABEL|OFFSET|POINTER|RECORD)\b",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"\/\*.*\*\/",
                    TokenType = TokenType.Comment,
                    Priority = 80,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"\"[^\"]*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 }
            }
        };

        // Rust highlighting rules
        _languageRules["rust"] = new LanguageHighlightingRules
        {
            LanguageId = "rust",
            DisplayName = "Rust",
            FileExtensions = new[] { ".rs" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(as|async|await|break|const|continue|crate|dyn|else|enum|extern|false|fn|for|if|impl|in|let|loop|match|mod|move|mut|pub|ref|return|self|Self|static|struct|super|trait|true|type|unsafe|use|where|while)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(bool|char|f32|f64|i8|i16|i32|i64|i128|isize|u8|u16|u32|u64|u128|usize|str)\b",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"//.*",
                    TokenType = TokenType.Comment,
                    Priority = 80 },
                new TokenHighlightingRule { Pattern = @"\/\*.*\*\/",
                    TokenType = TokenType.Comment,
                    Priority = 80,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"\"[^\"\\]*(?:\\.[^\"\\]*)*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"r#\"[^\"]*\"#",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?(f32|f64|i8|i16|i32|i64|i128|isize|u8|u16|u32|u64|u128|usize)?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 },
                new TokenHighlightingRule { Pattern = @"'\\?.'",
                    TokenType = TokenType.Character,
                    Priority = 70 }
            }
        };

        // Go highlighting rules
        _languageRules["go"] = new LanguageHighlightingRules
        {
            LanguageId = "go",
            DisplayName = "Go",
            FileExtensions = new[] { ".go" },
            TokenRules = new List<TokenHighlightingRule>
            {
                new TokenHighlightingRule { Pattern = @"\b(break|case|chan|const|continue|default|defer|else|fallthrough|for|func|go|goto|if|import|interface|map|package|range|return|select|struct|switch|type|var)\b",
                    TokenType = TokenType.Keyword,
                    Priority = 100 },
                new TokenHighlightingRule { Pattern = @"\b(bool|byte|complex64|complex128|error|float32|float64|int|int8|int16|int32|int64|rune|string|uint|uint8|uint16|uint32|uint64|uintptr)\b",
                    TokenType = TokenType.Type,
                    Priority = 90 },
                new TokenHighlightingRule { Pattern = @"//.*",
                    TokenType = TokenType.Comment,
                    Priority = 80 },
                new TokenHighlightingRule { Pattern = @"/\*.*\*/",
                    TokenType = TokenType.Comment,
                    Priority = 80,
                    IsMultiline = true },
                new TokenHighlightingRule { Pattern = @"\"[^\"\\]*(?:\\.[^\"\\]*)*\"",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"`[^`]*`",
                    TokenType = TokenType.String,
                    Priority = 70 },
                new TokenHighlightingRule { Pattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?i?\b",
                    TokenType = TokenType.Number,
                    Priority = 60 },
                new TokenHighlightingRule { Pattern = @"\b(true|false)\b",
                    TokenType = TokenType.Boolean,
                    Priority = 55 },
                new TokenHighlightingRule { Pattern = @"\b(nil)\b",
                    TokenType = TokenType.Null,
                    Priority = 55 }
            }
        };
    }

    /// <summary>
    /// Gets the highlighting rules for a specific language.
    /// </summary>
    public LanguageHighlightingRules GetLanguageRules(string languageId)
    {
        if (string.IsNullOrEmpty(languageId))
            return null;

        if (_languageRules.TryGetValue(languageId, out var rules))
            return rules;

        // Try to find by file extension
        foreach (var lr in _languageRules.Values)
        {
            if (lr.FileExtensions != null && Array.IndexOf(lr.FileExtensions, languageId) >= 0)
                return lr;
        }

        return null;
    }

    /// <summary>
    /// Gets all supported languages for syntax highlighting.
    /// </summary>
    public IEnumerable<LanguageHighlightingRules> GetSupportedLanguages()
    {
        return _languageRules.Values;
    }

    /// <summary>
    /// Adds custom highlighting rules for a language.
    /// </summary>
    public void AddLanguageRules(LanguageHighlightingRules rules)
    {
        if (rules == null || string.IsNullOrEmpty(rules.LanguageId))
            return;

        _languageRules[rules.LanguageId] = rules;
        _tokenTypeCache.Clear();
    }

    /// <summary>
    /// Removes highlighting rules for a language.
    /// </summary>
    public void RemoveLanguageRules(string languageId)
    {
        if (string.IsNullOrEmpty(languageId))
            return;

        _languageRules.Remove(languageId);
        _tokenTypeCache.Clear();
    }

    /// <summary>
    /// Applies syntax highlighting to the given source code.
    /// </summary>
    public HighlightedText ApplyHighlighting(string sourceCode, string languageId)
    {
        var rules = GetLanguageRules(languageId);
        if (rules == null)
            return new HighlightedText(sourceCode, new List<HighlightSpan>());

        return ApplyHighlighting(sourceCode, rules);
    }

    /// <summary>
    /// Applies syntax highlighting to the given source code using specific rules.
    /// </summary>
    public HighlightedText ApplyHighlighting(string sourceCode, LanguageHighlightingRules rules)
    {
        if (string.IsNullOrEmpty(sourceCode) || rules == null)
            return new HighlightedText(sourceCode, new List<HighlightSpan>());

        var spans = new List<HighlightSpan>();
        var text = sourceCode;

        // Sort rules by priority (descending) to ensure higher priority rules match first
        var sortedRules = new List<TokenHighlightingRule>(rules.TokenRules);
        sortedRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        foreach (var rule in sortedRules)
        {
            var matches = rule.Pattern.Matches(text);
            foreach (Match match in matches)
            {
                // Check if this span overlaps with existing spans
                var start = match.Index;
                var length = match.Length;
                var end = start + length;

                bool overlaps = false;
                foreach (var existing in spans)
                {
                    var existingEnd = existing.Start + existing.Length;
                    if (start < existingEnd && end > existing.Start)
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    spans.Add(new HighlightSpan
                    {
                        Start = start,
                        Length = length,
                        TokenType = rule.TokenType,
                        LanguageId = rules.LanguageId
                    });
                }
            }
        }

        // Sort spans by start position
        spans.Sort((a, b) => a.Start.CompareTo(b.Start));

        return new HighlightedText(sourceCode, spans);
    }

    /// <summary>
    /// Gets the CSS class for a token type based on the current theme.
    /// </summary>
    public string GetCssClass(TokenType tokenType)
    {
        var theme = _configuration.Theme;
        var colorScheme = _configuration.ColorScheme;

        return GetCssClass(tokenType, theme, colorScheme);
    }

    /// <summary>
    /// Gets the CSS class for a token type with specific theme and color scheme.
    /// </summary>
    public string GetCssClass(TokenType tokenType, Theme theme, ColorScheme colorScheme)
    {
        var baseClass = GetBaseCssClass(tokenType);
        
        if (theme == Theme.Dark)
            return $"{baseClass} dark-theme";
        else if (theme == Theme.Light)
            return $"{baseClass} light-theme";
        
        return baseClass;
    }

    /// <summary>
    /// Gets the base CSS class for a token type.
    /// </summary>
    private string GetBaseCssClass(TokenType tokenType)
    {
        return tokenType switch
        {
            TokenType.Keyword => "token-keyword",
            TokenType.Type => "token-type",
            TokenType.String => "token-string",
            TokenType.Number => "token-number",
            TokenType.Comment => "token-comment",
            TokenType.Character => "token-character",
            TokenType.Boolean => "token-boolean",
            TokenType.Null => "token-null",
            TokenType.Identifier => "token-identifier",
            TokenType.Operator => "token-operator",
            TokenType.Punctuation => "token-punctuation",
            TokenType.Preprocessor => "token-preprocessor",
            TokenType.Function => "token-function",
            TokenType.Variable => "token-variable",
            _ => "token-default"
        };
    }

    /// <summary>
    /// Gets the color for a token type based on the current theme and color scheme.
    /// </summary>
    public string GetColor(TokenType tokenType)
    {
        var theme = _configuration.Theme;
        var colorScheme = _configuration.ColorScheme;

        return GetColor(tokenType, theme, colorScheme);
    }

    /// <summary>
    /// Gets the color for a token type with specific theme and color scheme.
    /// </summary>
    public string GetColor(TokenType tokenType, Theme theme, ColorScheme colorScheme)
    {
        if (theme == Theme.Dark)
        {
            return tokenType switch
            {
                TokenType.Keyword => colorScheme.KeywordColorDark,
                TokenType.Type => colorScheme.TypeColorDark,
                TokenType.String => colorScheme.StringColorDark,
                TokenType.Number => colorScheme.NumberColorDark,
                TokenType.Comment => colorScheme.CommentColorDark,
                TokenType.Character => colorScheme.CharacterColorDark,
                TokenType.Boolean => colorScheme.BooleanColorDark,
                TokenType.Null => colorScheme.NullColorDark,
                TokenType.Identifier => colorScheme.IdentifierColorDark,
                TokenType.Operator => colorScheme.OperatorColorDark,
                TokenType.Punctuation => colorScheme.PunctuationColorDark,
                TokenType.Preprocessor => colorScheme.PreprocessorColorDark,
                TokenType.Function => colorScheme.FunctionColorDark,
                TokenType.Variable => colorScheme.VariableColorDark,
                _ => colorScheme.DefaultColorDark
            };
        }
        else
        {
            return tokenType switch
            {
                TokenType.Keyword => colorScheme.KeywordColorLight,
                TokenType.Type => colorScheme.TypeColorLight,
                TokenType.String => colorScheme.StringColorLight,
                TokenType.Number => colorScheme.NumberColorLight,
                TokenType.Comment => colorScheme.CommentColorLight,
                TokenType.Character => colorScheme.CharacterColorLight,
                TokenType.Boolean => colorScheme.BooleanColorLight,
                TokenType.Null => colorScheme.NullColorLight,
                TokenType.Identifier => colorScheme.IdentifierColorLight,
                TokenType.Operator => colorScheme.OperatorColorLight,
                TokenType.Punctuation => colorScheme.PunctuationColorLight,
                TokenType.Preprocessor => colorScheme.PreprocessorColorLight,
                TokenType.Function => colorScheme.FunctionColorLight,
                TokenType.Variable => colorScheme.VariableColorLight,
                _ => colorScheme.DefaultColorLight
            };
        }
    }

    /// <summary>
    /// Updates the configuration.
    /// </summary>
    public void UpdateConfiguration(SyntaxHighlightingConfiguration configuration)
    {
        if (configuration != null)
        {
            _configuration.Theme = configuration.Theme;
            _configuration.ColorScheme = configuration.ColorScheme;
            _configuration.Enabled = configuration.Enabled;
        }
    }

    /// <summary>
    /// Gets the current configuration.
    /// </summary>
    public SyntaxHighlightingConfiguration GetConfiguration()
    {
        return new SyntaxHighlightingConfiguration
        {
            Theme = _configuration.Theme,
            ColorScheme = _configuration.ColorScheme,
            Enabled = _configuration.Enabled
        };
    }
}

/// <summary>
/// Interface for syntax highlighting service.
/// </summary>
public interface ISyntaxHighlightingService
{
    /// <summary>
    /// Gets the highlighting rules for a specific language.
    /// </summary>
    LanguageHighlightingRules GetLanguageRules(string languageId);

    /// <summary>
    /// Gets all supported languages for syntax highlighting.
    /// </summary>
    IEnumerable<LanguageHighlightingRules> GetSupportedLanguages();

    /// <summary>
    /// Applies syntax highlighting to the given source code.
    /// </summary>
    HighlightedText ApplyHighlighting(string sourceCode, string languageId);

    /// <summary>
    /// Applies syntax highlighting to the given source code using specific rules.
    /// </summary>
    HighlightedText ApplyHighlighting(string sourceCode, LanguageHighlightingRules rules);

    /// <summary>
    /// Gets the CSS class for a token type.
    /// </summary>
    string GetCssClass(TokenType tokenType);

    /// <summary>
    /// Gets the color for a token type.
    /// </summary>
    string GetColor(TokenType tokenType);
}

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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Minotaur.Core.Services.Diff;

/// <summary>
/// Service for computing and visualizing code differences.
/// Provides side-by-side and inline diff views with syntax-aware diffing.
/// </summary>
public class CodeDiffService : ICodeDiffService
{
    private readonly Dictionary<string, Tokenizer> _languageTokenizers;

    /// <summary>
    /// Initializes a new instance of the CodeDiffService.
    /// </summary>
    public CodeDiffService()
    {
        _languageTokenizers = new Dictionary<string, Tokenizer>(StringComparer.OrdinalIgnoreCase);
        InitializeTokenizers();
    }

    /// <summary>
    /// Initializes tokenizers for supported languages.
    /// </summary>
    private void InitializeTokenizers()
    {
        // Add tokenizers for supported languages
        _languageTokenizers["csharp"] = new Tokenizer(GetCSharpTokens());
        _languageTokenizers["java"] = new Tokenizer(GetJavaTokens());
        _languageTokenizers["javascript"] = new Tokenizer(GetJavaScriptTokens());
        _languageTokenizers["typescript"] = new Tokenizer(GetTypeScriptTokens());
        _languageTokenizers["python"] = new Tokenizer(GetPythonTokens());
        _languageTokenizers["cobol"] = new Tokenizer(GetCobolTokens());
        _languageTokenizers["pli"] = new Tokenizer(GetPLITokens());
        _languageTokenizers["rust"] = new Tokenizer(GetRustTokens());
        _languageTokenizers["go"] = new Tokenizer(GetGoTokens());
    }

    /// <summary>
    /// Gets tokens for C#.
    /// </summary>
    private List<TokenPattern> GetCSharpTokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(abstract|as|base|break|case|catch|checked|class|const|continue|default|delegate|do|else|enum|event|explicit|extern|false|finally|fixed|for|foreach|goto|if|implicit|in|interface|internal|is|lock|namespace|new|null|operator|out|override|params|private|protected|public|readonly|ref|return|sealed|sizeof|stackalloc|static|switch|this|throw|true|try|typeof|unchecked|unsafe|using|virtual|void|while|yield)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"//.*" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"/\*.*\*/", IsMultiline = true },
            new TokenPattern { Type = TokenType.String, Pattern = @"\"[^\"]*\"" },
            new TokenPattern { Type = TokenType.Character, Pattern = @"'\\?.'" }
        };
    }

    /// <summary>
    /// Gets tokens for Java.
    /// </summary>
    private List<TokenPattern> GetJavaTokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(abstract|assert|boolean|break|byte|case|catch|char|class|const|continue|default|do|double|else|enum|extends|final|finally|float|for|goto|if|implements|import|instanceof|int|interface|long|native|new|package|private|protected|public|return|short|static|strictfp|super|switch|synchronized|this|throw|throws|transient|try|void|volatile|while)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"//.*" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"/\*.*\*/", IsMultiline = true },
            new TokenPattern { Type = TokenType.String, Pattern = @"\"[^\"]*\"" },
            new TokenPattern { Type = TokenType.Character, Pattern = @"'\\?.'" }
        };
    }

    /// <summary>
    /// Gets tokens for JavaScript.
    /// </summary>
    private List<TokenPattern> GetJavaScriptTokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(break|case|catch|class|const|continue|debugger|default|delete|do|else|enum|export|extends|false|finally|for|function|if|import|in|instanceof|new|null|return|super|switch|this|throw|true|try|typeof|var|void|while|with|yield)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"//.*" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"/\*.*\*/", IsMultiline = true },
            new TokenPattern { Type = TokenType.String, Pattern = @"\"[^\"]*\"" },
            new TokenPattern { Type = TokenType.String, Pattern = @"'`[^`]*`" },
            new TokenPattern { Type = TokenType.TemplateString, Pattern = @"`[^`]*`" }
        };
    }

    /// <summary>
    /// Gets tokens for TypeScript.
    /// </summary>
    private List<TokenPattern> GetTypeScriptTokens()
    {
        return new List<TokenPattern>(GetJavaScriptTokens())
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(interface|type|namespace|module|enum|implements|extends|public|private|protected)\b" }
        };
    }

    /// <summary>
    /// Gets tokens for Python.
    /// </summary>
    private List<TokenPattern> GetPythonTokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(False|None|True|and|as|assert|async|await|break|class|continue|def|del|elif|else|except|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|raise|return|try|while|with|yield)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"#.*" },
            new TokenPattern { Type = TokenType.String, Pattern = @"\"\"\"[^\"\"\"]*\"\"\"", IsMultiline = true },
            new TokenPattern { Type = TokenType.String, Pattern = @"\"[^\"\\]*(?:\\.[^\"\\]*)*\"" },
            new TokenPattern { Type = TokenType.String, Pattern = @"'[^'\\]*(?:\\.[^'\\]*)*'" }
        };
    }

    /// <summary>
    /// Gets tokens for COBOL.
    /// </summary>
    private List<TokenPattern> GetCobolTokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(ACCEPT|ADD|ALTER|CALL|CANCEL|CLOSE|COMPUTE|CONTINUE|DELETE|DISPLAY|DIVIDE|ENTRY|EXIT|GO TO|IF|INITIALIZE|INSPECT|MERGE|MOVE|MULTIPLY|OPEN|PERFORM|READ|RELEASE|RETURN|REWRITE|SEARCH|SET|SORT|START|STOP|STRING|SUBTRACT|UNLOCK|WRITE)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"\*.*" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"/\*.*\*/", IsMultiline = true }
        };
    }

    /// <summary>
    /// Gets tokens for PL/I.
    /// </summary>
    private List<TokenPattern> GetPLITokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(ALLOCATE|CALL|CLOSE|DEALLOCATE|DECLARE|DO|ELSE|END|ENTRY|EXIT|FREE|GET|GO TO|IF|OPEN|ON|PUT|RETURN|SELECT|SIGNAL|STOP|THEN|WAIT|WHEN|WRITE)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"\/\*.*\*\/" }
        };
    }

    /// <summary>
    /// Gets tokens for Rust.
    /// </summary>
    private List<TokenPattern> GetRustTokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(as|async|await|break|const|continue|crate|dyn|else|enum|extern|false|fn|for|if|impl|in|let|loop|match|mod|move|mut|pub|ref|return|self|Self|static|struct|super|trait|true|type|unsafe|use|where|while)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"//.*" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"\/\*.*\*\/" }
        };
    }

    /// <summary>
    /// Gets tokens for Go.
    /// </summary>
    private List<TokenPattern> GetGoTokens()
    {
        return new List<TokenPattern>
        {
            new TokenPattern { Type = TokenType.Keyword, Pattern = @"\b(break|case|chan|const|continue|default|defer|else|fallthrough|for|func|go|goto|if|import|interface|map|package|range|return|select|struct|switch|type|var)\b" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"//.*" },
            new TokenPattern { Type = TokenType.Comment, Pattern = @"/\*.*\*/" }
        };
    }

    /// <summary>
    /// Computes the difference between two strings.
    /// </summary>
    public DiffResult ComputeDiff(string oldText, string newText, string languageId = null)
    {
        var oldLines = SplitLines(oldText);
        var newLines = SplitLines(newText);

        // Use LCS (Longest Common Subsequence) algorithm for diff
        var lcs = FindLCS(oldLines, newLines);
        
        // Build diff from LCS
        var diff = BuildDiffFromLCS(oldLines, newLines, lcs);

        // Apply syntax-aware diffing if language is specified
        if (!string.IsNullOrEmpty(languageId) && _languageTokenizers.TryGetValue(languageId, out var tokenizer))
        {
            ApplySyntaxAwareDiff(diff, oldText, newText, tokenizer);
        }

        return new DiffResult
        {
            OldText = oldText,
            NewText = newText,
            LanguageId = languageId,
            Changes = diff,
            OldLineCount = oldLines.Count,
            NewLineCount = newLines.Count
        };
    }

    /// <summary>
    /// Splits text into lines.
    /// </summary>
    private List<string> SplitLines(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new List<string>();

        return text.Split('\n').ToList();
    }

    /// <summary>
    /// Finds the Longest Common Subsequence between two lists.
    /// </summary>
    private List<int> FindLCS(List<string> oldLines, List<string> newLines)
    {
        var m = oldLines.Count;
        var n = newLines.Count;
        var lcsLength = new int[m + 1, n + 1];

        // Build LCS length table
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (oldLines[i - 1] == newLines[j - 1])
                {
                    lcsLength[i, j] = lcsLength[i - 1, j - 1] + 1;
                }
                else
                {
                    lcsLength[i, j] = Math.Max(lcsLength[i - 1, j], lcsLength[i, j - 1]);
                }
            }
        }

        // Backtrack to find LCS
        var lcs = new List<int>();
        int i2 = m, j2 = n;
        while (i2 > 0 && j2 > 0)
        {
            if (oldLines[i2 - 1] == newLines[j2 - 1])
            {
                lcs.Insert(0, i2 - 1);
                i2--;
                j2--;
            }
            else if (lcsLength[i2 - 1, j2] > lcsLength[i2, j2 - 1])
            {
                i2--;
            }
            else
            {
                j2--;
            }
        }

        return lcs;
    }

    /// <summary>
    /// Builds diff from LCS.
    /// </summary>
    private List<DiffChange> BuildDiffFromLCS(List<string> oldLines, List<string> newLines, List<int> lcsIndices)
    {
        var changes = new List<DiffChange>();
        var oldIndex = 0;
        var newIndex = 0;
        var lcsIndex = 0;

        while (oldIndex < oldLines.Count || newIndex < newLines.Count)
        {
            if (lcsIndex < lcsIndices.Count && oldIndex == lcsIndices[lcsIndex] && newIndex == lcsIndices[lcsIndex])
            {
                // Common line
                changes.Add(new DiffChange
                {
                    Type = DiffChangeType.Equal,
                    OldLineNumber = oldIndex + 1,
                    NewLineNumber = newIndex + 1,
                    Text = oldLines[oldIndex]
                });
                oldIndex++;
                newIndex++;
                lcsIndex++;
            }
            else if (newIndex < newLines.Count && (lcsIndex >= lcsIndices.Count || newIndex != lcsIndices[lcsIndex]))
            {
                // Added line
                changes.Add(new DiffChange
                {
                    Type = DiffChangeType.Inserted,
                    OldLineNumber = -1,
                    NewLineNumber = newIndex + 1,
                    Text = newLines[newIndex]
                });
                newIndex++;
            }
            else if (oldIndex < oldLines.Count)
            {
                // Deleted line
                changes.Add(new DiffChange
                {
                    Type = DiffChangeType.Deleted,
                    OldLineNumber = oldIndex + 1,
                    NewLineNumber = -1,
                    Text = oldLines[oldIndex]
                });
                oldIndex++;
            }
        }

        return changes;
    }

    /// <summary>
    /// Applies syntax-aware diffing to refine the diff.
    /// </summary>
    private void ApplySyntaxAwareDiff(List<DiffChange> changes, string oldText, string newText, Tokenizer tokenizer)
    {
        // Group consecutive changes
        var groups = GroupConsecutiveChanges(changes);

        foreach (var group in groups)
        {
            if (group.Count == 1 && group[0].Type == DiffChangeType.Equal)
                continue;

            // Get the range of lines affected
            var startOldLine = group.First().OldLineNumber;
            var endOldLine = group.Last().OldLineNumber;
            var startNewLine = group.First().NewLineNumber;
            var endNewLine = group.Last().NewLineNumber;

            // Extract the old and new text for this range
            var oldRangeText = GetLines(oldText, startOldLine, endOldLine);
            var newRangeText = GetLines(newText, startNewLine, endNewLine);

            // Tokenize both
            var oldTokens = tokenizer.Tokenize(oldRangeText);
            var newTokens = tokenizer.Tokenize(newRangeText);

            // Find token-level differences
            var tokenDiff = ComputeTokenDiff(oldTokens, newTokens);

            // Update changes with token-level info
            UpdateChangesWithTokenDiff(changes, group, tokenDiff);
        }
    }

    /// <summary>
    /// Groups consecutive changes.
    /// </summary>
    private List<List<DiffChange>> GroupConsecutiveChanges(List<DiffChange> changes)
    {
        var groups = new List<List<DiffChange>>();
        var currentGroup = new List<DiffChange>();

        foreach (var change in changes)
        {
            if (currentGroup.Count == 0)
            {
                currentGroup.Add(change);
            }
            else if (change.Type == currentGroup.Last().Type ||
                     (currentGroup.Last().Type != DiffChangeType.Equal && change.Type != DiffChangeType.Equal))
            {
                currentGroup.Add(change);
            }
            else
            {
                groups.Add(currentGroup);
                currentGroup = new List<DiffChange> { change };
            }
        }

        if (currentGroup.Count > 0)
            groups.Add(currentGroup);

        return groups;
    }

    /// <summary>
    /// Gets lines from text.
    /// </summary>
    private string GetLines(string text, int startLine, int endLine)
    {
        if (string.IsNullOrEmpty(text) || startLine < 1 || endLine < startLine)
            return string.Empty;

        var lines = text.Split('\n');
        var sb = new StringBuilder();

        for (int i = startLine - 1; i < Math.Min(endLine, lines.Length); i++)
        {
            if (i > startLine - 1)
                sb.Append('\n');
            sb.Append(lines[i]);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Computes token-level diff.
    /// </summary>
    private List<TokenDiff> ComputeTokenDiff(List<Token> oldTokens, List<Token> newTokens)
    {
        var diff = new List<TokenDiff>();
        var oldIndex = 0;
        var newIndex = 0;

        while (oldIndex < oldTokens.Count || newIndex < newTokens.Count)
        {
            if (oldIndex < oldTokens.Count && newIndex < newTokens.Count &&
                oldTokens[oldIndex].Value == newTokens[newIndex].Value &&
                oldTokens[oldIndex].Type == newTokens[newIndex].Type)
            {
                // Equal token
                diff.Add(new TokenDiff
                {
                    Type = DiffChangeType.Equal,
                    OldTokenIndex = oldIndex,
                    NewTokenIndex = newIndex,
                    Token = oldTokens[oldIndex]
                });
                oldIndex++;
                newIndex++;
            }
            else if (newIndex < newTokens.Count && (oldIndex >= oldTokens.Count ||
                   (newIndex < newTokens.Count && oldIndex < oldTokens.Count &&
                    newTokens[newIndex].StartPosition < oldTokens[oldIndex].StartPosition)))
            {
                // Inserted token
                diff.Add(new TokenDiff
                {
                    Type = DiffChangeType.Inserted,
                    OldTokenIndex = -1,
                    NewTokenIndex = newIndex,
                    Token = newTokens[newIndex]
                });
                newIndex++;
            }
            else if (oldIndex < oldTokens.Count)
            {
                // Deleted token
                diff.Add(new TokenDiff
                {
                    Type = DiffChangeType.Deleted,
                    OldTokenIndex = oldIndex,
                    NewTokenIndex = -1,
                    Token = oldTokens[oldIndex]
                });
                oldIndex++;
            }
        }

        return diff;
    }

    /// <summary>
    /// Updates changes with token-level diff info.
    /// </summary>
    private void UpdateChangesWithTokenDiff(List<DiffChange> changes, List<DiffChange> group, List<TokenDiff> tokenDiff)
    {
        // For each change in the group, add token-level info
        foreach (var change in group)
        {
            // Find token diffs that correspond to this line
            var lineTokenDiffs = tokenDiff.Where(td => 
                (change.OldLineNumber > 0 && td.OldTokenIndex >= 0 && 
                 GetLineFromPosition(change.OldText, td.Token.StartPosition) == change.OldLineNumber) ||
                (change.NewLineNumber > 0 && td.NewTokenIndex >= 0 &&
                 GetLineFromPosition(change.Text, td.Token.StartPosition) == change.NewLineNumber)).ToList();

            change.TokenChanges = lineTokenDiffs;
        }
    }

    /// <summary>
    /// Gets line number from position in text.
    /// </summary>
    private int GetLineFromPosition(string text, int position)
    {
        if (string.IsNullOrEmpty(text) || position < 0)
            return 1;

        var lines = text.Split('\n');
        var currentPos = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            if (position >= currentPos && position < currentPos + lines[i].Length)
                return i + 1;
            currentPos += lines[i].Length + 1; // +1 for newline
        }

        return lines.Length;
    }

    /// <summary>
    /// Formats the diff as side-by-side HTML.
    /// </summary>
    public string FormatAsSideBySideHtml(DiffResult diffResult, string cssClassPrefix = "diff")
    {
        var sb = new StringBuilder();
        var oldLineNum = 1;
        var newLineNum = 1;

        sb.AppendLine("<div class=\"" + cssClassPrefix + "-container\">");
        sb.AppendLine("  <div class=\"" + cssClassPrefix + "-header\">");
        sb.AppendLine("    <div class=\"" + cssClassPrefix + "-old-header\">Old</div>");
        sb.AppendLine("    <div class=\"" + cssClassPrefix + "-new-header\">New</div>");
        sb.AppendLine("  </div>");
        sb.AppendLine("  <div class=\"" + cssClassPrefix + "-content\">");

        foreach (var change in diffResult.Changes)
        {
            // Old line
            sb.Append("    <div class=\"" + cssClassPrefix + "-line");
            if (change.OldLineNumber > 0)
            {
                sb.Append(" " + cssClassPrefix + "-old-line");
                sb.Append("\" data-line=\"" + change.OldLineNumber + "\">");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-number\">" + change.OldLineNumber + "</div>");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-content");
                
                if (change.Type == DiffChangeType.Deleted)
                    sb.Append(" " + cssClassPrefix + "-deleted\"");
                else
                    sb.Append("\"");
                
                sb.Append(">" + EscapeHtml(change.Text) + "</div>");
                sb.Append("    </div>");
            }
            else
            {
                sb.Append("\">");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-number\"></div>");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-content\"></div>");
                sb.Append("    </div>");
            }

            // New line
            sb.Append("    <div class=\"" + cssClassPrefix + "-line");
            if (change.NewLineNumber > 0)
            {
                sb.Append(" " + cssClassPrefix + "-new-line");
                sb.Append("\" data-line=\"" + change.NewLineNumber + "\">");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-number\">" + change.NewLineNumber + "</div>");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-content");
                
                if (change.Type == DiffChangeType.Inserted)
                    sb.Append(" " + cssClassPrefix + "-inserted\"");
                else
                    sb.Append("\"");
                
                sb.Append(">" + EscapeHtml(change.NewLineNumber > 0 ? change.Text : "") + "</div>");
                sb.Append("    </div>");
            }
            else
            {
                sb.Append("\">");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-number\"></div>");
                sb.Append("      <div class=\"" + cssClassPrefix + "-line-content\"></div>");
                sb.Append("    </div>");
            }
        }

        sb.AppendLine("  </div>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }

    /// <summary>
    /// Formats the diff as inline HTML.
    /// </summary>
    public string FormatAsInlineHtml(DiffResult diffResult, string cssClassPrefix = "diff")
    {
        var sb = new StringBuilder();

        sb.AppendLine("<div class=\"" + cssClassPrefix + "-inline-container\">");

        foreach (var change in diffResult.Changes)
        {
            if (change.Type == DiffChangeType.Equal)
            {
                sb.Append("  <div class=\"" + cssClassPrefix + "-line\">");
                sb.Append("    <span class=\"" + cssClassPrefix + "-line-number\">" + change.OldLineNumber + "</span>");
                sb.Append("    <span class=\"" + cssClassPrefix + "-line-content\">" + EscapeHtml(change.Text) + "</span>");
                sb.Append("  </div>");
            }
            else if (change.Type == DiffChangeType.Deleted)
            {
                sb.Append("  <div class=\"" + cssClassPrefix + "-line " + cssClassPrefix + "-deleted\">");
                sb.Append("    <span class=\"" + cssClassPrefix + "-line-number\">" + change.OldLineNumber + "</span>");
                sb.Append("    <span class=\"" + cssClassPrefix + "-line-content\">- " + EscapeHtml(change.Text) + "</span>");
                sb.Append("  </div>");
            }
            else if (change.Type == DiffChangeType.Inserted)
            {
                sb.Append("  <div class=\"" + cssClassPrefix + "-line " + cssClassPrefix + "-inserted\">");
                sb.Append("    <span class=\"" + cssClassPrefix + "-line-number\">" + change.NewLineNumber + "</span>");
                sb.Append("    <span class=\"" + cssClassPrefix + "-line-content\">+ " + EscapeHtml(change.Text) + "</span>");
                sb.Append("  </div>");
            }
        }

        sb.AppendLine("</div>");

        return sb.ToString();
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
    /// Gets the tokenizer for a language.
    /// </summary>
    public Tokenizer GetTokenizer(string languageId)
    {
        if (string.IsNullOrEmpty(languageId))
            return null;

        _languageTokenizers.TryGetValue(languageId, out var tokenizer);
        return tokenizer;
    }

    /// <summary>
    /// Tokenizes text using the specified language's tokenizer.
    /// </summary>
    public List<Token> Tokenize(string text, string languageId)
    {
        var tokenizer = GetTokenizer(languageId);
        if (tokenizer == null)
            return new List<Token>();

        return tokenizer.Tokenize(text);
    }
}

/// <summary>
/// Interface for code diff service.
/// </summary>
public interface ICodeDiffService
{
    /// <summary>
    /// Computes the difference between two strings.
    /// </summary>
    DiffResult ComputeDiff(string oldText, string newText, string languageId = null);

    /// <summary>
    /// Formats the diff as side-by-side HTML.
    /// </summary>
    string FormatAsSideBySideHtml(DiffResult diffResult, string cssClassPrefix = "diff");

    /// <summary>
    /// Formats the diff as inline HTML.
    /// </summary>
    string FormatAsInlineHtml(DiffResult diffResult, string cssClassPrefix = "diff");

    /// <summary>
    /// Gets the tokenizer for a language.
    /// </summary>
    Tokenizer GetTokenizer(string languageId);

    /// <summary>
    /// Tokenizes text using the specified language's tokenizer.
    /// </summary>
    List<Token> Tokenize(string text, string languageId);
}

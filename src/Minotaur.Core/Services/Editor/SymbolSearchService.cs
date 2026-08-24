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
using System.Text.RegularExpressions;

namespace Minotaur.Core.Services.Editor;

/// <summary>
/// Service for performing global symbol search across codebase.
/// Provides functionality to search for symbols (variables, functions, classes, etc.)
/// and navigate to their definitions and references.
/// </summary>
public class SymbolSearchService : ISymbolSearchService
{
    private readonly Dictionary<string, List<SymbolInfo>> _symbolCache = new();
    private readonly Dictionary<string, List<SymbolReference>> _referenceCache = new();
    private readonly Dictionary<string, SymbolSearchIndex> _indexCache = new();

    /// <summary>
    /// Initializes a new instance of the SymbolSearchService.
    /// </summary>
    public SymbolSearchService()
    {
    }

    /// <summary>
    /// Builds a symbol index for the given source code.
    /// </summary>
    public SymbolSearchIndex BuildIndex(string sourceCode, string languageId, string filePath = null)
    {
        var index = new SymbolSearchIndex
        {
            FilePath = filePath ?? "unknown",
            LanguageId = languageId,
            Symbols = new List<SymbolInfo>(),
            References = new List<SymbolReference>()
        };

        if (string.IsNullOrEmpty(sourceCode))
            return index;

        var languageRules = GetLanguageSymbolRules(languageId);
        var lines = sourceCode.Split('\n');

        // Find all symbol definitions
        foreach (var rule in languageRules.SymbolPatterns)
        {
            var matches = Regex.Matches(sourceCode, rule.Pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var symbolName = match.Groups[1].Value;
                    var symbolType = rule.SymbolType;
                    var startLine = CountLinesBefore(sourceCode, match.Index);
                    var startColumn = GetColumn(sourceCode, match.Index);
                    var endLine = CountLinesBefore(sourceCode, match.Index + match.Length);
                    var endColumn = GetColumn(sourceCode, match.Index + match.Length);

                    // Check if this symbol already exists
                    var existing = index.Symbols.FirstOrDefault(s => 
                        s.Name == symbolName && 
                        s.Line == startLine && 
                        s.Column == startColumn);

                    if (existing == null)
                    {
                        index.Symbols.Add(new SymbolInfo
                        {
                            Name = symbolName,
                            Type = symbolType,
                            FilePath = filePath,
                            Line = startLine + 1, // 1-based
                            Column = startColumn + 1,
                            StartPosition = match.Index,
                            EndPosition = match.Index + match.Length,
                            LanguageId = languageId,
                            Scope = DetermineScope(sourceCode, match.Index, languageId)
                        });
                    }
                }
            }
        }

        // Find all symbol references
        foreach (var rule in languageRules.ReferencePatterns)
        {
            var matches = Regex.Matches(sourceCode, rule.Pattern, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            
            foreach (Match match in matches)
            {
                if (match.Groups.Count > 1)
                {
                    var symbolName = match.Groups[1].Value;
                    var startLine = CountLinesBefore(sourceCode, match.Index);
                    var startColumn = GetColumn(sourceCode, match.Index);

                    // Find the matching symbol definition
                    var symbol = index.Symbols.FirstOrDefault(s => s.Name == symbolName);

                    index.References.Add(new SymbolReference
                    {
                        SymbolName = symbolName,
                        FilePath = filePath,
                        Line = startLine + 1,
                        Column = startColumn + 1,
                        StartPosition = match.Index,
                        EndPosition = match.Index + match.Length,
                        LanguageId = languageId,
                        SymbolInfo = symbol
                    });
                }
            }
        }

        // Cache the index
        if (!string.IsNullOrEmpty(filePath))
        {
            _indexCache[filePath] = index;
        }

        return index;
    }

    /// <summary>
    /// Gets symbol search rules for a specific language.
    /// </summary>
    private LanguageSymbolRules GetLanguageSymbolRules(string languageId)
    {
        // Default rules for all languages
        var defaultRules = new LanguageSymbolRules
        {
            LanguageId = languageId,
            SymbolPatterns = new List<SymbolPattern>
            {
                // Match words (identifiers)
                new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier },
                // Match quoted strings
                new SymbolPattern { Pattern = @"\"([^\"]*)\"", SymbolType = SymbolType.String },
                // Match numbers
                new SymbolPattern { Pattern = @"\b(\d+(\.\d+)?([eE][+-]?\d+)?)\b", SymbolType = SymbolType.Number }
            },
            ReferencePatterns = new List<SymbolPattern>
            {
                new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier }
            }
        };

        // Language-specific rules
        switch (languageId?.ToLower())
        {
            case "csharp":
                return new LanguageSymbolRules
                {
                    LanguageId = "csharp",
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        // Class definitions
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|internal|static|abstract|sealed|partial)\s+(class)\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Class },
                        // Interface definitions
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|internal|static|partial)\s+(interface)\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Interface },
                        // Struct definitions
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|internal|static|partial)\s+(struct)\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Struct },
                        // Enum definitions
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|internal|static|partial)\s+(enum)\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Enum },
                        // Method definitions
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|internal|static|async|override|virtual|abstract|sealed|new)\s+.*\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", SymbolType = SymbolType.Method },
                        // Property definitions
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|internal|static|virtual|abstract|sealed|new)\s+.*\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*{\s*get\s*;", SymbolType = SymbolType.Property },
                        // Field definitions
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|internal|static|readonly|const|volatile)\s+.*\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*[;=]", SymbolType = SymbolType.Field },
                        // Local variable definitions
                        new SymbolPattern { Pattern = @"\b(var|let|const)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*[;=]", SymbolType = SymbolType.Variable },
                        // Parameter definitions
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*[,)]", SymbolType = SymbolType.Parameter }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier },
                        new SymbolPattern { Pattern = @"\b(this\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field },
                        new SymbolPattern { Pattern = @"\b(base\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field }
                    }
                };

            case "java":
                return new LanguageSymbolRules
                {
                    LanguageId = "java",
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|static|final|abstract|strictfp|native)\s+(class)\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Class },
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|static|final|abstract)\s+(interface)\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Interface },
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|static|final|abstract)\s+.*\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", SymbolType = SymbolType.Method },
                        new SymbolPattern { Pattern = @"^\s*(public|private|protected|static|final|volatile)\s+.*\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*[;=]", SymbolType = SymbolType.Field },
                        new SymbolPattern { Pattern = @"\b(var|final|let)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*[;=]", SymbolType = SymbolType.Variable }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier },
                        new SymbolPattern { Pattern = @"\b(this\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field },
                        new SymbolPattern { Pattern = @"\b(super\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field }
                    }
                };

            case "javascript":
            case "typescript":
                return new LanguageSymbolRules
                {
                    LanguageId = languageId,
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"^\s*function\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*const\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*function\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*let\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*function\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*var\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*function\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*class\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*{", SymbolType = SymbolType.Class },
                        new SymbolPattern { Pattern = @"^\s*interface\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*{", SymbolType = SymbolType.Interface },
                        new SymbolPattern { Pattern = @"^\s*const\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=", SymbolType = SymbolType.Constant },
                        new SymbolPattern { Pattern = @"^\s*let\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*var\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*export\s+\w+\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*", SymbolType = SymbolType.Export }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier },
                        new SymbolPattern { Pattern = @"\b(this\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field },
                        new SymbolPattern { Pattern = @"\b(window\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Global }
                    }
                };

            case "python":
                return new LanguageSymbolRules
                {
                    LanguageId = "python",
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"^\s*class\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*:", SymbolType = SymbolType.Class },
                        new SymbolPattern { Pattern = @"^\s*def\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=\s*lambda\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*import\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Import },
                        new SymbolPattern { Pattern = @"^\s*from\s+\w+\s+import\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Import },
                        new SymbolPattern { Pattern = @"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*=", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*@([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Decorator }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier },
                        new SymbolPattern { Pattern = @"\b(self\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field }
                    }
                };

            case "cobol":
                return new LanguageSymbolRules
                {
                    LanguageId = "cobol",
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"^\s*01\s+([a-zA-Z0-9-]+)", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*77\s+([a-zA-Z0-9-]+)", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*([a-zA-Z0-9-]+)\.\s*", SymbolType = SymbolType.Paragraph },
                        new SymbolPattern { Pattern = @"^\s*FD\s+([a-zA-Z0-9-]+)", SymbolType = SymbolType.File },
                        new SymbolPattern { Pattern = @"^\s*SD\s+([a-zA-Z0-9-]+)", SymbolType = SymbolType.File },
                        new SymbolPattern { Pattern = @"^\s*PROGRAM-ID\.\s+([a-zA-Z0-9-]+)", SymbolType = SymbolType.Program }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z0-9-]+)\b", SymbolType = SymbolType.Identifier }
                    }
                };

            case "pli":
                return new LanguageSymbolRules
                {
                    LanguageId = "pli",
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*:\s*PROC\s*\(", SymbolType = SymbolType.Procedure },
                        new SymbolPattern { Pattern = @"^\s*DCL\s+1\s+([a-zA-Z_][a-zA-Z0-9_]*)", SymbolType = SymbolType.Structure },
                        new SymbolPattern { Pattern = @"^\s*DCL\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*:\s*TASK\s*;", SymbolType = SymbolType.Task }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier }
                    }
                };

            case "rust":
                return new LanguageSymbolRules
                {
                    LanguageId = "rust",
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"^\s*pub\s+struct\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*{", SymbolType = SymbolType.Struct },
                        new SymbolPattern { Pattern = @"^\s*pub\s+enum\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*{", SymbolType = SymbolType.Enum },
                        new SymbolPattern { Pattern = @"^\s*pub\s+trait\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*{", SymbolType = SymbolType.Trait },
                        new SymbolPattern { Pattern = @"^\s*impl\s+.*\s+for\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*{", SymbolType = SymbolType.Impl },
                        new SymbolPattern { Pattern = @"^\s*pub\s+fn\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*fn\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*mod\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*;", SymbolType = SymbolType.Module },
                        new SymbolPattern { Pattern = @"^\s*let\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*const\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*:", SymbolType = SymbolType.Constant },
                        new SymbolPattern { Pattern = @"^\s*static\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*:", SymbolType = SymbolType.Static }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier },
                        new SymbolPattern { Pattern = @"\b(self\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field }
                    }
                };

            case "go":
                return new LanguageSymbolRules
                {
                    LanguageId = "go",
                    SymbolPatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"^\s*package\s+([a-zA-Z0-9_]+)", SymbolType = SymbolType.Package },
                        new SymbolPattern { Pattern = @"^\s*type\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+struct\s*{", SymbolType = SymbolType.Struct },
                        new SymbolPattern { Pattern = @"^\s*type\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+interface\s*{", SymbolType = SymbolType.Interface },
                        new SymbolPattern { Pattern = @"^\s*func\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*\(", SymbolType = SymbolType.Function },
                        new SymbolPattern { Pattern = @"^\s*func\s*\(", SymbolType = SymbolType.Method },
                        new SymbolPattern { Pattern = @"^\s*var\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+", SymbolType = SymbolType.Variable },
                        new SymbolPattern { Pattern = @"^\s*const\s+([a-zA-Z_][a-zA-Z0-9_]*)\s+", SymbolType = SymbolType.Constant },
                        new SymbolPattern { Pattern = @"^\s*import\s+\"([^\"]+)\"", SymbolType = SymbolType.Import }
                    },
                    ReferencePatterns = new List<SymbolPattern>
                    {
                        new SymbolPattern { Pattern = @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Identifier },
                        new SymbolPattern { Pattern = @"\b(\w+\.([a-zA-Z_][a-zA-Z0-9_]*)\b", SymbolType = SymbolType.Field }
                    }
                };

            default:
                return defaultRules;
        }
    }

    /// <summary>
    /// Counts the number of lines before a given position.
    /// </summary>
    private int CountLinesBefore(string text, int position)
    {
        if (position <= 0) return 0;
        if (position >= text.Length) return text.Split('\n').Length - 1;

        return text.Substring(0, position).Split('\n').Length - 1;
    }

    /// <summary>
    /// Gets the column position within a line.
    /// </summary>
    private int GetColumn(string text, int position)
    {
        if (position <= 0) return 0;
        if (position >= text.Length) return text.Split('\n').Last().Length;

        var lastNewline = text.LastIndexOf('\n', position);
        return position - lastNewline - 1;
    }

    /// <summary>
    /// Determines the scope of a symbol based on its position.
    /// </summary>
    private string DetermineScope(string sourceCode, int position, string languageId)
    {
        var lines = sourceCode.Split('\n');
        var lineIndex = CountLinesBefore(sourceCode, position);
        var line = lines[lineIndex];
        var column = GetColumn(sourceCode, position);

        // Simple scope detection based on indentation
        var leadingSpaces = line.Substring(0, column).Length - line.Substring(0, column).TrimStart().Length;
        
        if (leadingSpaces == 0)
            return "global";
        else if (leadingSpaces < 4)
            return "namespace";
        else if (leadingSpaces < 8)
            return "class";
        else if (leadingSpaces < 12)
            return "method";
        else
            return "local";
    }

    /// <summary>
    /// Searches for symbols matching the given query.
    /// </summary>
    public List<SymbolInfo> SearchSymbols(string query, string languageId = null, SymbolType? typeFilter = null)
    {
        var results = new List<SymbolInfo>();

        if (string.IsNullOrEmpty(query))
            return results;

        // If we have a specific language, search in that language's cache
        if (!string.IsNullOrEmpty(languageId))
        {
            if (_symbolCache.TryGetValue(languageId, out var symbols))
            {
                var queryLower = query.ToLower();
                foreach (var symbol in symbols)
                {
                    if ((typeFilter == null || symbol.Type == typeFilter) &&
                        symbol.Name.ToLower().Contains(queryLower))
                    {
                        results.Add(symbol);
                    }
                }
            }
        }
        else
        {
            // Search across all cached symbols
            foreach (var kvp in _symbolCache)
            {
                var queryLower = query.ToLower();
                foreach (var symbol in kvp.Value)
                {
                    if ((typeFilter == null || symbol.Type == typeFilter) &&
                        symbol.Name.ToLower().Contains(queryLower))
                    {
                        results.Add(symbol);
                    }
                }
            }
        }

        // Sort by relevance (exact match first, then contains)
        results.Sort((a, b) => 
        {
            var aMatch = a.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            var bMatch = b.Name.Equals(query, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
            
            if (aMatch != bMatch)
                return aMatch.CompareTo(bMatch);
            
            return a.Name.CompareTo(b.Name);
        });

        return results;
    }

    /// <summary>
    /// Searches for symbols in a specific file.
    /// </summary>
    public List<SymbolInfo> SearchSymbolsInFile(string filePath, string query, SymbolType? typeFilter = null)
    {
        var results = new List<SymbolInfo>();

        if (string.IsNullOrEmpty(query) || string.IsNullOrEmpty(filePath))
            return results;

        if (_indexCache.TryGetValue(filePath, out var index))
        {
            var queryLower = query.ToLower();
            foreach (var symbol in index.Symbols)
            {
                if ((typeFilter == null || symbol.Type == typeFilter) &&
                    symbol.Name.ToLower().Contains(queryLower))
                {
                    results.Add(symbol);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Gets all references to a specific symbol.
    /// </summary>
    public List<SymbolReference> GetSymbolReferences(string symbolName, string languageId = null)
    {
        var results = new List<SymbolReference>();

        if (string.IsNullOrEmpty(symbolName))
            return results;

        if (!string.IsNullOrEmpty(languageId))
        {
            if (_referenceCache.TryGetValue(languageId, out var references))
            {
                foreach (var reference in references)
                {
                    if (reference.SymbolName.Equals(symbolName, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(reference);
                    }
                }
            }
        }
        else
        {
            foreach (var kvp in _referenceCache)
            {
                foreach (var reference in kvp.Value)
                {
                    if (reference.SymbolName.Equals(symbolName, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(reference);
                    }
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Gets the definition of a symbol.
    /// </summary>
    public SymbolInfo GetSymbolDefinition(string symbolName, string filePath, int line, int column)
    {
        if (string.IsNullOrEmpty(symbolName) || string.IsNullOrEmpty(filePath))
            return null;

        if (_indexCache.TryGetValue(filePath, out var index))
        {
            // Find symbol at or near the given position
            foreach (var symbol in index.Symbols)
            {
                if (symbol.Name == symbolName &&
                    symbol.Line == line &&
                    symbol.Column == column)
                {
                    return symbol;
                }
            }

            // If not found at exact position, find first symbol with matching name
            foreach (var symbol in index.Symbols)
            {
                if (symbol.Name == symbolName)
                {
                    return symbol;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Adds symbols to the cache.
    /// </summary>
    public void AddSymbolsToCache(string languageId, List<SymbolInfo> symbols)
    {
        if (string.IsNullOrEmpty(languageId) || symbols == null)
            return;

        if (!_symbolCache.ContainsKey(languageId))
            _symbolCache[languageId] = new List<SymbolInfo>();

        _symbolCache[languageId].AddRange(symbols);
    }

    /// <summary>
    /// Adds references to the cache.
    /// </summary>
    public void AddReferencesToCache(string languageId, List<SymbolReference> references)
    {
        if (string.IsNullOrEmpty(languageId) || references == null)
            return;

        if (!_referenceCache.ContainsKey(languageId))
            _referenceCache[languageId] = new List<SymbolReference>();

        _referenceCache[languageId].AddRange(references);
    }

    /// <summary>
    /// Clears the symbol cache.
    /// </summary>
    public void ClearSymbolCache()
    {
        _symbolCache.Clear();
        _referenceCache.Clear();
        _indexCache.Clear();
    }

    /// <summary>
    /// Clears the cache for a specific language.
    /// </summary>
    public void ClearLanguageCache(string languageId)
    {
        _symbolCache.Remove(languageId);
        _referenceCache.Remove(languageId);

        // Remove indexes for this language
        var keysToRemove = _indexCache.Where(kvp => kvp.Value.LanguageId == languageId).Select(kvp => kvp.Key).ToList();
        foreach (var key in keysToRemove)
        {
            _indexCache.Remove(key);
        }
    }

    /// <summary>
    /// Gets all symbols of a specific type in a file.
    /// </summary>
    public List<SymbolInfo> GetSymbolsByType(string filePath, SymbolType type)
    {
        var results = new List<SymbolInfo>();

        if (string.IsNullOrEmpty(filePath))
            return results;

        if (_indexCache.TryGetValue(filePath, out var index))
        {
            foreach (var symbol in index.Symbols)
            {
                if (symbol.Type == type)
                {
                    results.Add(symbol);
                }
            }
        }

        return results;
    }

    /// <summary>
    /// Gets the symbol at a specific position in a file.
    /// </summary>
    public SymbolInfo GetSymbolAtPosition(string filePath, int line, int column)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        if (_indexCache.TryGetValue(filePath, out var index))
        {
            // Find symbol at or containing the given position
            foreach (var symbol in index.Symbols)
            {
                if (symbol.Line == line && symbol.Column <= column &&
                    symbol.Column + symbol.Name.Length >= column)
                {
                    return symbol;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets all symbols in a file.
    /// </summary>
    public List<SymbolInfo> GetAllSymbols(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return new List<SymbolInfo>();

        if (_indexCache.TryGetValue(filePath, out var index))
        {
            return index.Symbols;
        }

        return new List<SymbolInfo>();
    }
}

/// <summary>
/// Interface for symbol search service.
/// </summary>
public interface ISymbolSearchService
{
    /// <summary>
    /// Builds a symbol index for the given source code.
    /// </summary>
    SymbolSearchIndex BuildIndex(string sourceCode, string languageId, string filePath = null);

    /// <summary>
    /// Searches for symbols matching the given query.
    /// </summary>
    List<SymbolInfo> SearchSymbols(string query, string languageId = null, SymbolType? typeFilter = null);

    /// <summary>
    /// Searches for symbols in a specific file.
    /// </summary>
    List<SymbolInfo> SearchSymbolsInFile(string filePath, string query, SymbolType? typeFilter = null);

    /// <summary>
    /// Gets all references to a specific symbol.
    /// </summary>
    List<SymbolReference> GetSymbolReferences(string symbolName, string languageId = null);

    /// <summary>
    /// Gets the definition of a symbol.
    /// </summary>
    SymbolInfo GetSymbolDefinition(string symbolName, string filePath, int line, int column);

    /// <summary>
    /// Gets the symbol at a specific position in a file.
    /// </summary>
    SymbolInfo GetSymbolAtPosition(string filePath, int line, int column);

    /// <summary>
    /// Gets all symbols of a specific type in a file.
    /// </summary>
    List<SymbolInfo> GetSymbolsByType(string filePath, SymbolType type);

    /// <summary>
    /// Gets all symbols in a file.
    /// </summary>
    List<SymbolInfo> GetAllSymbols(string filePath);

    /// <summary>
    /// Clears the symbol cache.
    /// </summary>
    void ClearSymbolCache();

    /// <summary>
    /// Clears the cache for a specific language.
    /// </summary>
    void ClearLanguageCache(string languageId);
}

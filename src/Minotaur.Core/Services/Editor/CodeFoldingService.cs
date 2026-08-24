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

namespace Minotaur.Core.Services.Editor;

/// <summary>
/// Service for managing code folding regions in source code.
/// Provides functionality to identify foldable regions and manage fold state.
/// </summary>
public class CodeFoldingService : ICodeFoldingService
{
    private readonly Dictionary<string, List<FoldableRegion>> _languagePatterns;
    private readonly Dictionary<string, FoldState> _foldStates = new();
    private readonly Dictionary<string, List<FoldableRegion>> _customRegions = new();

    /// <summary>
    /// Initializes a new instance of the CodeFoldingService.
    /// </summary>
    public CodeFoldingService()
    {
        _languagePatterns = new Dictionary<string, List<FoldableRegion>>(StringComparer.OrdinalIgnoreCase);
        InitializeDefaultPatterns();
    }

    /// <summary>
    /// Initializes default folding patterns for supported languages.
    /// </summary>
    private void InitializeDefaultPatterns()
    {
        // C# folding patterns
        _languagePatterns["csharp"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*//\s*#region\s+(.*)$", EndPattern = @"^\s*//\s*#endregion\s*$", Type = FoldType.Region, Priority = 100 },
            new FoldableRegion { Pattern = @"^\s*namespace\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Namespace, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*class\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Class, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*struct\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Struct, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*interface\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Interface, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*enum\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Enum, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*(public|private|protected|internal|static|async|override)\s+.*\s+(\w+\s+)?\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Method, Priority = 80, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*(if|else if)\s*\(", EndPattern = @"^\s*}", Type = FoldType.Conditional, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*(for|foreach|while)\s*\(", EndPattern = @"^\s*}", Type = FoldType.Loop, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*try\s*{", EndPattern = @"^\s*}", Type = FoldType.Try, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*catch\s*\(", EndPattern = @"^\s*}", Type = FoldType.Catch, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*finally\s*{", EndPattern = @"^\s*}", Type = FoldType.Finally, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*using\s*\(", EndPattern = @"^\s*}", Type = FoldType.Using, Priority = 60, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*lock\s*\(", EndPattern = @"^\s*}", Type = FoldType.Lock, Priority = 60, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*unsafe\s*{", EndPattern = @"^\s*}", Type = FoldType.Unsafe, Priority = 60, IsMultiline = true }
        };

        // Java folding patterns
        _languagePatterns["java"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*//\s*#region\s+(.*)$", EndPattern = @"^\s*//\s*#endregion\s*$", Type = FoldType.Region, Priority = 100 },
            new FoldableRegion { Pattern = @"^\s*package\s+\w+\s*;", EndPattern = @"^\s*;", Type = FoldType.Package, Priority = 90 },
            new FoldableRegion { Pattern = @"^\s*import\s+\w+\s*;", EndPattern = @"^\s*;", Type = FoldType.Import, Priority = 85 },
            new FoldableRegion { Pattern = @"^\s*public\s+class\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Class, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*public\s+interface\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Interface, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*public\s+enum\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Enum, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*(public|private|protected|static|final|synchronized|native|abstract)\s+.*\s+(\w+\s+)?\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Method, Priority = 80, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*(if|else if)\s*\(", EndPattern = @"^\s*}", Type = FoldType.Conditional, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*(for|while|do)\s*\(", EndPattern = @"^\s*}", Type = FoldType.Loop, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*try\s*{", EndPattern = @"^\s*}", Type = FoldType.Try, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*catch\s*\(", EndPattern = @"^\s*}", Type = FoldType.Catch, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*finally\s*{", EndPattern = @"^\s*}", Type = FoldType.Finally, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*synchronized\s*\(", EndPattern = @"^\s*}", Type = FoldType.Synchronized, Priority = 60, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*static\s*{", EndPattern = @"^\s*}", Type = FoldType.Static, Priority = 60, IsMultiline = true }
        };

        // JavaScript/TypeScript folding patterns
        _languagePatterns["javascript"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*//\s*#region\s+(.*)$", EndPattern = @"^\s*//\s*#endregion\s*$", Type = FoldType.Region, Priority = 100 },
            new FoldableRegion { Pattern = @"^\s*function\s+\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Function, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*const\s+\w+\s*=\s*function\s*\(", EndPattern = @"^\s*}", Type = FoldType.Function, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*let\s+\w+\s*=\s*function\s*\(", EndPattern = @"^\s*}", Type = FoldType.Function, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*var\s+\w+\s*=\s*function\s*\(", EndPattern = @"^\s*}", Type = FoldType.Function, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*class\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Class, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*interface\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Interface, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*if\s*\(", EndPattern = @"^\s*}", Type = FoldType.Conditional, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*else\s*{", EndPattern = @"^\s*}", Type = FoldType.Else, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*(for|while|do)\s*\(", EndPattern = @"^\s*}", Type = FoldType.Loop, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*try\s*{", EndPattern = @"^\s*}", Type = FoldType.Try, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*catch\s*\(", EndPattern = @"^\s*}", Type = FoldType.Catch, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*finally\s*{", EndPattern = @"^\s*}", Type = FoldType.Finally, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*switch\s*\(", EndPattern = @"^\s*}", Type = FoldType.Switch, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*namespace\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Namespace, Priority = 80, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*export\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Module, Priority = 80, IsMultiline = true }
        };

        _languagePatterns["typescript"] = new List<FoldableRegion>(_languagePatterns["javascript"]);

        // Python folding patterns
        _languagePatterns["python"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*#\s*region\s+(.*)$", EndPattern = @"^\s*#\s*endregion\s*$", Type = FoldType.Region, Priority = 100 },
            new FoldableRegion { Pattern = @"^\s*class\s+\w+\s*:", EndPattern = @"^\s*$", Type = FoldType.Class, Priority = 90, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*def\s+\w+\s*\(", EndPattern = @"^\s*$", Type = FoldType.Function, Priority = 90, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*if\s+.*\s*:", EndPattern = @"^\s*$", Type = FoldType.Conditional, Priority = 70, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*elif\s+.*\s*:", EndPattern = @"^\s*$", Type = FoldType.Conditional, Priority = 70, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*else\s*:", EndPattern = @"^\s*$", Type = FoldType.Else, Priority = 70, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*(for|while)\s+.*\s*:", EndPattern = @"^\s*$", Type = FoldType.Loop, Priority = 70, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*try\s*:", EndPattern = @"^\s*$", Type = FoldType.Try, Priority = 70, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*except\s*:", EndPattern = @"^\s*$", Type = FoldType.Catch, Priority = 70, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*finally\s*:", EndPattern = @"^\s*$", Type = FoldType.Finally, Priority = 70, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*with\s+.*\s*:", EndPattern = @"^\s*$", Type = FoldType.With, Priority = 60, IsMultiline = true, IndentSensitive = true },
            new FoldableRegion { Pattern = @"^\s*import\s+.*", EndPattern = @"^\s*$", Type = FoldType.Import, Priority = 85, IsMultiline = true, IndentSensitive = false }
        };

        // COBOL folding patterns
        _languagePatterns["cobol"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*IDENTIFICATION\s+DIVISION\.", EndPattern = @"^\s*PROCEDURE\s+DIVISION\.", Type = FoldType.Division, Priority = 100, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*DATA\s+DIVISION\.", EndPattern = @"^\s*(PROCEDURE\s+DIVISION|ENVIRONMENT\s+DIVISION)", Type = FoldType.Division, Priority = 100, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*WORKING-STORAGE\s+SECTION\.", EndPattern = @"^\s*(01\s+|77\s+|66\s+)", Type = FoldType.Section, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*01\s+\w+\.", EndPattern = @"^\s*01\s+\w+\.", Type = FoldType.DataEntry, Priority = 80, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*\w+\.", EndPattern = @"^\s*\w+\.", Type = FoldType.Paragraph, Priority = 80, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*IF\s+.*\s+THEN", EndPattern = @"^\s*END-IF\.", Type = FoldType.Conditional, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*PERFORM\s+.*", EndPattern = @"^\s*\w+\.", Type = FoldType.Perform, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*EVALUATE\s+.*", EndPattern = @"^\s*END-EVALUATE\.", Type = FoldType.Evaluate, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*SELECT\s+.*", EndPattern = @"^\s*END-SELECT\.", Type = FoldType.Select, Priority = 70, IsMultiline = true }
        };

        // PL/I folding patterns
        _languagePatterns["pli"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*\w+:\s*PROC\s*\(", EndPattern = @"^\s*END\s+\w+", Type = FoldType.Procedure, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*DCL\s+1\s+\w+", EndPattern = @"^\s*DCL\s+1\s+\w+", Type = FoldType.Structure, Priority = 85, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*DO\s*;", EndPattern = @"^\s*END\s*;", Type = FoldType.DoGroup, Priority = 80, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*DO\s+WHILE\s*\(", EndPattern = @"^\s*END\s*;", Type = FoldType.DoWhile, Priority = 80, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*DO\s+\w+\s*=\s*\w+\s+TO\s+\w+", EndPattern = @"^\s*END\s*;", Type = FoldType.DoFor, Priority = 80, IsMultiline = true },
            new FoldableRule { Pattern = @"^\s*SELECT\s*\(", EndPattern = @"^\s*END\s*;", Type = FoldType.Select, Priority = 75, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*IF\s+.*\s+THEN\s*;", EndPattern = @"^\s*END\s*;", Type = FoldType.Conditional, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*ON\s+\w+", EndPattern = @"^\s*END\s*;", Type = FoldType.Exception, Priority = 65, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*\w+:\s*TASK\s*;", EndPattern = @"^\s*END\s+\w+", Type = FoldType.Task, Priority = 60, IsMultiline = true }
        };

        // Rust folding patterns
        _languagePatterns["rust"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*mod\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Module, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*pub\s+struct\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Struct, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*pub\s+enum\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Enum, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*pub\s+trait\s+\w+\s*{", EndPattern = @"^\s*}", Type = FoldType.Trait, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*impl\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.Impl, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*pub\s+fn\s+\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Function, Priority = 85, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*fn\s+\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Function, Priority = 85, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*if\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.Conditional, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*else\s*{", EndPattern = @"^\s*}", Type = FoldType.Else, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*match\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.Match, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*loop\s*{", EndPattern = @"^\s*}", Type = FoldType.Loop, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*while\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.While, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*for\s+.*\s+in\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.For, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*unsafe\s*{", EndPattern = @"^\s*}", Type = FoldType.Unsafe, Priority = 60, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*async\s*{", EndPattern = @"^\s*}", Type = FoldType.Async, Priority = 60, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*mod\s+\w+;", EndPattern = @"^\s*;", Type = FoldType.ModuleDeclaration, Priority = 80 }
        };

        // Go folding patterns
        _languagePatterns["go"] = new List<FoldableRegion>
        {
            new FoldableRegion { Pattern = @"^\s*package\s+\w+", EndPattern = @"^\s*$", Type = FoldType.Package, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*import\s*\(", EndPattern = @"^\s*\)", Type = FoldType.Import, Priority = 85, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*type\s+\w+\s+struct\s*{", EndPattern = @"^\s*}", Type = FoldType.Struct, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*type\s+\w+\s+interface\s*{", EndPattern = @"^\s*}", Type = FoldType.Interface, Priority = 90, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*func\s+\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Function, Priority = 85, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*func\s*\(", EndPattern = @"^\s*}", Type = FoldType.Method, Priority = 85, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*if\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.Conditional, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*else\s*{", EndPattern = @"^\s*}", Type = FoldType.Else, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*switch\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.Switch, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*case\s+.*\s*:", EndPattern = @"^\s*case\s+|^\s*default\s*:", Type = FoldType.Case, Priority = 65, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*for\s+.*\s*{", EndPattern = @"^\s*}", Type = FoldType.Loop, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*go\s+\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Goroutine, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*select\s*{", EndPattern = @"^\s*}", Type = FoldType.Select, Priority = 70, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*defer\s+\w+\s*\(", EndPattern = @"^\s*}", Type = FoldType.Defer, Priority = 60, IsMultiline = true },
            new FoldableRegion { Pattern = @"^\s*chan\s+\w+\s*=\s*make\s*\(", EndPattern = @"^\s*\)", Type = FoldType.Channel, Priority = 60, IsMultiline = true }
        };
    }

    /// <summary>
    /// Identifies foldable regions in the given source code for a specific language.
    /// </summary>
    public List<FoldableRegion> IdentifyFoldableRegions(string sourceCode, string languageId)
    {
        var regions = new List<FoldableRegion>();
        
        if (string.IsNullOrEmpty(sourceCode) || string.IsNullOrEmpty(languageId))
            return regions;

        if (!_languagePatterns.TryGetValue(languageId, out var patterns))
            return regions;

        var lines = sourceCode.Split('\n');
        var stack = new Stack<(FoldableRegion Pattern, int StartLine)>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            
            // Check each pattern
            foreach (var pattern in patterns)
            {
                if (Regex.IsMatch(line, pattern.Pattern, RegexOptions.IgnoreCase))
                {
                    // Push start pattern onto stack
                    stack.Push((pattern, i));
                }
                else if (pattern.IsMultiline && Regex.IsMatch(line, pattern.EndPattern, RegexOptions.IgnoreCase))
                {
                    // Check if there's a matching start pattern on the stack
                    while (stack.Count > 0)
                    {
                        var (startPattern, startLine) = stack.Pop();
                        
                        // Only match if this is the corresponding end pattern
                        if (startPattern.EndPattern == pattern.EndPattern ||
                            startPattern.Pattern == pattern.Pattern)
                        {
                            regions.Add(new FoldableRegion
                            {
                                StartLine = startLine,
                                EndLine = i,
                                Type = startPattern.Type,
                                StartText = lines[startLine].Trim(),
                                EndText = line.Trim(),
                                LanguageId = languageId,
                                IsCollapsed = GetFoldState(languageId, startLine, i) == FoldState.Collapsed
                            });
                            break;
                        }
                    }
                }
            }
        }

        // Sort by start line
        regions.Sort((a, b) => a.StartLine.CompareTo(b.StartLine));
        
        return regions;
    }

    /// <summary>
    /// Gets the fold state for a specific region.
    /// </summary>
    public FoldState GetFoldState(string languageId, int startLine, int endLine)
    {
        var key = $"{languageId}:{startLine}:{endLine}";
        return _foldStates.TryGetValue(key, out var state) ? state : FoldState.Expanded;
    }

    /// <summary>
    /// Sets the fold state for a specific region.
    /// </summary>
    public void SetFoldState(string languageId, int startLine, int endLine, FoldState state)
    {
        var key = $"{languageId}:{startLine}:{endLine}";
        _foldStates[key] = state;
    }

    /// <summary>
    /// Toggles the fold state for a specific region.
    /// </summary>
    public FoldState ToggleFoldState(string languageId, int startLine, int endLine)
    {
        var current = GetFoldState(languageId, startLine, endLine);
        var newState = current == FoldState.Expanded ? FoldState.Collapsed : FoldState.Expanded;
        SetFoldState(languageId, startLine, endLine, newState);
        return newState;
    }

    /// <summary>
    /// Adds a custom foldable region.
    /// </summary>
    public void AddCustomRegion(string languageId, FoldableRegion region)
    {
        if (string.IsNullOrEmpty(languageId) || region == null)
            return;

        if (!_customRegions.ContainsKey(languageId))
            _customRegions[languageId] = new List<FoldableRegion>();

        _customRegions[languageId].Add(region);
    }

    /// <summary>
    /// Removes a custom foldable region.
    /// </summary>
    public void RemoveCustomRegion(string languageId, FoldableRegion region)
    {
        if (string.IsNullOrEmpty(languageId) || region == null)
            return;

        if (_customRegions.TryGetValue(languageId, out var regions))
        {
            regions.Remove(region);
        }
    }

    /// <summary>
    /// Gets all custom regions for a language.
    /// </summary>
    public List<FoldableRegion> GetCustomRegions(string languageId)
    {
        if (string.IsNullOrEmpty(languageId))
            return new List<FoldableRegion>();

        return _customRegions.TryGetValue(languageId, out var regions) ? regions : new List<FoldableRegion>();
    }

    /// <summary>
    /// Clears all fold states.
    /// </summary>
    public void ClearFoldStates()
    {
        _foldStates.Clear();
    }

    /// <summary>
    /// Expands all folded regions.
    /// </summary>
    public void ExpandAll()
    {
        foreach (var key in _foldStates.Keys.ToList())
        {
            _foldStates[key] = FoldState.Expanded;
        }
    }

    /// <summary>
    /// Collapses all foldable regions.
    /// </summary>
    public void CollapseAll(string languageId, List<FoldableRegion> regions)
    {
        foreach (var region in regions)
        {
            SetFoldState(languageId, region.StartLine, region.EndLine, FoldState.Collapsed);
        }
    }

    /// <summary>
    /// Gets the text to display for a folded region.
    /// </summary>
    public string GetFoldedText(FoldableRegion region, int lineCount)
    {
        var typeName = region.Type.ToString();
        var startText = region.StartText;
        
        // Truncate start text if too long
        if (startText.Length > 50)
            startText = startText.Substring(0, 47) + "...";

        return $"... {typeName}: {startText} ({lineCount} lines hidden) ...";
    }
}

/// <summary>
/// Interface for code folding service.
/// </summary>
public interface ICodeFoldingService
{
    /// <summary>
    /// Identifies foldable regions in the given source code for a specific language.
    /// </summary>
    List<FoldableRegion> IdentifyFoldableRegions(string sourceCode, string languageId);

    /// <summary>
    /// Gets the fold state for a specific region.
    /// </summary>
    FoldState GetFoldState(string languageId, int startLine, int endLine);

    /// <summary>
    /// Sets the fold state for a specific region.
    /// </summary>
    void SetFoldState(string languageId, int startLine, int endLine, FoldState state);

    /// <summary>
    /// Toggles the fold state for a specific region.
    /// </summary>
    FoldState ToggleFoldState(string languageId, int startLine, int endLine);

    /// <summary>
    /// Adds a custom foldable region.
    /// </summary>
    void AddCustomRegion(string languageId, FoldableRegion region);

    /// <summary>
    /// Removes a custom foldable region.
    /// </summary>
    void RemoveCustomRegion(string languageId, FoldableRegion region);

    /// <summary>
    /// Gets all custom regions for a language.
    /// </summary>
    List<FoldableRegion> GetCustomRegions(string languageId);

    /// <summary>
    /// Clears all fold states.
    /// </summary>
    void ClearFoldStates();

    /// <summary>
    /// Expands all folded regions.
    /// </summary>
    void ExpandAll();

    /// <summary>
    /// Collapses all foldable regions.
    /// </summary>
    void CollapseAll(string languageId, List<FoldableRegion> regions);

    /// <summary>
    /// Gets the text to display for a folded region.
    /// </summary>
    string GetFoldedText(FoldableRegion region, int lineCount);
}

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

namespace Minotaur.Core.Models.Editor;

/// <summary>
/// Types of foldable regions.
/// </summary>
public enum FoldType
{
    /// <summary>Unknown fold type</summary>
    Unknown,
    /// <summary>Custom region (e.g., #region)</summary>
    Region,
    /// <summary>Namespace</summary>
    Namespace,
    /// <summary>Class</summary>
    Class,
    /// <summary>Interface</summary>
    Interface,
    /// <summary>Struct</summary>
    Struct,
    /// <summary>Enum</summary>
    Enum,
    /// <summary>Method/Function</summary>
    Method,
    /// <summary>Function</summary>
    Function,
    /// <summary>Procedure</summary>
    Procedure,
    /// <summary>Conditional (if, else if)</summary>
    Conditional,
    /// <summary>Else clause</summary>
    Else,
    /// <summary>Loop (for, while, do)</summary>
    Loop,
    /// <summary>Try block</summary>
    Try,
    /// <summary>Catch block</summary>
    Catch,
    /// <summary>Finally block</summary>
    Finally,
    /// <summary>Using statement</summary>
    Using,
    /// <summary>Lock statement</summary>
    Lock,
    /// <summary>Unsafe block</summary>
    Unsafe,
    /// <summary>Async block</summary>
    Async,
    /// <summary>Package</summary>
    Package,
    /// <summary>Import</summary>
    Import,
    /// <summary>Module</summary>
    Module,
    /// <summary>Module declaration</summary>
    ModuleDeclaration,
    /// <summary>Switch statement</summary>
    Switch,
    /// <summary>Case clause</summary>
    Case,
    /// <summary>Goroutine</summary>
    Goroutine,
    /// <summary>Select statement</summary>
    Select,
    /// <summary>Defer statement</summary>
    Defer,
    /// <summary>Channel</summary>
    Channel,
    /// <summary>Division (COBOL)</summary>
    Division,
    /// <summary>Section (COBOL)</summary>
    Section,
    /// <summary>Paragraph (COBOL)</summary>
    Paragraph,
    /// <summary>Data entry (COBOL)</summary>
    DataEntry,
    /// <summary>Perform (COBOL)</summary>
    Perform,
    /// <summary>Evaluate (COBOL)</summary>
    Evaluate,
    /// <summary>Structure (PL/I)</summary>
    Structure,
    /// <summary>Do group (PL/I)</summary>
    DoGroup,
    /// <summary>Do while (PL/I)</summary>
    DoWhile,
    /// <summary>Do for (PL/I)</summary>
    DoFor,
    /// <summary>Exception (PL/I)</summary>
    Exception,
    /// <summary>Task (PL/I)</summary>
    Task,
    /// <summary>Trait (Rust)</summary>
    Trait,
    /// <summary>Impl (Rust)</summary>
    Impl,
    /// <summary>Match (Rust)</summary>
    Match
}

/// <summary>
/// State of a foldable region.
/// </summary>
public enum FoldState
{
    /// <summary>Region is expanded (visible)</summary>
    Expanded,
    /// <summary>Region is collapsed (hidden)</summary>
    Collapsed,
    /// <summary>Region is partially collapsed</summary>
    PartiallyCollapsed
}

/// <summary>
/// Represents a foldable region in source code.
/// </summary>
public class FoldableRegion
{
    /// <summary>
    /// Gets or sets the start line of the region (1-based).
    /// </summary>
    public int StartLine { get; set; } = 0;

    /// <summary>
    /// Gets or sets the end line of the region (1-based).
    /// </summary>
    public int EndLine { get; set; } = 0;

    /// <summary>
    /// Gets or sets the type of foldable region.
    /// </summary>
    public FoldType Type { get; set; } = FoldType.Unknown;

    /// <summary>
    /// Gets or sets the text at the start of the region.
    /// </summary>
    public string StartText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the text at the end of the region.
    /// </summary>
    public string EndText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language ID.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern to match the start of the region.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the pattern to match the end of the region.
    /// </summary>
    public string EndPattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether this region can span multiple lines.
    /// </summary>
    public bool IsMultiline { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this region is sensitive to indentation.
    /// </summary>
    public bool IndentSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets the priority of this region pattern.
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this region is currently collapsed.
    /// </summary>
    public bool IsCollapsed { get; set; } = false;

    /// <summary>
    /// Gets or sets whether this region can be collapsed.
    /// </summary>
    public bool CanCollapse { get; set; } = true;

    /// <summary>
    /// Gets the number of lines in this region.
    /// </summary>
    public int LineCount => EndLine - StartLine + 1;

    /// <summary>
    /// Gets whether this region is valid (has valid start and end lines).
    /// </summary>
    public bool IsValid => StartLine > 0 && EndLine >= StartLine;

    /// <summary>
    /// Creates a copy of this region.
    /// </summary>
    public FoldableRegion Clone()
    {
        return new FoldableRegion
        {
            StartLine = StartLine,
            EndLine = EndLine,
            Type = Type,
            StartText = StartText,
            EndText = EndText,
            LanguageId = LanguageId,
            Pattern = Pattern,
            EndPattern = EndPattern,
            IsMultiline = IsMultiline,
            IndentSensitive = IndentSensitive,
            Priority = Priority,
            IsCollapsed = IsCollapsed,
            CanCollapse = CanCollapse
        };
    }

    /// <summary>
    /// Creates a string representation of this region.
    /// </summary>
    public override string ToString()
    {
        return $"{Type}: {StartLine}-{EndLine} ({LineCount} lines)";
    }
}

/// <summary>
/// Represents the fold state of a file.
/// </summary>
public class FileFoldState
{
    /// <summary>
    /// Gets or sets the file path.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the language ID.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of foldable regions in the file.
    /// </summary>
    public List<FoldableRegion> Regions { get; set; } = new List<FoldableRegion>();

    /// <summary>
    /// Gets or sets the dictionary of fold states (key: "startLine:endLine").
    /// </summary>
    public Dictionary<string, FoldState> FoldStates { get; set; } = new Dictionary<string, FoldState>();

    /// <summary>
    /// Gets or sets whether all regions are expanded.
    /// </summary>
    public bool AllExpanded => FoldStates.Values.All(s => s == FoldState.Expanded);

    /// <summary>
    /// Gets or sets whether all regions are collapsed.
    /// </summary>
    public bool AllCollapsed => FoldStates.Values.All(s => s == FoldState.Collapsed);

    /// <summary>
    /// Gets the fold state for a specific region.
    /// </summary>
    public FoldState GetFoldState(FoldableRegion region)
    {
        var key = GetRegionKey(region);
        return FoldStates.TryGetValue(key, out var state) ? state : FoldState.Expanded;
    }

    /// <summary>
    /// Sets the fold state for a specific region.
    /// </summary>
    public void SetFoldState(FoldableRegion region, FoldState state)
    {
        var key = GetRegionKey(region);
        FoldStates[key] = state;
    }

    /// <summary>
    /// Toggles the fold state for a specific region.
    /// </summary>
    public FoldState ToggleFoldState(FoldableRegion region)
    {
        var current = GetFoldState(region);
        var newState = current == FoldState.Expanded ? FoldState.Collapsed : FoldState.Expanded;
        SetFoldState(region, newState);
        return newState;
    }

    /// <summary>
    /// Gets the key for a region.
    /// </summary>
    private string GetRegionKey(FoldableRegion region)
    {
        return $"{region.StartLine}:{region.EndLine}";
    }

    /// <summary>
    /// Expands all regions.
    /// </summary>
    public void ExpandAll()
    {
        foreach (var key in FoldStates.Keys.ToList())
        {
            FoldStates[key] = FoldState.Expanded;
        }
    }

    /// <summary>
    /// Collapses all regions.
    /// </summary>
    public void CollapseAll()
    {
        foreach (var key in FoldStates.Keys.ToList())
        {
            FoldStates[key] = FoldState.Collapsed;
        }
    }

    /// <summary>
    /// Collapses all regions of a specific type.
    /// </summary>
    public void CollapseByType(FoldType type)
    {
        foreach (var region in Regions.Where(r => r.Type == type))
        {
            var key = GetRegionKey(region);
            FoldStates[key] = FoldState.Collapsed;
        }
    }

    /// <summary>
    /// Expands all regions of a specific type.
    /// </summary>
    public void ExpandByType(FoldType type)
    {
        foreach (var region in Regions.Where(r => r.Type == type))
        {
            var key = GetRegionKey(region);
            FoldStates[key] = FoldState.Expanded;
        }
    }
}

/// <summary>
/// Represents a fold level (nesting level of folds).
/// </summary>
public class FoldLevel
{
    /// <summary>
    /// Gets or sets the line number.
    /// </summary>
    public int LineNumber { get; set; }

    /// <summary>
    /// Gets or sets the fold level (0 = not folded, >0 = folded).
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets whether this line is the start of a fold.
    /// </summary>
    public bool IsStart { get; set; }

    /// <summary>
    /// Gets or sets whether this line is the end of a fold.
    /// </summary>
    public bool IsEnd { get; set; }

    /// <summary>
    /// Gets or sets the fold type.
    /// </summary>
    public FoldType FoldType { get; set; } = FoldType.Unknown;

    /// <summary>
    /// Creates a string representation of this fold level.
    /// </summary>
    public override string ToString()
    {
        return $"Line {LineNumber}: Level {Level} ({(IsStart ? "Start" : IsEnd ? "End" : "")}) {FoldType}";
    }
}

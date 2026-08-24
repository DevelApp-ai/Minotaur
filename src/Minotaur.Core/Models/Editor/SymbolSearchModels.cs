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
/// Types of symbols that can be searched for.
/// </summary>
public enum SymbolType
{
    /// <summary>Unknown symbol type</summary>
    Unknown,
    /// <summary>Namespace/module</summary>
    Namespace,
    /// <summary>Package (Go)</summary>
    Package,
    /// <summary>Class</summary>
    Class,
    /// <summary>Interface</summary>
    Interface,
    /// <summary>Struct</summary>
    Struct,
    /// <summary>Enum</summary>
    Enum,
    /// <summary>Trait (Rust)</summary>
    Trait,
    /// <summary>Method</summary>
    Method,
    /// <summary>Function</summary>
    Function,
    /// <summary>Procedure (PL/I, COBOL)</summary>
    Procedure,
    /// <summary>Paragraph (COBOL)</summary>
    Paragraph,
    /// <summary>Field/property</summary>
    Field,
    /// <summary>Property</summary>
    Property,
    /// <summary>Variable</summary>
    Variable,
    /// <summary>Constant</summary>
    Constant,
    /// <summary>Parameter</summary>
    Parameter,
    /// <summary>Type</summary>
    Type,
    /// <summary>Import</summary>
    Import,
    /// <summary>Export</summary>
    Export,
    /// <summary>Decorator (Python)</summary>
    Decorator,
    /// <summary>Module (Rust, Python)</summary>
    Module,
    /// <summary>File (COBOL)</summary>
    File,
    /// <summary>Program (COBOL)</summary>
    Program,
    /// <summary>Task (PL/I)</summary>
    Task,
    /// <summary>Structure (PL/I)</summary>
    Structure,
    /// <summary>Impl (Rust)</summary>
    Impl,
    /// <summary>Identifier (variable, function name, etc.)</summary>
    Identifier,
    /// <summary>String literal</summary>
    String,
    /// <summary>Number literal</summary>
    Number,
    /// <summary>Boolean literal</summary>
    Boolean,
    /// <summary>Null literal</summary>
    Null,
    /// <summary>Global symbol</summary>
    Global
}

/// <summary>
/// Represents information about a symbol.
/// </summary>
public class SymbolInfo
{
    /// <summary>
    /// Gets or sets the name of the symbol.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the symbol.
    /// </summary>
    public SymbolType Type { get; set; } = SymbolType.Unknown;

    /// <summary>
    /// Gets or sets the file path where the symbol is defined.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number where the symbol is defined (1-based).
    /// </summary>
    public int Line { get; set; } = 0;

    /// <summary>
    /// Gets or sets the column number where the symbol is defined (1-based).
    /// </summary>
    public int Column { get; set; } = 0;

    /// <summary>
    /// Gets or sets the start position of the symbol in the source text.
    /// </summary>
    public int StartPosition { get; set; } = 0;

    /// <summary>
    /// Gets or sets the end position of the symbol in the source text.
    /// </summary>
    public int EndPosition { get; set; } = 0;

    /// <summary>
    /// Gets or sets the language ID.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the scope of the symbol (global, namespace, class, method, local).
    /// </summary>
    public string Scope { get; set; } = "global";

    /// <summary>
    /// Gets or sets the parent symbol (e.g., class for a method).
    /// </summary>
    public string ParentSymbol { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the return type (for functions/methods).
    /// </summary>
    public string ReturnType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parameters (for functions/methods).
    /// </summary>
    public List<ParameterInfo> Parameters { get; set; } = new List<ParameterInfo>();

    /// <summary>
    /// Gets or sets the documentation comment.
    /// </summary>
    public string Documentation { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the symbol is public/exported.
    /// </summary>
    public bool IsPublic { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the symbol is static.
    /// </summary>
    public bool IsStatic { get; set; } = false;

    /// <summary>
    /// Gets the length of the symbol name.
    /// </summary>
    public int Length => Name.Length;

    /// <summary>
    /// Gets whether this symbol has a valid position.
    /// </summary>
    public bool HasValidPosition => Line > 0 && Column > 0;

    /// <summary>
    /// Creates a string representation of this symbol.
    /// </summary>
    public override string ToString()
    {
        return $"{Type}: {Name} at {FilePath}:{Line}:{Column}";
    }

    /// <summary>
    /// Creates a copy of this symbol info.
    /// </summary>
    public SymbolInfo Clone()
    {
        return new SymbolInfo
        {
            Name = Name,
            Type = Type,
            FilePath = FilePath,
            Line = Line,
            Column = Column,
            StartPosition = StartPosition,
            EndPosition = EndPosition,
            LanguageId = LanguageId,
            Scope = Scope,
            ParentSymbol = ParentSymbol,
            ReturnType = ReturnType,
            Parameters = new List<ParameterInfo>(Parameters),
            Documentation = Documentation,
            IsPublic = IsPublic,
            IsStatic = IsStatic
        };
    }
}

/// <summary>
/// Represents information about a function/method parameter.
/// </summary>
public class ParameterInfo
{
    /// <summary>
    /// Gets or sets the name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type of the parameter.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether the parameter is optional.
    /// </summary>
    public bool IsOptional { get; set; } = false;

    /// <summary>
    /// Gets or sets the default value (if optional).
    /// </summary>
    public string DefaultValue { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the position of the parameter (0-based).
    /// </summary>
    public int Position { get; set; } = 0;

    /// <summary>
    /// Creates a string representation of this parameter.
    /// </summary>
    public override string ToString()
    {
        return $"{Type} {Name}";
    }

    /// <summary>
    /// Creates a copy of this parameter info.
    /// </summary>
    public ParameterInfo Clone()
    {
        return new ParameterInfo
        {
            Name = Name,
            Type = Type,
            IsOptional = IsOptional,
            DefaultValue = DefaultValue,
            Position = Position
        };
    }
}

/// <summary>
/// Represents a reference to a symbol.
/// </summary>
public class SymbolReference
{
    /// <summary>
    /// Gets or sets the name of the referenced symbol.
    /// </summary>
    public string SymbolName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the file path where the reference occurs.
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the line number where the reference occurs (1-based).
    /// </summary>
    public int Line { get; set; } = 0;

    /// <summary>
    /// Gets or sets the column number where the reference occurs (1-based).
    /// </summary>
    public int Column { get; set; } = 0;

    /// <summary>
    /// Gets or sets the start position of the reference in the source text.
    /// </summary>
    public int StartPosition { get; set; } = 0;

    /// <summary>
    /// Gets or sets the end position of the reference in the source text.
    /// </summary>
    public int EndPosition { get; set; } = 0;

    /// <summary>
    /// Gets or sets the language ID.
    /// </summary>
    public string LanguageId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol information (if resolved).
    /// </summary>
    public SymbolInfo SymbolInfo { get; set; } = null;

    /// <summary>
    /// Gets the length of the reference.
    /// </summary>
    public int Length => EndPosition - StartPosition;

    /// <summary>
    /// Gets whether this reference has a valid position.
    /// </summary>
    public bool HasValidPosition => Line > 0 && Column > 0;

    /// <summary>
    /// Creates a string representation of this reference.
    /// </summary>
    public override string ToString()
    {
        return $"Reference to {SymbolName} at {FilePath}:{Line}:{Column}";
    }

    /// <summary>
    /// Creates a copy of this symbol reference.
    /// </summary>
    public SymbolReference Clone()
    {
        return new SymbolReference
        {
            SymbolName = SymbolName,
            FilePath = FilePath,
            Line = Line,
            Column = Column,
            StartPosition = StartPosition,
            EndPosition = EndPosition,
            LanguageId = LanguageId,
            SymbolInfo = SymbolInfo?.Clone()
        };
    }
}

/// <summary>
/// Represents a pattern for matching symbols.
/// </summary>
public class SymbolPattern
{
    /// <summary>
    /// Gets or sets the regular expression pattern to match.
    /// </summary>
    public string Pattern { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the symbol type to assign to matches.
    /// </summary>
    public SymbolType SymbolType { get; set; } = SymbolType.Unknown;

    /// <summary>
    /// Gets or sets the priority of this pattern (higher priority patterns are matched first).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Gets or sets whether this pattern is case-sensitive.
    /// </summary>
    public bool IsCaseSensitive { get; set; } = false;

    /// <summary>
    /// Creates a copy of this symbol pattern.
    /// </summary>
    public SymbolPattern Clone()
    {
        return new SymbolPattern
        {
            Pattern = Pattern,
            SymbolType = SymbolType,
            Priority = Priority,
            IsCaseSensitive = IsCaseSensitive
        };
    }
}

/// <summary>
/// Represents the symbol search rules for a specific language.
/// </summary>
public class LanguageSymbolRules
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
    /// Gets or sets the list of patterns for matching symbol definitions.
    /// </summary>
    public List<SymbolPattern> SymbolPatterns { get; set; } = new List<SymbolPattern>();

    /// <summary>
    /// Gets or sets the list of patterns for matching symbol references.
    /// </summary>
    public List<SymbolPattern> ReferencePatterns { get; set; } = new List<SymbolPattern>();

    /// <summary>
    /// Gets or sets the list of patterns for matching symbol declarations.
    /// </summary>
    public List<SymbolPattern> DeclarationPatterns { get; set; } = new List<SymbolPattern>();

    /// <summary>
    /// Adds a symbol pattern.
    /// </summary>
    public void AddSymbolPattern(SymbolPattern pattern)
    {
        if (pattern != null)
            SymbolPatterns.Add(pattern);
    }

    /// <summary>
    /// Adds a reference pattern.
    /// </summary>
    public void AddReferencePattern(SymbolPattern pattern)
    {
        if (pattern != null)
            ReferencePatterns.Add(pattern);
    }

    /// <summary>
    /// Creates a copy of this language symbol rules.
    /// </summary>
    public LanguageSymbolRules Clone()
    {
        return new LanguageSymbolRules
        {
            LanguageId = LanguageId,
            DisplayName = DisplayName,
            SymbolPatterns = SymbolPatterns.ConvertAll(p => p.Clone()),
            ReferencePatterns = ReferencePatterns.ConvertAll(p => p.Clone()),
            DeclarationPatterns = DeclarationPatterns.ConvertAll(p => p.Clone())
        };
    }
}

/// <summary>
/// Represents a search index for symbols in a file.
/// </summary>
public class SymbolSearchIndex
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
    /// Gets or sets the list of symbols in the file.
    /// </summary>
    public List<SymbolInfo> Symbols { get; set; } = new List<SymbolInfo>();

    /// <summary>
    /// Gets or sets the list of symbol references in the file.
    /// </summary>
    public List<SymbolReference> References { get; set; } = new List<SymbolReference>();

    /// <summary>
    /// Gets the number of symbols in the index.
    /// </summary>
    public int SymbolCount => Symbols.Count;

    /// <summary>
    /// Gets the number of references in the index.
    /// </summary>
    public int ReferenceCount => References.Count;

    /// <summary>
    /// Gets all symbols of a specific type.
    /// </summary>
    public List<SymbolInfo> GetSymbolsByType(SymbolType type)
    {
        return Symbols.FindAll(s => s.Type == type);
    }

    /// <summary>
    /// Gets all references to a specific symbol.
    /// </summary>
    public List<SymbolReference> GetReferencesTo(string symbolName)
    {
        return References.FindAll(r => r.SymbolName == symbolName);
    }

    /// <summary>
    /// Gets all symbols with a specific name.
    /// </summary>
    public List<SymbolInfo> GetSymbolsByName(string name)
    {
        return Symbols.FindAll(s => s.Name == name);
    }

    /// <summary>
    /// Creates a string representation of this index.
    /// </summary>
    public override string ToString()
    {
        return $"SymbolSearchIndex for {FilePath} ({LanguageId}): {SymbolCount} symbols, {ReferenceCount} references";
    }
}

/// <summary>
/// Represents the result of a symbol search.
/// </summary>
public class SymbolSearchResult
{
    /// <summary>
    /// Gets or sets the search query.
    /// </summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the list of matching symbols.
    /// </summary>
    public List<SymbolInfo> Symbols { get; set; } = new List<SymbolInfo>();

    /// <summary>
    /// Gets or sets the list of matching references.
    /// </summary>
    public List<SymbolReference> References { get; set; } = new List<SymbolReference>();

    /// <summary>
    /// Gets or sets the language filter (if any).
    /// </summary>
    public string LanguageFilter { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the type filter (if any).
    /// </summary>
    public SymbolType? TypeFilter { get; set; } = null;

    /// <summary>
    /// Gets the total number of results.
    /// </summary>
    public int TotalResults => Symbols.Count + References.Count;

    /// <summary>
    /// Gets whether there are any results.
    /// </summary>
    public bool HasResults => TotalResults > 0;

    /// <summary>
    /// Creates a string representation of this result.
    /// </summary>
    public override string ToString()
    {
        return $"Search for '{Query}': {TotalResults} results";
    }
}

/// <summary>
/// Represents a search query for symbols.
/// </summary>
public class SymbolSearchQuery
{
    /// <summary>
    /// Gets or sets the search text.
    /// </summary>
    public string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets whether to search in symbol names only.
    /// </summary>
    public bool SearchInNamesOnly { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to search in symbol types.
    /// </summary>
    public bool SearchInTypes { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to search in file paths.
    /// </summary>
    public bool SearchInFilePaths { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to search in documentation.
    /// </summary>
    public bool SearchInDocumentation { get; set; } = false;

    /// <summary>
    /// Gets or sets the language filter (null for all languages).
    /// </summary>
    public string LanguageFilter { get; set; } = null;

    /// <summary>
    /// Gets or sets the type filter (null for all types).
    /// </summary>
    public SymbolType? TypeFilter { get; set; } = null;

    /// <summary>
    /// Gets or sets whether to search for exact matches only.
    /// </summary>
    public bool ExactMatch { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to search case-sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets whether to include references in the search.
    /// </summary>
    public bool IncludeReferences { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of results to return.
    /// </summary>
    public int MaxResults { get; set; } = 100;

    /// <summary>
    /// Gets whether this query is valid (has search text).
    /// </summary>
    public bool IsValid => !string.IsNullOrEmpty(SearchText);

    /// <summary>
    /// Creates a string representation of this query.
    /// </summary>
    public override string ToString()
    {
        return $"SymbolSearchQuery: '{SearchText}' (Language: {LanguageFilter}, Type: {TypeFilter})";
    }
}

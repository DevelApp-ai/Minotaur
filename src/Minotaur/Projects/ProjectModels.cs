/*
 * This file is part of Minotaur.
 * 
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published
 * by the Free Software Foundation, either version 3 of the License, or
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

namespace Minotaur.Projects;

/// <summary>
/// Represents the type of project detected.
/// </summary>
public enum ProjectType
{
    /// <summary>
    /// A .NET solution file (.sln).
    /// </summary>
    DotNetSolution,
    
    /// <summary>
    /// A .NET project file (.csproj, .vbproj, .fsproj).
    /// </summary>
    DotNetProject,
    
    /// <summary>
    /// A Node.js project with package.json.
    /// </summary>
    NodeProject,
    
    /// <summary>
    /// A Python project with requirements.txt or setup.py.
    /// </summary>
    PythonProject,
    
    /// <summary>
    /// A Java project with pom.xml or .gradle files.
    /// </summary>
    JavaProject,
    
    /// <summary>
    /// A C++ project with CMakeLists.txt or Makefile.
    /// </summary>
    CppProject,
    
    /// <summary>
    /// A Rust project with Cargo.toml.
    /// </summary>
    RustProject,
    
    /// <summary>
    /// A Go project with go.mod.
    /// </summary>
    GoProject,
    
    /// <summary>
    /// A generic folder without specific project files.
    /// </summary>
    GenericFolder
}

/// <summary>
/// Represents a complete project structure with cross-file relationships.
/// </summary>
public class ProjectStructure
{
    /// <summary>
    /// Gets the project root path.
    /// </summary>
    public string RootPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the detected project type.
    /// </summary>
    public ProjectType ProjectType { get; init; }

    /// <summary>
    /// Gets all files in the project with their metadata.
    /// </summary>
    public IReadOnlyList<ProjectFile> Files { get; init; } = Array.Empty<ProjectFile>();

    /// <summary>
    /// Gets cross-file relationships in the project.
    /// </summary>
    public IReadOnlyList<CrossFileRelationship> CrossFileRelationships { get; init; } = Array.Empty<CrossFileRelationship>();

    /// <summary>
    /// Gets project-level configuration files.
    /// </summary>
    public IReadOnlyList<ConfigurationFile> ConfigurationFiles { get; init; } = Array.Empty<ConfigurationFile>();

    /// <summary>
    /// Gets build dependencies and references.
    /// </summary>
    public IReadOnlyList<ProjectDependency> Dependencies { get; init; } = Array.Empty<ProjectDependency>();
}

/// <summary>
/// Represents a file within a project structure.
/// </summary>
public class ProjectFile
{
    /// <summary>
    /// Gets the absolute file path.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the relative path from project root.
    /// </summary>
    public string RelativePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the file type classification.
    /// </summary>
    public FileType FileType { get; init; }

    /// <summary>
    /// Gets the grammar to use for parsing this file.
    /// </summary>
    public string? EmbeddedGrammar { get; init; }

    /// <summary>
    /// Gets symbols extracted from this file.
    /// </summary>
    public IReadOnlyList<ExtractedSymbol> Symbols { get; init; } = Array.Empty<ExtractedSymbol>();

    /// <summary>
    /// Gets dependencies this file has on other files.
    /// </summary>
    public IReadOnlyList<FileDependency> Dependencies { get; init; } = Array.Empty<FileDependency>();
}

/// <summary>
/// Represents the type of a file within a project.
/// </summary>
public enum FileType
{
    SourceCode,
    Configuration,
    Documentation,
    Resource,
    BuildFile,
    Test
}

/// <summary>
/// Represents a symbol extracted from a source file.
/// </summary>
public class ExtractedSymbol
{
    /// <summary>
    /// Gets the symbol name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the symbol type (class, function, variable, etc.).
    /// </summary>
    public SymbolType SymbolType { get; init; }

    /// <summary>
    /// Gets the symbol visibility.
    /// </summary>
    public SymbolVisibility Visibility { get; init; }

    /// <summary>
    /// Gets the location of the symbol in the source file.
    /// </summary>
    public SourceLocation Location { get; init; } = new();

    /// <summary>
    /// Gets the symbol signature (for functions, methods, etc.).
    /// </summary>
    public string? Signature { get; init; }
}

/// <summary>
/// Represents the type of symbol.
/// </summary>
public enum SymbolType
{
    /// <summary>
    /// A class definition.
    /// </summary>
    Class,
    
    /// <summary>
    /// An interface definition.
    /// </summary>
    Interface,
    
    /// <summary>
    /// A standalone function.
    /// </summary>
    Function,
    
    /// <summary>
    /// A method within a class or interface.
    /// </summary>
    Method,
    
    /// <summary>
    /// A variable declaration.
    /// </summary>
    Variable,
    
    /// <summary>
    /// A constant value.
    /// </summary>
    Constant,
    
    /// <summary>
    /// A type definition.
    /// </summary>
    Type,
    
    /// <summary>
    /// A namespace declaration.
    /// </summary>
    Namespace,
    
    /// <summary>
    /// A module definition.
    /// </summary>
    Module,
    
    /// <summary>
    /// A property definition.
    /// </summary>
    Property,
    
    /// <summary>
    /// A field definition.
    /// </summary>
    Field,
    
    /// <summary>
    /// An enumeration definition.
    /// </summary>
    Enum,
    
    /// <summary>
    /// A structure definition.
    /// </summary>
    Struct
}

/// <summary>
/// Represents the visibility of a symbol.
/// </summary>
public enum SymbolVisibility
{
    /// <summary>
    /// Publicly accessible from any code.
    /// </summary>
    Public,
    
    /// <summary>
    /// Accessible only within the same class or structure.
    /// </summary>
    Private,
    
    /// <summary>
    /// Accessible within the same class or derived classes.
    /// </summary>
    Protected,
    
    /// <summary>
    /// Accessible only within the same assembly or module.
    /// </summary>
    Internal,
    
    /// <summary>
    /// Accessible within the same package (language-specific).
    /// </summary>
    Package
}

/// <summary>
/// Represents a location within a source file.
/// </summary>
public class SourceLocation
{
    /// <summary>
    /// Gets the line number (1-based).
    /// </summary>
    public int Line { get; init; }

    /// <summary>
    /// Gets the column number (1-based).
    /// </summary>
    public int Column { get; init; }

    /// <summary>
    /// Gets the length of the symbol.
    /// </summary>
    public int Length { get; init; }
}

/// <summary>
/// Represents a dependency from one file to another.
/// </summary>
public class FileDependency
{
    /// <summary>
    /// Gets the path to the dependency file.
    /// </summary>
    public string DependencyPath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the nature of the dependency.
    /// </summary>
    public DependencyNature Nature { get; init; }

    /// <summary>
    /// Gets the resolved absolute path (if resolvable).
    /// </summary>
    public string? ResolvedPath { get; init; }

    /// <summary>
    /// Gets whether this is an external dependency.
    /// </summary>
    public bool IsExternal { get; init; }
}

/// <summary>
/// Represents the nature of a file dependency.
/// </summary>
public enum DependencyNature
{
    /// <summary>
    /// An import statement or directive.
    /// </summary>
    Import,
    
    /// <summary>
    /// An include directive (typically for C/C++).
    /// </summary>
    Include,
    
    /// <summary>
    /// A reference to another assembly or library.
    /// </summary>
    Reference,
    
    /// <summary>
    /// An embedded resource dependency.
    /// </summary>
    Embed,
    
    /// <summary>
    /// A using statement or namespace import.
    /// </summary>
    Using
}

/// <summary>
/// Represents a cross-file relationship in the project.
/// </summary>
public class CrossFileRelationship
{
    /// <summary>
    /// Gets the source file path.
    /// </summary>
    public string SourceFile { get; init; } = string.Empty;

    /// <summary>
    /// Gets the target file path.
    /// </summary>
    public string TargetFile { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of relationship.
    /// </summary>
    public RelationshipType RelationshipType { get; init; }

    /// <summary>
    /// Gets additional context about the relationship.
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// Represents the type of cross-file relationship.
/// </summary>
public enum RelationshipType
{
    NamespaceUsage,
    TypeDependency,
    ResourceDependency,
    ConfigurationDependency,
    InheritanceRelationship,
    CompositionRelationship
}

/// <summary>
/// Represents a configuration file in the project.
/// </summary>
public class ConfigurationFile
{
    /// <summary>
    /// Gets the configuration file path.
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// Gets the type of configuration.
    /// </summary>
    public ConfigurationType ConfigurationType { get; init; }

    /// <summary>
    /// Gets the configuration settings.
    /// </summary>
    public IReadOnlyDictionary<string, object> Settings { get; init; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents the type of configuration file.
/// </summary>
public enum ConfigurationType
{
    AppSettings,
    WebConfig,
    AppConfig,
    Project,
    Package,
    Build
}

/// <summary>
/// Represents a project dependency.
/// </summary>
public class ProjectDependency
{
    /// <summary>
    /// Gets the dependency name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Gets the dependency version.
    /// </summary>
    public string? Version { get; init; }

    /// <summary>
    /// Gets the dependency source.
    /// </summary>
    public DependencySource Source { get; init; }

    /// <summary>
    /// Gets the dependency type.
    /// </summary>
    public DependencyType DependencyType { get; init; }
}

/// <summary>
/// Represents the source of a dependency.
/// </summary>
public enum DependencySource
{
    Local,
    Remote,
    System,
    Package
}

/// <summary>
/// Represents the type of dependency.
/// </summary>
public enum DependencyType
{
    Compilation,
    Runtime,
    Test,
    Build,
    Development
}
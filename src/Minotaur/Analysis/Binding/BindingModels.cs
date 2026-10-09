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

using System.Text.Json;

namespace Minotaur.Analysis.Binding;

/// <summary>
/// Declarative binding semantics for one grammar, parsed from grammar metadata.
/// The binder is fully driven by this profile: adding binding for a new grammar
/// means adding annotations, not engine code (Minotaur issue #121).
/// </summary>
public class GrammarBindingProfile
{
    /// <summary>
    /// Grammar the profile was derived from (null when constructed directly).
    /// </summary>
    public GrammarGeneration.Models.Grammar? Grammar { get; private set; }

    /// <summary>
    /// Names of productions that open a new scope (e.g. method bodies, blocks).
    /// </summary>
    public HashSet<string> ScopeOpeners { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Names of productions that declare a named symbol. The declared name is
    /// taken from the profile's role annotations on that production.
    /// </summary>
    public HashSet<string> DeclarationRules { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Names of productions that resolve to a container name for qualified-name
    /// purposes (e.g. namespace/package/module declarations).
    /// </summary>
    public HashSet<string> ContainerRules { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Names of productions that import names into the file scope
    /// (using/import/include statements).
    /// </summary>
    public HashSet<string> ImportRules { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Token types that carry a qualified name reference (the token's text is
    /// the dotted name being referenced, e.g. a base-class list).
    /// </summary>
    public HashSet<string> ReferenceTokenTypes { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Production name -> role -> token type. Roles recognized by the binder:
    /// "name" (declared symbol name), "container" (enclosing container name),
    /// "import" (imported name), "reference" (qualified reference). Roles
    /// without an annotation fall back to the "identifier" token type.
    /// </summary>
    public Dictionary<string, Dictionary<string, string>> RuleRoles { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Token types that mark a declaration production as a type declaration
    /// (used to build cross-file type identity for SpecTreeGenerator-class consumers).
    /// </summary>
    public HashSet<string> TypeDeclarationTokens { get; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets the role -> token type mapping for a production, if annotated.
    /// </summary>
    public bool TryGetRoles(string ruleName, out Dictionary<string, string> roles)
    {
        return RuleRoles.TryGetValue(ruleName, out roles!);
    }

    /// <summary>
    /// Builds a binding profile from a grammar's metadata. Recognized key:
    /// <c>binding</c> containing JSON in the shape
    /// <code>
    /// {
    ///   "scopeOpeners": ["method_body", "block"],
    ///   "declarations": { "class_declaration": { "name": "identifier" }, ... },
    ///   "containers": ["namespace_declaration"],
    ///   "imports": { "using_directive": { "import": "qualified_name" } },
    ///   "references": { "base_list": { "reference": "identifier" } },
    ///   "typeDeclarationTokens": ["class", "interface"]
    /// }
    /// </code>
    /// </summary>
    public static GrammarBindingProfile FromGrammar(GrammarGeneration.Models.Grammar grammar)
    {
        var profile = new GrammarBindingProfile { Grammar = grammar };

        if (!grammar.Metadata.TryGetValue("binding", out var json) || string.IsNullOrWhiteSpace(json))
        {
            return profile;
        }

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        foreach (var element in ReadArray(root, "scopeOpeners"))
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                profile.ScopeOpeners.Add(element.GetString()!);
            }
        }

        foreach (var (ruleName, roles) in ReadObject(root, "declarations"))
        {
            profile.DeclarationRules.Add(ruleName);
            CollectRoles(profile, ruleName, roles);
        }

        foreach (var (ruleName, roles) in ReadObject(root, "imports"))
        {
            profile.ImportRules.Add(ruleName);
            CollectRoles(profile, ruleName, roles);
        }

        foreach (var (ruleName, roles) in ReadObject(root, "references"))
        {
            CollectRoles(profile, ruleName, roles);
        }

        if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("containers", out var containers))
        {
            if (containers.ValueKind == JsonValueKind.Array)
            {
                foreach (var element in containers.EnumerateArray())
                {
                    if (element.ValueKind == JsonValueKind.String)
                    {
                        profile.ContainerRules.Add(element.GetString()!);
                    }
                }
            }
            else if (containers.ValueKind == JsonValueKind.Object)
            {
                foreach (var (ruleName, roles) in containers.EnumerateObject())
                {
                    profile.ContainerRules.Add(ruleName);
                    CollectRoles(profile, ruleName, roles);
                }
            }
        }

        foreach (var element in ReadArray(root, "typeDeclarationTokens"))
        {
            if (element.ValueKind == JsonValueKind.String)
            {
                profile.TypeDeclarationTokens.Add(element.GetString()!);
            }
        }

        return profile;
    }

    private static void CollectRoles(GrammarBindingProfile profile, string ruleName, JsonElement roles)
    {
        if (roles.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        foreach (var property in roles.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.String)
            {
                SetRole(profile, ruleName, property.Name, property.Value.GetString()!);
            }
        }
    }

    internal static void SetRole(GrammarBindingProfile profile, string ruleName, string role, string tokenType)
    {
        if (!profile.RuleRoles.TryGetValue(ruleName, out var roles))
        {
            roles = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            profile.RuleRoles[ruleName] = roles;
        }

        roles[role] = tokenType;
    }

    private static IEnumerable<JsonElement> ReadArray(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(name, out var array) &&
            array.ValueKind == JsonValueKind.Array)
        {
            return array.EnumerateArray();
        }

        return Enumerable.Empty<JsonElement>();
    }

    private static IEnumerable<(string RuleName, JsonElement Roles)> ReadObject(JsonElement root, string name)
    {
        if (root.ValueKind == JsonValueKind.Object &&
            root.TryGetProperty(name, out var obj) &&
            obj.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in obj.EnumerateObject())
            {
                yield return (property.Name, property.Value);
            }
        }
    }
}

/// <summary>
/// A symbol declared by binding, with a stable identifier usable across files.
/// </summary>
public class BindingSymbol
{
    /// <summary>
    /// Stable, deterministic symbol id: language + container + name, so the
    /// same declaration in two parse runs (or two files) maps to one identity.
    /// </summary>
    public string SymbolId { get; }

    public string Name { get; }

    /// <summary>
    /// Dotted container path (namespace/package/module chain), empty when none.
    /// </summary>
    public string Container { get; }

    public string FullyQualifiedName => string.IsNullOrEmpty(Container) ? Name : $"{Container}.{Name}";

    /// <summary>
    /// Kind of declaration, taken from the profile's type-declaration token
    /// when present, otherwise the declaring production name.
    /// </summary>
    public string Kind { get; }

    /// <summary>
    /// File the symbol was declared in (for cross-file binding).
    /// </summary>
    public string File { get; }

    public int Line { get; }

    public int Column { get; }

    public BindingSymbol(string language, string name, string container, string kind, string file, int line, int column)
    {
        Name = name;
        Container = container;
        Kind = kind;
        File = file;
        Line = line;
        Column = column;
        SymbolId = $"{language}|{FullyQualifiedName}";
    }
}

/// <summary>
/// A resolved or unresolved reference recorded by the binder.
/// </summary>
public class BindingReference
{
    public string Name { get; }

    /// <summary>
    /// Resolved symbol id, or null when the reference could not be resolved.
    /// </summary>
    public string? ResolvedSymbolId { get; }

    /// <summary>
    /// True when the reference was resolved against a declared symbol.
    /// </summary>
    public bool IsResolved => ResolvedSymbolId != null;

    public string File { get; }

    public int Line { get; }

    public int Column { get; }

    public BindingReference(string name, string? resolvedSymbolId, string file, int line, int column)
    {
        Name = name;
        ResolvedSymbolId = resolvedSymbolId;
        File = file;
        Line = line;
        Column = column;
    }
}

/// <summary>
/// Scope-aware symbol table: a chain of scopes from file (import-visible) scope
/// down to block scopes. Name lookup walks the chain outward, then falls back
/// to qualified-name lookup across all files in the binding session.
/// </summary>
public class ScopeAwareSymbolTable
{
    private readonly Dictionary<string, List<BindingSymbol>> _symbolsByContainer = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<BindingSymbol>> _symbolsBySimpleName = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<BindingSymbol> _allSymbols = new();

    public IReadOnlyList<BindingSymbol> AllSymbols => _allSymbols;

    /// <summary>
    /// Registers a declared symbol. Same declaration re-registered (e.g. on
    /// incremental re-bind) updates in place because SymbolId is deterministic.
    /// </summary>
    public void AddSymbol(BindingSymbol symbol)
    {
        if (_allSymbols.Any(s => s.SymbolId == symbol.SymbolId))
        {
            return;
        }

        _allSymbols.Add(symbol);

        if (!_symbolsByContainer.TryGetValue(symbol.Container, out var inContainer))
        {
            inContainer = new List<BindingSymbol>();
            _symbolsByContainer[symbol.Container] = inContainer;
        }

        inContainer.Add(symbol);

        if (!_symbolsBySimpleName.TryGetValue(symbol.Name, out var byName))
        {
            byName = new List<BindingSymbol>();
            _symbolsBySimpleName[symbol.Name] = byName;
        }

        byName.Add(symbol);
    }

    /// <summary>
    /// Resolves a possibly qualified name using a scope chain of container paths
    /// (innermost first). Handles same-named symbols in different containers.
    /// </summary>
    public BindingSymbol? Resolve(string name, IReadOnlyList<string> scopeContainers, IReadOnlyList<string> importedContainers)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        if (name.Contains('.'))
        {
            var container = GetContainerOf(name);
            var simpleName = name[(container.Length + 1)..];
            return _symbolsByContainer.TryGetValue(container, out var candidates)
                ? candidates.FirstOrDefault(c => c.Name == simpleName)
                : null;
        }

        foreach (var container in scopeContainers)
        {
            var symbol = FindInContainer(container, name);
            if (symbol != null)
            {
                return symbol;
            }
        }

        foreach (var container in importedContainers)
        {
            var symbol = FindInContainer(container, name);
            if (symbol != null)
            {
                return symbol;
            }
        }

        var matches = _symbolsBySimpleName.TryGetValue(name, out var byName) ? byName : null;
        return matches is { Count: 1 } ? matches[0] : null;
    }

    private BindingSymbol? FindInContainer(string container, string name)
    {
        return _symbolsByContainer.TryGetValue(container, out var candidates)
            ? candidates.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal))
            : null;
    }

    private static string GetContainerOf(string qualifiedName)
    {
        var lastDot = qualifiedName.LastIndexOf('.');
        return lastDot <= 0 ? string.Empty : qualifiedName[..lastDot];
    }
}

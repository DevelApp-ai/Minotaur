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

using Minotaur.Core;

namespace Minotaur.Analysis.Binding;

/// <summary>
/// Result of binding one or more files.
/// </summary>
public class BindingResult
{
    public List<BindingSymbol> Symbols { get; } = new();

    public List<BindingReference> References { get; } = new();

    /// <summary>
    /// References that could not be resolved to a declared symbol.
    /// </summary>
    public IEnumerable<BindingReference> UnresolvedReferences => References.Where(r => !r.IsResolved);
}

/// <summary>
/// Grammar-driven semantic binder. All binding semantics (which productions
/// declare symbols, open scopes, name containers, import names, or carry
/// references) come from a <see cref="GrammarBindingProfile"/> derived from
/// grammar annotations — the engine itself contains no per-language logic
/// (Minotaur issue #121).
/// </summary>
public class SemanticBinder
{
    private const string NameRole = "name";
    private const string ContainerRole = "container";
    private const string ImportRole = "import";
    private const string ReferenceRole = "reference";

    private readonly ScopeAwareSymbolTable _table = new();
    private readonly BindingResult _result = new();
    private readonly string _language;

    public SemanticBinder(string language, GrammarBindingProfile profile)
    {
        _language = language;
        Profile = profile;
    }

    public GrammarBindingProfile Profile { get; }

    public BindingResult Result => _result;

    public ScopeAwareSymbolTable Table => _table;

    /// <summary>
    /// Binds a parsed file (cognitive graph root) into the session. Declarations
    /// are collected first for all files; call <see cref="ResolvePendingReferences"/>
    /// after the last file to resolve cross-file references.
    /// </summary>
    public BindingResult BindFile(CognitiveGraphNode root, string file)
    {
        ArgumentNullException.ThrowIfNull(root);

        var context = new FileBindingContext(file);
        Walk(root, context);
        return _result;
    }

    /// <summary>
    /// Resolves all recorded references against the symbols declared so far.
    /// Call once after all files are bound.
    /// </summary>
    public void ResolvePendingReferences()
    {
        foreach (var pending in _pendingReferences)
        {
            var resolved = _table.Resolve(pending.Name, pending.Containers, pending.Imports);
            _result.References.Add(new BindingReference(
                pending.Name,
                resolved?.SymbolId,
                pending.File,
                pending.Line,
                pending.Column));
        }

        _pendingReferences.Clear();
    }

    private readonly List<PendingReference> _pendingReferences = new();

    private void Walk(CognitiveGraphNode node, FileBindingContext context)
    {
        var isNonTerminal = node is NonTerminalNode nonTerminal;
        var ruleName = isNonTerminal ? nonTerminal.RuleName : string.Empty;

        if (isNonTerminal && Profile.DeclarationRules.Contains(ruleName))
        {
            HandleDeclaration(nonTerminal, context);
        }

        if (isNonTerminal && Profile.ImportRules.Contains(ruleName))
        {
            HandleImport(nonTerminal, context);
        }

        if (isNonTerminal && Profile.ContainerRules.Contains(ruleName))
        {
            HandleContainer(nonTerminal, context);
            return;
        }

        if (isNonTerminal && Profile.TryGetRoles(ruleName, out var roles) &&
            roles.TryGetValue(ReferenceRole, out var referenceTokenType))
        {
            HandleReference(nonTerminal, referenceTokenType, context);
        }
        else if (!isNonTerminal && node is TerminalNode terminal &&
                 Profile.ReferenceTokenTypes.Contains(terminal.TokenType))
        {
            RecordReference(terminal.Text, context, terminal.SourcePosition);
        }

        foreach (var child in node.Children)
        {
            Walk(child, context);
        }
    }

    private void HandleDeclaration(NonTerminalNode node, FileBindingContext context)
    {
        Profile.TryGetRoles(node.RuleName, out var roles);
        var nameTokenType = roles?.GetValueOrDefault(NameRole);
        var name = FindNameText(node, nameTokenType);
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        var kind = FindKind(node, node.RuleName);
        var position = node.SourcePosition;
        var symbol = new BindingSymbol(
            _language,
            name,
            context.ContainerPath,
            kind,
            context.File,
            position?.Line is > 0 ? position.Line : 1,
            position?.Column is > 0 ? position.Column : 1);

        _table.AddSymbol(symbol);
        _result.Symbols.Add(symbol);
    }

    private void HandleImport(NonTerminalNode node, FileBindingContext context)
    {
        Profile.TryGetRoles(node.RuleName, out var roles);
        var importTokenType = roles?.GetValueOrDefault(ImportRole);
        var importName = FindNameText(node, importTokenType);
        if (!string.IsNullOrEmpty(importName))
        {
            context.AddImport(importName);
        }
    }

    private void HandleContainer(NonTerminalNode node, FileBindingContext context)
    {
        Profile.TryGetRoles(node.RuleName, out var roles);
        var containerTokenType = roles?.GetValueOrDefault(ContainerRole);
        var containerName = FindNameText(node, containerTokenType);
        if (string.IsNullOrEmpty(containerName))
        {
            return;
        }

        context.PushContainer(containerName);
        foreach (var child in node.Children)
        {
            Walk(child, context);
        }
        context.PopContainer();
    }

    private void HandleReference(NonTerminalNode node, string referenceTokenType, FileBindingContext context)
    {
        var name = FindNameText(node, referenceTokenType);
        if (!string.IsNullOrEmpty(name))
        {
            RecordReference(name, context, node.SourcePosition);
        }
    }

    private void RecordReference(string name, FileBindingContext context, SourcePosition? position)
    {
        _pendingReferences.Add(new PendingReference(
            name,
            context.File,
            position?.Line is > 0 ? position.Line : 1,
            position?.Column is > 0 ? position.Column : 1,
            context.VisibleContainers(),
            context.Imports.ToList()));
    }

    /// <summary>
    /// Finds the text carrying a name inside a production: the first terminal
    /// with the annotated token type, falling back to the first identifier
    /// node (IdentifierNode or a terminal typed as "identifier").
    /// </summary>
    private static string? FindNameText(NonTerminalNode node, string? tokenType)
    {
        if (!string.IsNullOrEmpty(tokenType))
        {
            var typed = FindTerminal(node, t => string.Equals(t.TokenType, tokenType, StringComparison.OrdinalIgnoreCase));
            if (typed != null)
            {
                return typed.Text;
            }
        }

        var identifier = FindTerminal(node, t =>
            t is IdentifierNode ||
            string.Equals(t.TokenType, "identifier", StringComparison.OrdinalIgnoreCase));
        return identifier?.Text;
    }

    private static TerminalNode? FindTerminal(CognitiveGraphNode node, Func<TerminalNode, bool> predicate)
    {
        if (node is TerminalNode terminal && predicate(terminal))
        {
            return terminal;
        }

        foreach (var child in node.Children)
        {
            var found = FindTerminal(child, predicate);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    /// <summary>
    /// Determines the declaration kind: the keyword token matching a
    /// type-declaration annotation (e.g. "class", "interface"), else the rule name.
    /// </summary>
    private string FindKind(NonTerminalNode node, string ruleName)
    {
        foreach (var token in Profile.TypeDeclarationTokens)
        {
            if (FindTerminal(node, t => string.Equals(t.Text, token, StringComparison.OrdinalIgnoreCase)) != null)
            {
                return token;
            }
        }

        return ruleName;
    }

    private sealed class PendingReference
    {
        public PendingReference(string name, string file, int line, int column, List<string> containers, List<string> imports)
        {
            Name = name;
            File = file;
            Line = line;
            Column = column;
            Containers = containers;
            Imports = imports;
        }

        public string Name { get; }

        public string File { get; }

        public int Line { get; }

        public int Column { get; }

        public List<string> Containers { get; }

        public List<string> Imports { get; }
    }

    /// <summary>
    /// Per-file binding state: container chain, imports, and visible names.
    /// </summary>
    private sealed class FileBindingContext
    {
        private readonly Stack<string> _containerStack = new();
        private readonly string _file;

        public FileBindingContext(string file)
        {
            _file = file;
        }

        public string File => _file;

        public List<string> Imports { get; } = new();

        public string ContainerPath => string.Join(".", _containerStack.Reverse());

        public void PushContainer(string name) => _containerStack.Push(name);

        public void PopContainer() => _containerStack.Pop();

        public void AddImport(string importName)
        {
            if (!Imports.Contains(importName))
            {
                Imports.Add(importName);
            }
        }

        /// <summary>
        /// Containers visible at the current point, innermost first: the
        /// current container chain up to the root.
        /// </summary>
        public List<string> VisibleContainers()
        {
            var containers = new List<string>();
            var path = string.Empty;
            foreach (var segment in _containerStack.Reverse())
            {
                path = string.IsNullOrEmpty(path) ? segment : $"{path}.{segment}";
                containers.Add(path);
            }

            return containers;
        }
    }
}

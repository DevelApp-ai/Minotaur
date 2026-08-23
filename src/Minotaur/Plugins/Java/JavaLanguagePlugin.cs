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

using System;
using System.Collections.Generic;
using Minotaur.Core;
using Minotaur.Plugins;

namespace Minotaur.Plugins.Java;

/// <summary>
/// Java language plugin for Minotaur
/// Provides Java-specific parsing, unparsing, and code generation support
/// </summary>
public class JavaLanguagePlugin : ILanguagePlugin
{
    private readonly Dictionary<string, string> _fileExtensions;
    private readonly HashSet<string> _keywords;

    /// <summary>
    /// Initializes a new instance of the JavaLanguagePlugin class.
    /// </summary>
    public JavaLanguagePlugin()
    {
        LanguageId = "java";
        LanguageName = "Java";
        _fileExtensions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {".java", "java"},
            {".jav", "java"}
        };

        _keywords = new HashSet<string>(StringComparer.Ordinal)
        {
            "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char",
            "class", "const", "continue", "default", "do", "double", "else", "enum",
            "extends", "final", "finally", "float", "for", "goto", "if", "implements",
            "import", "instanceof", "int", "interface", "long", "native", "new",
            "package", "private", "protected", "public", "return", "short", "static",
            "strictfp", "super", "switch", "synchronized", "this", "throw", "throws",
            "transient", "try", "void", "volatile", "while"
        };
    }

    /// <summary>
    /// Gets the unique identifier for this language.
    /// </summary>
    public string LanguageId { get; }

    /// <summary>
    /// Gets the display name for this language.
    /// </summary>
    public string LanguageName { get; }

    /// <summary>
    /// Gets the file extensions supported by this language plugin.
    /// </summary>
    public IReadOnlyDictionary<string, string> FileExtensions => _fileExtensions;

    /// <summary>
    /// Gets the set of keywords for this language.
    /// </summary>
    public IReadOnlySet<string> Keywords => _keywords;

    /// <summary>
    /// Gets the default file extension for this language.
    /// </summary>
    public string DefaultFileExtension => ".java";

    /// <summary>
    /// Gets the priority of this plugin (higher priority plugins are tried first).
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Determines if this plugin can handle the specified file extension.
    /// </summary>
    /// <param name="extension">The file extension to check.</param>
    /// <returns>True if this plugin can handle the file extension.</returns>
    public bool CanHandleExtension(string extension)
    {
        return _fileExtensions.ContainsKey(extension);
    }

    /// <summary>
    /// Determines if the specified text is a keyword in this language.
    /// </summary>
    /// <param name="text">The text to check.</param>
    /// <returns>True if the text is a keyword.</returns>
    public bool IsKeyword(string text)
    {
        return _keywords.Contains(text);
    }

    /// <summary>
    /// Determines if the specified character can start an identifier in this language.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character can start an identifier.</returns>
    public bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_' || c == '$';
    }

    /// <summary>
    /// Determines if the specified character can be part of an identifier in this language.
    /// </summary>
    /// <param name="c">The character to check.</param>
    /// <returns>True if the character can be part of an identifier.</returns>
    public bool IsIdentifierChar(char c)
    {
        return IsIdentifierStart(c) || char.IsDigit(c);
    }

    /// <summary>
    /// Gets the unparse strategy for this language.
    /// </summary>
    public IUnparseStrategy UnparseStrategy => new JavaUnparseStrategy();

    /// <summary>
    /// Gets the validation strategy for this language.
    /// </summary>
    public IValidationStrategy ValidationStrategy => new JavaValidationStrategy();

    /// <summary>
    /// Gets the refactoring operations supported by this language.
    /// </summary>
    public IEnumerable<IRefactoringOperation> RefactoringOperations
    {
        get
        {
            yield return new ExtractMethodOperation(this);
            yield return new InlineMethodOperation(this);
            yield return new RenameSymbolOperation(this);
        }
    }

    /// <summary>
    /// Creates a new cognitive graph for the specified source code.
    /// </summary>
    /// <param name="sourceCode">The source code to parse.</param>
    /// <param name="fileName">The name of the file being parsed.</param>
    /// <returns>A cognitive graph representing the source code.</returns>
    public CognitiveGraphNode CreateCognitiveGraph(string sourceCode, string fileName)
    {
        var root = new NonTerminalNode("compilation_unit", 0);
        root.Metadata["language"] = LanguageId;
        root.Metadata["fileName"] = fileName;
        root.Metadata["sourceCode"] = sourceCode;

        var tokens = Tokenize(sourceCode);
        foreach (var token in tokens)
        {
            CognitiveGraphNode node;
            if (IsKeyword(token.Text)) node = new TerminalNode(token.Text, "keyword");
            else if (IsIdentifier(token.Text)) node = new IdentifierNode(token.Text);
            else if (IsStringLiteral(token.Text)) node = new LiteralNode(token.Text, "string", token.Text);
            else if (IsNumberLiteral(token.Text)) node = new LiteralNode(token.Text, "number", ParseNumber(token.Text));
            else if (IsOperator(token.Text)) node = new TerminalNode(token.Text, "operator");
            else if (IsPunctuation(token.Text)) node = new TerminalNode(token.Text, "punctuation");
            else node = new TerminalNode(token.Text, "token");
            root.AddChild(node);
        }
        return root;
    }

    /// <summary>
    /// Tokenizes the specified source code.
    /// </summary>
    public List<Token> Tokenize(string sourceCode)
    {
        var tokens = new List<Token>();
        int position = 0;
        while (position < sourceCode.Length)
        {
            char current = sourceCode[position];
            if (char.IsWhiteSpace(current)) { position++; continue; }
            if (current == '/' && position + 1 < sourceCode.Length)
            {
                char next = sourceCode[position + 1];
                if (next == '/') {
                    int end = sourceCode.IndexOf('\n', position);
                    if (end == -1) end = sourceCode.Length;
                    tokens.Add(new Token("comment", sourceCode.Substring(position, end - position), position));
                    position = end; continue;
                } else if (next == '*') {
                    int end = sourceCode.IndexOf("*/", position + 2, StringComparison.Ordinal);
                    if (end == -1) end = sourceCode.Length; else end += 2;
                    tokens.Add(new Token("comment", sourceCode.Substring(position, end - position), position));
                    position = end; continue;
                }
            }
            if (current == '"' || current == '\'') {
                char quote = current; int start = position; position++;
                while (position < sourceCode.Length) {
                    current = sourceCode[position];
                    if (current == '\\') { position += 2; continue; }
                    if (current == quote) { position++; break; }
                    position++;
                }
                tokens.Add(new Token("string", sourceCode.Substring(start, position - start), start));
                continue;
            }
            if (char.IsDigit(current) || (current == '.' && position + 1 < sourceCode.Length && char.IsDigit(sourceCode[position + 1])))
            {
                int start = position; bool isFloat = false;
                if (current == '.') { isFloat = true; position++; }
                while (position < sourceCode.Length) {
                    current = sourceCode[position];
                    if (char.IsDigit(current)) position++;
                    else if (current == '.' && !isFloat) { isFloat = true; position++; }
                    else if ((current == 'e' || current == 'E') && position + 1 < sourceCode.Length &&
                             (char.IsDigit(sourceCode[position + 1]) || sourceCode[position + 1] == '+' || sourceCode[position + 1] == '-')) {
                        position++;
                        if (sourceCode[position] == '+' || sourceCode[position] == '-') position++;
                        while (position < sourceCode.Length && char.IsDigit(sourceCode[position])) position++;
                        break;
                    } else break;
                }
                tokens.Add(new Token("number", sourceCode.Substring(start, position - start), start));
                continue;
            }
            if (IsIdentifierStart(current)) {
                int start = position;
                while (position < sourceCode.Length && IsIdentifierChar(sourceCode[position])) position++;
                string text = sourceCode.Substring(start, position - start);
                tokens.Add(new Token(IsKeyword(text) ? "keyword" : "identifier", text, start));
                continue;
            }
            if (IsOperatorOrPunctuation(current)) {
                if (position + 1 < sourceCode.Length) {
                    string twoChars = sourceCode.Substring(position, 2);
                    if (IsMultiCharOperator(twoChars)) {
                        tokens.Add(new Token("operator", twoChars, position));
                        position += 2; continue;
                    }
                }
                tokens.Add(new Token("operator", sourceCode.Substring(position, 1), position));
                position++; continue;
            }
            tokens.Add(new Token("unknown", sourceCode.Substring(position, 1), position));
            position++;
        }
        return tokens;
    }

    public bool IsIdentifier(string text) {
        if (string.IsNullOrEmpty(text)) return false;
        if (!IsIdentifierStart(text[0])) return false;
        for (int i = 1; i < text.Length; i++) if (!IsIdentifierChar(text[i])) return false;
        return !IsKeyword(text);
    }

    public bool IsStringLiteral(string text) => text.StartsWith("\"") && text.EndsWith("\"");
    public bool IsNumberLiteral(string text) => double.TryParse(text, out _);
    public bool IsOperatorOrPunctuation(char c) => "+-*/%=<>!&|^~.,;:()[]{}?@#".IndexOf(c) >= 0;
    public bool IsMultiCharOperator(string text) => text == "==" || text == "!=" || text == "<=" || text == ">=" ||
               text == "&&" || text == "||" || text == "++" || text == "--" ||
               text == "+=" || text == "-=" || text == "*=" || text == "/=";
    public object ParseNumber(string text) => int.TryParse(text, out int iv) ? iv : double.TryParse(text, out double dv) ? dv : text;

    /// <summary>
    /// Disposes the plugin and releases any resources.
    /// </summary>
    public void Dispose() { }

    /// <summary>Token structure</summary>
    public class Token
    {
        public string Type { get; }
        public string Text { get; }
        public int Position { get; }
        public Token(string type, string text, int position) { Type = type; Text = text; Position = position; }
    }

    /// <summary>Java unparse strategy</summary>
    public class JavaUnparseStrategy : IUnparseStrategy
    {
        public string Unparse(CognitiveGraphNode node)
        {
            var sb = new System.Text.StringBuilder();
            UnparseNode(node, sb);
            return sb.ToString();
        }
        private void UnparseNode(CognitiveGraphNode node, System.Text.StringBuilder sb)
        {
            if (node is TerminalNode terminal) sb.Append(terminal.Text);
            else if (node is IdentifierNode identifier) sb.Append(identifier.Name);
            else if (node is LiteralNode literal) sb.Append(literal.Text);
            else if (node is NonTerminalNode nonTerminal) foreach (var child in nonTerminal.Children) UnparseNode(child, sb);
        }
    }

    /// <summary>Java validation strategy</summary>
    public class JavaValidationStrategy : IValidationStrategy
    {
        public IEnumerable<ValidationError> Validate(CognitiveGraphNode node)
        {
            var errors = new List<ValidationError>();
            ValidateNode(node, errors);
            return errors;
        }
        private void ValidateNode(CognitiveGraphNode node, List<ValidationError> errors) { }
    }

    /// <summary>Validation error structure</summary>
    public class ValidationError
    {
        public string Message { get; set; }
        public string Type { get; set; }
        public int Line { get; set; }
        public int Column { get; set; }
    }

    /// <summary>Refactoring operation base class</summary>
    public abstract class RefactoringOperation : IRefactoringOperation
    {
        protected readonly JavaLanguagePlugin Plugin;
        protected RefactoringOperation(JavaLanguagePlugin plugin) { Plugin = plugin; }
        public abstract string Name { get; }
        public abstract string Description { get; }
        public abstract bool CanApply(CognitiveGraphNode node);
        public abstract CognitiveGraphNode Apply(CognitiveGraphNode node);
    }

    /// <summary>Extract method refactoring</summary>
    public class ExtractMethodOperation : RefactoringOperation
    {
        public ExtractMethodOperation(JavaLanguagePlugin plugin) : base(plugin) { }
        public override string Name => "Extract Method";
        public override string Description => "Extracts selected code into a new method";
        public override bool CanApply(CognitiveGraphNode node) => true;
        public override CognitiveGraphNode Apply(CognitiveGraphNode node) => node;
    }

    /// <summary>Inline method refactoring</summary>
    public class InlineMethodOperation : RefactoringOperation
    {
        public InlineMethodOperation(JavaLanguagePlugin plugin) : base(plugin) { }
        public override string Name => "Inline Method";
        public override string Description => "Inlines a method call with its implementation";
        public override bool CanApply(CognitiveGraphNode node) => true;
        public override CognitiveGraphNode Apply(CognitiveGraphNode node) => node;
    }

    /// <summary>Rename symbol refactoring</summary>
    public class RenameSymbolOperation : RefactoringOperation
    {
        public RenameSymbolOperation(JavaLanguagePlugin plugin) : base(plugin) { }
        public override string Name => "Rename Symbol";
        public override string Description => "Renames a symbol throughout the code";
        public override bool CanApply(CognitiveGraphNode node) => node is IdentifierNode;
        public override CognitiveGraphNode Apply(CognitiveGraphNode node) => node;
    }
}

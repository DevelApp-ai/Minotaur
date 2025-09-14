using CognitiveGraph;
using CognitiveGraph.Schema;
using CognitiveGraph.Accessors;
using GolemCognitiveGraph.Visitors;

namespace GolemCognitiveGraph.Core;

/// <summary>
/// Represents a terminal node in the cognitive graph (leaf node with text content).
/// </summary>
public class TerminalNode : CognitiveGraphNode
{
    /// <summary>
    /// Gets or sets the text content of this terminal node.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the token type of this terminal node.
    /// </summary>
    public string TokenType { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the TerminalNode class.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="tokenType">The token type.</param>
    public TerminalNode(string text = "", string tokenType = "") : base()
    {
        Text = text;
        TokenType = tokenType;
        NodeType = "terminal";

        // Store text in metadata for unparser access
        Metadata["text"] = text;
        Metadata["tokenType"] = tokenType;
    }

    /// <summary>
    /// Initializes a new instance of the TerminalNode class with underlying node.
    /// </summary>
    /// <param name="text">The text content.</param>
    /// <param name="tokenType">The token type.</param>
    /// <param name="underlyingNode">The underlying SymbolNode from CognitiveGraph.</param>
    public TerminalNode(string text, string tokenType, SymbolNode underlyingNode) : base(underlyingNode)
    {
        Text = text;
        TokenType = tokenType;
        NodeType = "terminal";

        // Store text in metadata for unparser access
        Metadata["text"] = text;
        Metadata["tokenType"] = tokenType;
    }

    /// <summary>
    /// Creates a deep copy of this terminal node.
    /// </summary>
    /// <returns>A deep copy of this node.</returns>
    public override CognitiveGraphNode Clone()
    {
        var clone = new TerminalNode(Text, TokenType)
        {
            SourcePosition = SourcePosition
        };

        foreach (var metadata in Metadata)
        {
            clone.Metadata[metadata.Key] = metadata.Value;
        }

        return clone;
    }

    /// <summary>
    /// Accepts a visitor for traversal operations.
    /// </summary>
    /// <param name="visitor">The visitor to accept.</param>
    public override void Accept(ICognitiveGraphVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Returns the text content of this terminal node.
    /// </summary>
    /// <returns>The text content.</returns>
    public override string ToString()
    {
        return $"Terminal[{TokenType}]: '{Text}'";
    }
}

/// <summary>
/// Represents a non-terminal node in the cognitive graph (internal node with children).
/// </summary>
public class NonTerminalNode : CognitiveGraphNode
{
    /// <summary>
    /// Gets or sets the rule name that this non-terminal represents.
    /// </summary>
    public string RuleName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the production index that was used to create this node.
    /// </summary>
    public int ProductionIndex { get; set; } = -1;

    /// <summary>
    /// Initializes a new instance of the NonTerminalNode class.
    /// </summary>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="productionIndex">The production index.</param>
    public NonTerminalNode(string ruleName = "", int productionIndex = -1) : base()
    {
        RuleName = ruleName;
        ProductionIndex = productionIndex;
        NodeType = "nonterminal";

        // Store rule information in metadata
        Metadata["ruleName"] = ruleName;
        Metadata["productionIndex"] = productionIndex;
    }

    /// <summary>
    /// Initializes a new instance of the NonTerminalNode class with underlying node.
    /// </summary>
    /// <param name="ruleName">The rule name.</param>
    /// <param name="productionIndex">The production index.</param>
    /// <param name="underlyingNode">The underlying SymbolNode from CognitiveGraph.</param>
    public NonTerminalNode(string ruleName, int productionIndex, SymbolNode underlyingNode) : base(underlyingNode)
    {
        RuleName = ruleName;
        ProductionIndex = productionIndex;
        NodeType = "nonterminal";

        // Store rule information in metadata
        Metadata["ruleName"] = ruleName;
        Metadata["productionIndex"] = productionIndex;
    }

    /// <summary>
    /// Creates a deep copy of this non-terminal node and its children.
    /// </summary>
    /// <returns>A deep copy of this node.</returns>
    public override CognitiveGraphNode Clone()
    {
        var clone = new NonTerminalNode(RuleName, ProductionIndex)
        {
            SourcePosition = SourcePosition
        };

        foreach (var metadata in Metadata)
        {
            clone.Metadata[metadata.Key] = metadata.Value;
        }

        foreach (var child in Children)
        {
            clone.AddChild(child.Clone());
        }

        return clone;
    }

    /// <summary>
    /// Accepts a visitor for traversal operations.
    /// </summary>
    /// <param name="visitor">The visitor to accept.</param>
    public override void Accept(ICognitiveGraphVisitor visitor)
    {
        visitor.Visit(this);
    }

    /// <summary>
    /// Returns a string representation of this non-terminal node.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"NonTerminal[{RuleName}]: {Children.Count} children";
    }
}

/// <summary>
/// Represents a specialized node for literals (strings, numbers, etc.).
/// </summary>
public class LiteralNode : TerminalNode
{
    /// <summary>
    /// Gets or sets the literal type (string, number, boolean, etc.).
    /// </summary>
    public string LiteralType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the parsed value of the literal.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Initializes a new instance of the LiteralNode class.
    /// </summary>
    /// <param name="text">The literal text.</param>
    /// <param name="literalType">The literal type.</param>
    /// <param name="value">The parsed value.</param>
    public LiteralNode(string text = "", string literalType = "", object? value = null)
        : base(text, "literal")
    {
        LiteralType = literalType;
        Value = value;
        NodeType = "literal";

        // Store literal information in metadata
        Metadata["literalType"] = literalType;
        Metadata["value"] = value ?? text;
    }

    /// <summary>
    /// Creates a deep copy of this literal node.
    /// </summary>
    /// <returns>A deep copy of this node.</returns>
    public override CognitiveGraphNode Clone()
    {
        var clone = new LiteralNode(Text, LiteralType, Value)
        {
            SourcePosition = SourcePosition
        };

        foreach (var metadata in Metadata)
        {
            clone.Metadata[metadata.Key] = metadata.Value;
        }

        return clone;
    }

    /// <summary>
    /// Returns a string representation of this literal node.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"Literal[{LiteralType}]: '{Text}' = {Value}";
    }
}

/// <summary>
/// Represents a specialized node for identifiers.
/// </summary>
public class IdentifierNode : TerminalNode
{
    /// <summary>
    /// Gets or sets the namespace or scope of this identifier.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or sets whether this identifier is qualified (has namespace prefix).
    /// </summary>
    public bool IsQualified { get; set; }

    /// <summary>
    /// Initializes a new instance of the IdentifierNode class.
    /// </summary>
    /// <param name="name">The identifier name.</param>
    /// <param name="namespaceName">The namespace (optional).</param>
    public IdentifierNode(string name = "", string? namespaceName = null)
        : base(name, "identifier")
    {
        Namespace = namespaceName;
        IsQualified = !string.IsNullOrEmpty(namespaceName);
        NodeType = "identifier";

        // Store identifier information in metadata
        Metadata["namespace"] = namespaceName ?? string.Empty;
        Metadata["isQualified"] = IsQualified;
    }

    /// <summary>
    /// Gets the fully qualified name of this identifier.
    /// </summary>
    public string FullName
    {
        get
        {
            return IsQualified ? $"{Namespace}.{Text}" : Text;
        }
    }

    /// <summary>
    /// Creates a deep copy of this identifier node.
    /// </summary>
    /// <returns>A deep copy of this node.</returns>
    public override CognitiveGraphNode Clone()
    {
        var clone = new IdentifierNode(Text, Namespace)
        {
            SourcePosition = SourcePosition
        };

        foreach (var metadata in Metadata)
        {
            clone.Metadata[metadata.Key] = metadata.Value;
        }

        return clone;
    }

    /// <summary>
    /// Returns a string representation of this identifier node.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"Identifier: {FullName}";
    }
}
using CognitiveGraph;
using CognitiveGraph.Schema;
using CognitiveGraph.Accessors;
using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Visitors;
using System.Text;

namespace GolemCognitiveGraph.Unparser;

/// <summary>
/// Core component for generating code from cognitive graph structures.
/// Implements configurable unparsing strategies for different languages and formats.
/// </summary>
public class GraphUnparser : IDisposable
{
    private readonly Dictionary<string, IUnparseStrategy> _strategies = new();
    private readonly UnparseConfiguration _configuration;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the GraphUnparser class.
    /// </summary>
    /// <param name="configuration">The unparsing configuration to use.</param>
    public GraphUnparser(UnparseConfiguration? configuration = null)
    {
        _configuration = configuration ?? new UnparseConfiguration();
        RegisterDefaultStrategies();
    }

    /// <summary>
    /// Registers an unparsing strategy for a specific node type.
    /// </summary>
    /// <param name="nodeType">The node type to register the strategy for.</param>
    /// <param name="strategy">The unparsing strategy.</param>
    public void RegisterStrategy(string nodeType, IUnparseStrategy strategy)
    {
        ArgumentNullException.ThrowIfNull(nodeType);
        ArgumentNullException.ThrowIfNull(strategy);

        _strategies[nodeType] = strategy;
    }

    /// <summary>
    /// Unparses a cognitive graph node to source code.
    /// </summary>
    /// <param name="node">The root node to unparse.</param>
    /// <returns>The generated source code.</returns>
    public string Unparse(CognitiveGraphNode node)
    {
        ArgumentNullException.ThrowIfNull(node);

        var context = new UnparseContext(_configuration);
        var visitor = new UnparseVisitor(_strategies, context);

        node.Accept(visitor);
        return context.GetResult();
    }

    /// <summary>
    /// Unparses a cognitive graph node to source code asynchronously.
    /// </summary>
    /// <param name="node">The root node to unparse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated source code.</returns>
    public async Task<string> UnparseAsync(CognitiveGraphNode node, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);

        return await Task.Run(() => Unparse(node), cancellationToken);
    }

    /// <summary>
    /// Unparses a cognitive graph node and writes the result to a stream.
    /// </summary>
    /// <param name="node">The root node to unparse.</param>
    /// <param name="stream">The stream to write to.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public async Task UnparseToStreamAsync(CognitiveGraphNode node, Stream stream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(node);
        ArgumentNullException.ThrowIfNull(stream);

        var result = await UnparseAsync(node, cancellationToken);
        var bytes = Encoding.UTF8.GetBytes(result);
        await stream.WriteAsync(bytes, cancellationToken);
    }

    private void RegisterDefaultStrategies()
    {
        // Register built-in strategies for common node types
        RegisterStrategy("identifier", new IdentifierUnparseStrategy());
        RegisterStrategy("literal", new LiteralUnparseStrategy());
        RegisterStrategy("operator", new OperatorUnparseStrategy());
        RegisterStrategy("whitespace", new WhitespaceUnparseStrategy());
        RegisterStrategy("comment", new CommentUnparseStrategy());
        RegisterStrategy("block", new BlockUnparseStrategy());
        RegisterStrategy("expression", new ExpressionUnparseStrategy());
        RegisterStrategy("statement", new StatementUnparseStrategy());
        RegisterStrategy("nonterminal", new NonTerminalUnparseStrategy());
        RegisterStrategy("terminal", new TerminalUnparseStrategy());
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            foreach (var strategy in _strategies.Values)
            {
                if (strategy is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _strategies.Clear();
            _disposed = true;
        }
    }
}

/// <summary>
/// Configuration settings for the unparsing process.
/// </summary>
public class UnparseConfiguration
{
    /// <summary>
    /// Gets or sets the indentation string to use.
    /// </summary>
    public string IndentString { get; set; } = "    ";

    /// <summary>
    /// Gets or sets the line ending to use.
    /// </summary>
    public string LineEnding { get; set; } = Environment.NewLine;

    /// <summary>
    /// Gets or sets whether to preserve original formatting when available.
    /// </summary>
    public bool PreserveFormatting { get; set; } = true;

    /// <summary>
    /// Gets or sets whether to include comments in the output.
    /// </summary>
    public bool IncludeComments { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum line length before wrapping.
    /// </summary>
    public int MaxLineLength { get; set; } = 120;

    /// <summary>
    /// Gets or sets whether to format the output for readability.
    /// </summary>
    public bool FormatOutput { get; set; } = true;
}

/// <summary>
/// Context for the unparsing process, maintaining state and output.
/// </summary>
public class UnparseContext
{
    private readonly StringBuilder _output = new();
    private readonly UnparseConfiguration _configuration;
    private int _indentLevel;
    private bool _atLineStart = true;

    /// <summary>
    /// Initializes a new instance of the UnparseContext class.
    /// </summary>
    /// <param name="configuration">The unparsing configuration.</param>
    public UnparseContext(UnparseConfiguration configuration)
    {
        _configuration = configuration;
    }

    /// <summary>
    /// Gets the current indentation level.
    /// </summary>
    public int IndentLevel => _indentLevel;

    /// <summary>
    /// Gets the unparsing configuration.
    /// </summary>
    public UnparseConfiguration Configuration => _configuration;

    /// <summary>
    /// Writes text to the output.
    /// </summary>
    /// <param name="text">The text to write.</param>
    public void Write(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (_atLineStart && _configuration.FormatOutput)
        {
            WriteIndentation();
            _atLineStart = false;
        }

        _output.Append(text);
    }

    /// <summary>
    /// Writes a line to the output.
    /// </summary>
    /// <param name="text">The text to write (optional).</param>
    public void WriteLine(string? text = null)
    {
        if (!string.IsNullOrEmpty(text))
        {
            Write(text);
        }

        _output.Append(_configuration.LineEnding);
        _atLineStart = true;
    }

    /// <summary>
    /// Increases the indentation level.
    /// </summary>
    public void IncreaseIndent()
    {
        _indentLevel++;
    }

    /// <summary>
    /// Decreases the indentation level.
    /// </summary>
    public void DecreaseIndent()
    {
        if (_indentLevel > 0)
        {
            _indentLevel--;
        }
    }

    /// <summary>
    /// Gets the current output as a string.
    /// </summary>
    /// <returns>The generated output.</returns>
    public string GetResult()
    {
        return _output.ToString();
    }

    private void WriteIndentation()
    {
        for (int i = 0; i < _indentLevel; i++)
        {
            _output.Append(_configuration.IndentString);
        }
    }
}

/// <summary>
/// Visitor implementation for unparsing cognitive graph nodes.
/// </summary>
internal class UnparseVisitor : CognitiveGraphVisitorBase
{
    private readonly Dictionary<string, IUnparseStrategy> _strategies;
    private readonly UnparseContext _context;

    public UnparseVisitor(Dictionary<string, IUnparseStrategy> strategies, UnparseContext context)
    {
        _strategies = strategies;
        _context = context;
    }

    public override void Visit(CognitiveGraphNode node)
    {
        // First unparse this node
        if (_strategies.TryGetValue(node.NodeType, out var strategy))
        {
            strategy.UnparseNode(node, _context);
        }
        else
        {
            // Fallback strategy for unknown node types
            _context.Write($"/* Unknown node type: {node.NodeType} */");
        }

        // Then visit children
        VisitChildren(node);
    }
}
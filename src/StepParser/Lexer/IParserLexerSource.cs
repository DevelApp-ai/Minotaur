namespace StepParser.Lexer;

/// <summary>
/// Interface for a source line in the parser/lexer system.
/// </summary>
public interface IParserLexerSourceLine
{
    /// <summary>
    /// Gets the line number.
    /// </summary>
    int LineNumber { get; }

    /// <summary>
    /// Gets the content of the line.
    /// </summary>
    string Content { get; }
}

/// <summary>
/// Interface for a container of source lines.
/// </summary>
public interface IParserLexerSourceContainer
{
    /// <summary>
    /// Gets the source lines.
    /// </summary>
    IEnumerable<IParserLexerSourceLine> SourceLines { get; }
}

/// <summary>
/// Implementation of a source line.
/// </summary>
public class SourceLine : IParserLexerSourceLine
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SourceLine"/> class.
    /// </summary>
    /// <param name="lineNumber">The line number.</param>
    /// <param name="content">The content of the line.</param>
    public SourceLine(int lineNumber, string content)
    {
        LineNumber = lineNumber;
        Content = content;
    }

    /// <inheritdoc/>
    public int LineNumber { get; }

    /// <inheritdoc/>
    public string Content { get; }

    /// <summary>
    /// Returns a string representation of this source line.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        return $"Line {LineNumber}: {Content}";
    }
}

/// <summary>
/// Implementation of a source container.
/// </summary>
public class SourceContainer : IParserLexerSourceContainer
{
    private readonly List<IParserLexerSourceLine> _sourceLines;

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceContainer"/> class.
    /// </summary>
    public SourceContainer()
    {
        _sourceLines = new List<IParserLexerSourceLine>();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SourceContainer"/> class with content.
    /// </summary>
    /// <param name="content">The content to parse into lines.</param>
    public SourceContainer(string content)
    {
        _sourceLines = new List<IParserLexerSourceLine>();
        LoadFromContent(content);
    }

    /// <inheritdoc/>
    public IEnumerable<IParserLexerSourceLine> SourceLines => _sourceLines;

    /// <summary>
    /// Adds a source line.
    /// </summary>
    /// <param name="line">The line to add.</param>
    public void AddLine(IParserLexerSourceLine line)
    {
        _sourceLines.Add(line);
    }

    /// <summary>
    /// Loads content from a string, splitting into lines.
    /// </summary>
    /// <param name="content">The content to load.</param>
    public void LoadFromContent(string content)
    {
        _sourceLines.Clear();
        var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.None);
        for (int i = 0; i < lines.Length; i++)
        {
            _sourceLines.Add(new SourceLine(i, lines[i]));
        }
    }

    /// <summary>
    /// Gets the total number of lines.
    /// </summary>
    public int LineCount => _sourceLines.Count;
}
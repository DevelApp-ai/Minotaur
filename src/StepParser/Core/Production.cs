namespace StepParser.Core;

/// <summary>
/// Represents a production rule in the grammar.
/// </summary>
public class Production
{
    private readonly string _name;
    private readonly List<IProductionPart> _parts;
    private readonly string? _context;
    private Action<string, Dictionary<string, bool>, int>? _callback;

    /// <summary>
    /// Initializes a new instance of the <see cref="Production"/> class.
    /// </summary>
    /// <param name="name">The name of the production.</param>
    /// <param name="context">Optional context for context-sensitive parsing.</param>
    public Production(string name, string? context = null)
    {
        _name = name;
        _parts = new List<IProductionPart>();
        _context = context;
    }

    /// <summary>
    /// Gets the name of the production.
    /// </summary>
    public string Name => _name;

    /// <summary>
    /// Gets the parts of the production.
    /// </summary>
    public IReadOnlyList<IProductionPart> Parts => _parts;

    /// <summary>
    /// Gets the context of the production.
    /// </summary>
    public string? Context => _context;

    /// <summary>
    /// Gets or sets the callback for this production.
    /// </summary>
    public Action<string, Dictionary<string, bool>, int>? Callback
    {
        get => _callback;
        set => _callback = value;
    }

    /// <summary>
    /// Adds a part to the production.
    /// </summary>
    /// <param name="part">The part to add.</param>
    public void AddPart(IProductionPart part)
    {
        _parts.Add(part);
    }

    /// <summary>
    /// Executes the callback for this production.
    /// </summary>
    /// <param name="value">The matched value.</param>
    /// <param name="contextStates">The context states.</param>
    /// <param name="position">The position in the input.</param>
    public void ExecuteCallback(string value, Dictionary<string, bool> contextStates, int position)
    {
        _callback?.Invoke(value, contextStates, position);
    }

    /// <summary>
    /// Returns a string representation of this production.
    /// </summary>
    /// <returns>A string representation.</returns>
    public override string ToString()
    {
        var partsStr = string.Join(" ", _parts.Select(p => p.ToString()));
        return $"Production({_name}: {partsStr})";
    }
}
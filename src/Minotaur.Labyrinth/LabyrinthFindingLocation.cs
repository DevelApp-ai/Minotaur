namespace Minotaur.Labyrinth;

/// <summary>
/// Location information for a finding, carried from the CognitiveGraph via the
/// host adapter (issue #105). Match nodes that know their source position
/// implement this contract; the reporting layer renders the position and uses
/// it for baseline fingerprints when available.
/// </summary>
public interface ILabyrinthFindingLocation
{
    /// <summary>The file (or other source URI) the node originated from.</summary>
    string FilePath { get; }

    /// <summary>The 1-based line the node starts at.</summary>
    int StartLine { get; }

    /// <summary>The 1-based column the node starts at.</summary>
    int StartColumn { get; }
}

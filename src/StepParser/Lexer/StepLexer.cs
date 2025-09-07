using StepParser.Core;
using StepParser.Lexer;
using StepParser.Memory;

namespace StepParser.Lexer;

/// <summary>
/// Factory for creating pooled lexer paths.
/// </summary>
public class LexerPathFactory : IObjectFactory<LexerPath>
{
    /// <inheritdoc/>
    public LexerPath Create()
    {
        return new LexerPath();
    }

    /// <inheritdoc/>
    public void Reset(LexerPath obj)
    {
        obj.Reset();
    }

    /// <inheritdoc/>
    public bool Validate(LexerPath obj)
    {
        return obj.LexerPathId != LexerPath.NOTSET;
    }
}

/// <summary>
/// Unified StepLexer class with zero-copy core.
/// Maintains the same API as the original StepLexer while internally using
/// zero-copy infrastructure for superior performance.
/// </summary>
public class StepLexer
{
    // Zero-copy infrastructure (private, hidden from users)
    private readonly MemoryArena _arena;
    private readonly StringInterner _stringInterner;
    private readonly ObjectPool<LexerPath> _pathPool;

    // Public API preserved for compatibility
    private readonly IParserLexerSourceContainer _sourceLinesContainer;
    private readonly Core.StepParser _stepParser;
    private readonly LexerOptions _lexerOptions;
    private readonly Dictionary<int, LexerPath> _lexerPathMap;
    private readonly List<LexerPath> _invalidatedLexerPaths;
    private int _maximumLexerPathId;

    /// <summary>
    /// Initializes a new instance of the <see cref="StepLexer"/> class.
    /// </summary>
    /// <param name="stepParser">The parser to use.</param>
    /// <param name="lexerOptions">The lexer options.</param>
    /// <param name="sourceLinesContainer">The source lines container.</param>
    public StepLexer(Core.StepParser stepParser, LexerOptions lexerOptions, IParserLexerSourceContainer sourceLinesContainer)
    {
        // Initialize zero-copy infrastructure
        _arena = new MemoryArena(1024 * 1024); // 1MB initial size
        _stringInterner = new StringInterner(_arena);

        // Create object pool for lexer paths
        var pathFactory = new LexerPathFactory();
        _pathPool = new ObjectPool<LexerPath>(pathFactory, _arena, 100, 1000);

        // Preserve existing initialization logic
        _stepParser = stepParser;
        _lexerOptions = lexerOptions;
        _sourceLinesContainer = sourceLinesContainer;
        _lexerPathMap = new Dictionary<int, LexerPath>();
        _invalidatedLexerPaths = new List<LexerPath>();
        _maximumLexerPathId = LexerPath.NOTSET;
        Reset();
    }

    /// <summary>
    /// Resets the lexer.
    /// </summary>
    public void Reset()
    {
        // Clear existing paths and return them to pool
        foreach (var path in _lexerPathMap.Values)
        {
            _pathPool.Release(path);
        }
        _lexerPathMap.Clear();

        // Clear invalidated paths and return them to pool
        foreach (var path in _invalidatedLexerPaths)
        {
            _pathPool.Release(path);
        }
        _invalidatedLexerPaths.Clear();

        _maximumLexerPathId = LexerPath.NOTSET;
        NewStartLexerPath();
    }

    /// <summary>
    /// Gets the next tokens.
    /// </summary>
    /// <returns>An enumerable of token lists.</returns>
    public IEnumerable<List<Token>> NextTokens()
    {
        var tokenList = new List<Token>();
        var currentLexerPaths = new List<LexerPath>();

        while (_lexerPathMap.Count > 0)
        {
            tokenList.Clear();
            CheckForParserLexerPathInvalidation(tokenList);
            CheckForMergeLexerPaths(_lexerPathMap.Values.ToList(), tokenList);
            currentLexerPaths.Clear();
            currentLexerPaths.AddRange(_lexerPathMap.Values);

            foreach (var lexerPath in currentLexerPaths)
            {
                ProcessNextToken(lexerPath, tokenList);
            }

            yield return new List<Token>(tokenList);
        }
    }

    /// <summary>
    /// Invalidates a lexer path.
    /// </summary>
    /// <param name="lexerPathId">The ID of the lexer path to invalidate.</param>
    public void InvalidateLexerPath(int lexerPathId)
    {
        RemoveLexerPath(lexerPathId, true);
    }

    /// <summary>
    /// Creates a new start lexer path using the object pool.
    /// </summary>
    private void NewStartLexerPath()
    {
        var path = _pathPool.Acquire();
        _maximumLexerPathId++;
        path.LexerPathId = _maximumLexerPathId;
        path.ActiveLineNumber = 0;
        path.ActiveCharacterNumber = 0;
        _lexerPathMap[_maximumLexerPathId] = path;
    }

    /// <summary>
    /// Removes a lexer path and optionally marks it as invalidated.
    /// </summary>
    /// <param name="lexerPathId">The ID of the path to remove.</param>
    /// <param name="markAsInvalidated">Whether to mark as invalidated.</param>
    private void RemoveLexerPath(int lexerPathId, bool markAsInvalidated)
    {
        if (_lexerPathMap.TryGetValue(lexerPathId, out var path))
        {
            _lexerPathMap.Remove(lexerPathId);

            if (markAsInvalidated)
            {
                _invalidatedLexerPaths.Add(path);
            }
            else
            {
                _pathPool.Release(path);
            }
        }
    }

    /// <summary>
    /// Checks for parser lexer path invalidation.
    /// </summary>
    /// <param name="tokenList">The token list to add invalidation tokens to.</param>
    private void CheckForParserLexerPathInvalidation(List<Token> tokenList)
    {
        if (_lexerOptions.ReturnLexerPathTokens)
        {
            foreach (var invalidLexerPath in _invalidatedLexerPaths)
            {
                tokenList.Add(new Token(
                    invalidLexerPath.LexerPathId,
                    new Terminal("LEXERPATH_REMOVED", ""),
                    Token.LEXERPATH_EXTERN_REMOVED,
                    invalidLexerPath.ActiveLineNumber,
                    invalidLexerPath.ActiveCharacterNumber));
            }
        }

        // Return invalidated paths to pool
        foreach (var path in _invalidatedLexerPaths)
        {
            _pathPool.Release(path);
        }
        _invalidatedLexerPaths.Clear();
    }

    /// <summary>
    /// Checks for merge opportunities between lexer paths.
    /// </summary>
    /// <param name="lexerPaths">The current lexer paths.</param>
    /// <param name="tokenList">The token list to add merge tokens to.</param>
    private void CheckForMergeLexerPaths(List<LexerPath> lexerPaths, List<Token> tokenList)
    {
        // Group paths by position to find merge candidates
        var pathsByPosition = new Dictionary<string, List<LexerPath>>();

        foreach (var path in lexerPaths)
        {
            var positionKey = $"{path.ActiveLineNumber}-{path.ActiveCharacterNumber}";
            if (!pathsByPosition.ContainsKey(positionKey))
            {
                pathsByPosition[positionKey] = new List<LexerPath>();
            }
            pathsByPosition[positionKey].Add(path);
        }

        // Check for paths that can be merged
        foreach (var pathsAtPosition in pathsByPosition.Values)
        {
            if (pathsAtPosition.Count > 1)
            {
                // Multiple paths at same position - potential merge opportunity
                var basePath = pathsAtPosition[0];

                for (int i = 1; i < pathsAtPosition.Count; i++)
                {
                    var mergePath = pathsAtPosition[i];

                    // Check if paths can be merged (simplified criteria)
                    if (CanMergePaths(basePath, mergePath))
                    {
                        // Create merge token if lexer options require it
                        if (_lexerOptions.ReturnLexerPathTokens)
                        {
                            tokenList.Add(new Token(
                                mergePath.LexerPathId,
                                new Terminal("LEXERPATH_MERGE", ""),
                                basePath.LexerPathId.ToString(),
                                mergePath.ActiveLineNumber,
                                mergePath.ActiveCharacterNumber));
                        }

                        // Remove the merged path
                        RemoveLexerPath(mergePath.LexerPathId, false);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Checks if two lexer paths can be merged.
    /// </summary>
    /// <param name="basePath">The base path to merge into.</param>
    /// <param name="mergePath">The path to potentially merge.</param>
    /// <returns>True if paths can be merged.</returns>
    private bool CanMergePaths(LexerPath basePath, LexerPath mergePath)
    {
        // Simple merge criteria - paths at same position
        return basePath.ActiveLineNumber == mergePath.ActiveLineNumber &&
               basePath.ActiveCharacterNumber == mergePath.ActiveCharacterNumber;
    }

    /// <summary>
    /// Processes the next token for a lexer path.
    /// </summary>
    /// <param name="lexerPath">The lexer path to process.</param>
    /// <param name="tokenList">The token list to add tokens to.</param>
    private void ProcessNextToken(LexerPath lexerPath, List<Token> tokenList)
    {
        // Get current source line
        var sourceLine = GetCurrentSourceLine(lexerPath);
        if (sourceLine == null)
        {
            RemoveLexerPath(lexerPath.LexerPathId, false);
            return;
        }

        // Get valid terminals from parser
        var validTerminals = _stepParser.GetValidTerminals(lexerPath);

        // Try to match terminals
        var matches = FindTerminalMatches(sourceLine, lexerPath, validTerminals);

        if (matches.Count == 0)
        {
            // No matches found, create unknown token or remove path
            CreateUnknownToken(lexerPath, tokenList);
        }
        else
        {
            // Process matches and create new paths if needed
            ProcessTerminalMatches(lexerPath, matches, tokenList);
        }
    }

    /// <summary>
    /// Gets the current source line for a lexer path.
    /// </summary>
    /// <param name="lexerPath">The lexer path.</param>
    /// <returns>The current source line or null.</returns>
    private IParserLexerSourceLine? GetCurrentSourceLine(LexerPath lexerPath)
    {
        return _sourceLinesContainer.SourceLines
            .FirstOrDefault(line => line.LineNumber == lexerPath.ActiveLineNumber);
    }

    /// <summary>
    /// Finds terminal matches using string operations.
    /// </summary>
    /// <param name="sourceLine">The source line to match against.</param>
    /// <param name="lexerPath">The current lexer path.</param>
    /// <param name="validTerminals">The valid terminals to try.</param>
    /// <returns>Array of terminal matches.</returns>
    private List<TerminalMatch> FindTerminalMatches(
        IParserLexerSourceLine sourceLine,
        LexerPath lexerPath,
        IEnumerable<Terminal> validTerminals)
    {
        var matches = new List<TerminalMatch>();
        var lineContent = sourceLine.Content;
        var startPos = lexerPath.ActiveCharacterNumber;

        foreach (var terminal in validTerminals)
        {
            var match = TryMatchTerminal(terminal, lineContent, startPos);
            if (match != null)
            {
                matches.Add(match);
            }
        }

        return matches;
    }

    /// <summary>
    /// Tries to match a terminal at the current position.
    /// </summary>
    /// <param name="terminal">The terminal to match.</param>
    /// <param name="lineContent">The line content.</param>
    /// <param name="startPos">The starting position.</param>
    /// <returns>A terminal match or null.</returns>
    private TerminalMatch? TryMatchTerminal(Terminal terminal, string lineContent, int startPos)
    {
        if (startPos >= lineContent.Length)
            return null;

        var input = lineContent.Substring(startPos);
        var match = terminal.MatchInput(input);

        if (match != null && match.Success)
        {
            var value = match.Groups.Count > 1 ? match.Groups[1].Value : match.Groups[0].Value;
            return new TerminalMatch(terminal, value, match.Length);
        }

        return null;
    }

    /// <summary>
    /// Creates an unknown token.
    /// </summary>
    /// <param name="lexerPath">The lexer path.</param>
    /// <param name="tokenList">The token list to add the token to.</param>
    private void CreateUnknownToken(LexerPath lexerPath, List<Token> tokenList)
    {
        var sourceLine = GetCurrentSourceLine(lexerPath);
        if (sourceLine == null)
        {
            RemoveLexerPath(lexerPath.LexerPathId, false);
            return;
        }

        var lineContent = sourceLine.Content;
        var charPos = lexerPath.ActiveCharacterNumber;

        if (charPos >= lineContent.Length)
        {
            // End of line, move to next line
            lexerPath.ActiveLineNumber++;
            lexerPath.ActiveCharacterNumber = 0;
            return;
        }

        var value = lineContent.Substring(charPos, 1);

        var unknownToken = new Token(
            lexerPath.LexerPathId,
            new Terminal("UNKNOWN", ""),
            value,
            lexerPath.ActiveLineNumber,
            lexerPath.ActiveCharacterNumber);

        lexerPath.ActiveCharacterNumber++;
        tokenList.Add(unknownToken);
    }

    /// <summary>
    /// Processes terminal matches and creates tokens.
    /// </summary>
    /// <param name="lexerPath">The current lexer path.</param>
    /// <param name="matches">The terminal matches.</param>
    /// <param name="tokenList">The token list to add tokens to.</param>
    private void ProcessTerminalMatches(
        LexerPath lexerPath,
        List<TerminalMatch> matches,
        List<Token> tokenList)
    {
        if (matches.Count == 1)
        {
            // Single match, create token
            CreateTokenFromMatch(lexerPath, matches[0], tokenList);
        }
        else
        {
            // Multiple matches (ambiguity), handle by creating multiple paths
            HandleMultipleMatches(lexerPath, matches, tokenList);
        }
    }

    /// <summary>
    /// Creates a token from a terminal match.
    /// </summary>
    /// <param name="lexerPath">The lexer path.</param>
    /// <param name="match">The terminal match.</param>
    /// <param name="tokenList">The token list to add the token to.</param>
    private void CreateTokenFromMatch(LexerPath lexerPath, TerminalMatch match, List<Token> tokenList)
    {
        var value = _stringInterner.Intern(match.Value);

        var token = new Token(
            lexerPath.LexerPathId,
            match.Terminal,
            value,
            lexerPath.ActiveLineNumber,
            lexerPath.ActiveCharacterNumber);

        lexerPath.ActiveCharacterNumber += match.Length;
        tokenList.Add(token);
    }

    /// <summary>
    /// Handles multiple terminal matches (ambiguity) using object pooling.
    /// </summary>
    /// <param name="lexerPath">The lexer path.</param>
    /// <param name="matches">The terminal matches.</param>
    /// <param name="tokenList">The token list to add tokens to.</param>
    private void HandleMultipleMatches(LexerPath lexerPath, List<TerminalMatch> matches, List<Token> tokenList)
    {
        // Use the first match for the current path
        CreateTokenFromMatch(lexerPath, matches[0], tokenList);

        // Create new paths for the other matches using object pool
        for (int i = 1; i < matches.Count; i++)
        {
            var newPath = _pathPool.Acquire();
            _maximumLexerPathId++;

            // Copy state from current path
            newPath.LexerPathId = _maximumLexerPathId;
            newPath.ActiveLineNumber = lexerPath.ActiveLineNumber;
            newPath.ActiveCharacterNumber = lexerPath.ActiveCharacterNumber;

            // Add to path map
            _lexerPathMap[_maximumLexerPathId] = newPath;

            // Create token for this path
            CreateTokenFromMatch(newPath, matches[i], tokenList);
        }
    }

    /// <summary>
    /// Releases all resources used by the lexer.
    /// </summary>
    public void Dispose()
    {
        _pathPool?.Dispose();
        _arena?.Dispose();
    }
}
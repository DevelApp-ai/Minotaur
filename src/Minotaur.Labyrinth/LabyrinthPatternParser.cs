using DevelApp.StepParser;

namespace Minotaur.Labyrinth;

/// <summary>
/// Parses Labyrinth pattern snippets into mini-ASTs
/// (<see cref="LabyrinthPatternAst"/>) using the real DevelApp.StepLexer /
/// DevelApp.StepParser pipeline with the rule's target grammar, augmented with
/// the standard metavariable (<c>$NAME</c>) and ellipsis (<c>...</c>) pattern
/// tokens (Minotaur issue #101).
/// <para>
/// Pattern snippets are intentionally partial: they only have to be
/// <em>tokenizable</em> against the target grammar, not syntactically valid
/// programs. Syntax errors from the parser phase are therefore tolerated;
/// only lexer-phase failures (no token rule matches the input) are fatal,
/// because they mean the pattern is written in something the target grammar
/// cannot lex at all.
/// </para>
/// </summary>
public sealed class LabyrinthPatternParser : IDisposable
{
    private readonly StepParserEngine _engine;

    /// <summary>
    /// Creates a pattern parser for the given target grammar.
    /// </summary>
    /// <param name="grammarContent">The target grammar file content (StepLexer grammar format).</param>
    /// <param name="grammarName">A display name for the grammar, used in error messages.</param>
    /// <exception cref="LabyrinthRuleException">
    /// Thrown when the grammar itself cannot be loaded.
    /// </exception>
    public LabyrinthPatternParser(string grammarContent, string grammarName = "target grammar")
    {
        _engine = new StepParserEngine();
        try
        {
            _engine.LoadGrammarFromContent(grammarContent, grammarName);

            // The grammar loader tolerates unrecognized content (it silently
            // produces an empty grammar), so verify the grammar is real before
            // the pattern tokens are injected (they would mask the emptiness).
            if (_engine.CurrentGrammar is null || _engine.CurrentGrammar.TokenRules.Count == 0)
            {
                throw new InvalidOperationException("no token rules were found");
            }

            _engine.EnablePatternTokens();
        }
        catch (Exception ex)
        {
            _engine.Dispose();
            throw new LabyrinthRuleException($"Failed to load Labyrinth target grammar '{grammarName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses a single pattern snippet into a mini-AST. The snippet must be
    /// lexable by the target grammar (plus metavariable and ellipsis tokens);
    /// it does not have to satisfy the grammar's production rules.
    /// </summary>
    /// <param name="pattern">The pattern snippet, e.g. <c>ExecuteAction(..., $DATA, ...)</c>.</param>
    /// <returns>The pattern mini-AST. An empty/whitespace pattern yields an AST with no nodes.</returns>
    /// <exception cref="LabyrinthRuleException">
    /// Thrown when the target grammar's lexer cannot tokenize the pattern
    /// (unexpected input or a stalled lexer), i.e. the pattern is not written
    /// in the target grammar's token vocabulary.
    /// </exception>
    public LabyrinthPatternAst ParsePattern(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return new LabyrinthPatternAst(pattern ?? string.Empty, Array.Empty<LabyrinthPatternNode>());
        }

        StepParsingResult result;
        try
        {
            result = _engine.Parse(pattern, "pattern");
        }
        catch (Exception ex)
        {
            throw new LabyrinthRuleException($"Failed to lex Labyrinth pattern '{pattern}': {ex.Message}", ex);
        }

        var lexerError = result.Diagnostics.FirstOrDefault(d =>
            d.Code == DiagnosticCodes.LexerUnexpectedInput || d.Code == DiagnosticCodes.LexerStalled);
        if (lexerError is not null)
        {
            throw new LabyrinthRuleException(
                $"Pattern '{pattern}' cannot be tokenized by the Labyrinth target grammar: {lexerError.Message}");
        }

        var nodes = new List<LabyrinthPatternNode>(result.Tokens.Count);
        foreach (var token in result.Tokens)
        {
            var kind = token.Type switch
            {
                PatternTokens.MetavariableTokenName => LabyrinthPatternNodeKind.Metavariable,
                PatternTokens.EllipsisTokenName => LabyrinthPatternNodeKind.Ellipsis,
                _ => LabyrinthPatternNodeKind.Literal
            };
            nodes.Add(new LabyrinthPatternNode(kind, token.Type, token.Value, token.StartPosition, token.Length));
        }

        return new LabyrinthPatternAst(pattern, nodes);
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _engine.Dispose();
    }
}

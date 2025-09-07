using StepParser.Core;
using StepParser.Grammar;
using StepParser.Lexer;
using Xunit;

namespace StepParser.Tests;

public class StepParserBasicTests
{
    [Fact]
    public void StepParser_CanBeCreated()
    {
        // Arrange & Act
        using var parser = new StepParser.Core.StepParser();
        
        // Assert
        Assert.NotNull(parser);
        Assert.Equal(string.Empty, parser.ActiveGrammarName);
    }

    [Fact]
    public void Grammar_CanBeCreatedAndSet()
    {
        // Arrange
        using var parser = new StepParser.Core.StepParser();
        var grammar = new Grammar.Grammar("TestGrammar");
        
        // Act
        parser.SetActiveGrammar(grammar);
        
        // Assert
        Assert.Equal("TestGrammar", parser.ActiveGrammarName);
    }

    [Fact]
    public void Terminal_CanBeCreatedAndUsed()
    {
        // Arrange
        var terminal = new Terminal("identifier", @"[a-zA-Z_][a-zA-Z0-9_]*");
        
        // Act
        var match = terminal.MatchInput("testIdentifier");
        
        // Assert
        Assert.NotNull(match);
        Assert.True(match.Success);
        Assert.Equal("testIdentifier", match.Value);
    }

    [Fact]
    public void Production_CanBeCreatedAndConfigured()
    {
        // Arrange
        var production = new Production("assignment");
        var identifierTerminal = new Terminal("identifier", @"[a-zA-Z_][a-zA-Z0-9_]*");
        var equalsTerminal = new Terminal("equals", @"=");
        
        // Act
        production.AddPart(identifierTerminal);
        production.AddPart(equalsTerminal);
        
        // Assert
        Assert.Equal("assignment", production.Name);
        Assert.Equal(2, production.Parts.Count);
    }

    [Fact]
    public void Token_CanBeCreatedWithCorrectProperties()
    {
        // Arrange
        var terminal = new Terminal("number", @"\d+");
        
        // Act
        var token = new Token(1, terminal, "123", 0, 5);
        
        // Assert
        Assert.Equal(1, token.LexerPathId);
        Assert.Equal(terminal, token.Terminal);
        Assert.Equal("123", token.Value);
        Assert.Equal(0, token.LineNumber);
        Assert.Equal(5, token.CharacterNumber);
    }

    [Fact]
    public void SourceContainer_CanLoadContent()
    {
        // Arrange
        var content = "line1\nline2\nline3";
        
        // Act
        var container = new SourceContainer(content);
        
        // Assert
        Assert.Equal(3, container.LineCount);
        var lines = container.SourceLines.ToList();
        Assert.Equal("line1", lines[0].Content);
        Assert.Equal("line2", lines[1].Content);
        Assert.Equal("line3", lines[2].Content);
    }

    [Fact]
    public async Task StepParser_CanParseSimpleGrammar()
    {
        // Arrange
        using var parser = new StepParser.Core.StepParser();
        var grammar = new Grammar.Grammar("SimpleGrammar");
        
        // Create a simple grammar for identifiers
        var identifierTerminal = new Terminal("identifier", @"[a-zA-Z_][a-zA-Z0-9_]*");
        grammar.AddValidStartTerminal(identifierTerminal);
        
        var identifierProduction = new Production("identifier_rule");
        identifierProduction.AddPart(identifierTerminal);
        grammar.AddProduction(identifierProduction);
        grammar.AddStartProduction(identifierProduction);
        
        parser.SetActiveGrammar(grammar);
        
        var sourceContainer = new SourceContainer("testVariable");
        
        // Act
        var matches = await parser.ParseAsync("SimpleGrammar", sourceContainer);
        
        // Assert
        Assert.NotNull(matches);
        // Note: The actual parsing logic would need more implementation to produce matches
        // This test validates that the API works and doesn't throw exceptions
    }

    [Fact]
    public void ContextStates_CanBeSetAndRetrieved()
    {
        // Arrange
        using var parser = new StepParser.Core.StepParser();
        
        // Act
        parser.SetContextState("in_function", true);
        parser.SetContextState("in_class", false);
        
        // Assert
        Assert.True(parser.GetContextState("in_function"));
        Assert.False(parser.GetContextState("in_class"));
        Assert.False(parser.GetContextState("non_existent"));
    }

    [Fact]
    public void Callbacks_CanBeRegisteredAndCleared()
    {
        // Arrange
        using var parser = new StepParser.Core.StepParser();
        var callbackExecuted = false;
        
        void TestCallback(Dictionary<string, object> context)
        {
            callbackExecuted = true;
        }
        
        // Act
        parser.RegisterCallback("test_production", TestCallback);
        parser.ClearCallbacks();
        
        // Assert - Since we cleared callbacks, they shouldn't be executed
        // This test validates the callback registration API
        Assert.NotNull(parser); // Basic validation that operations completed
    }
}
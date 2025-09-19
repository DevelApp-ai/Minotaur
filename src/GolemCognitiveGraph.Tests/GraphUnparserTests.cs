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

using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Unparser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GolemCognitiveGraph.Tests;

[TestClass]
public class GraphUnparserTests
{
    [TestMethod]
    public void GraphUnparser_UnparseSimpleExpression_GeneratesCorrectCode()
    {
        // Arrange
        var root = new NonTerminalNode("expression");
        var left = new IdentifierNode("x");
        var op = new TerminalNode("+", "operator");
        op.NodeType = "operator";  // Set node type explicitly
        var right = new LiteralNode("5", "number", 5);

        root.AddChild(left);
        root.AddChild(op);
        root.AddChild(right);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "x");
        StringAssert.Contains(result, "+");
        StringAssert.Contains(result, "5");
    }

    [TestMethod]
    public void GraphUnparser_UnparseWithComments_IncludesComments()
    {
        // Arrange
        var root = new NonTerminalNode("block");
        var comment = new TerminalNode("This is a comment", "comment");
        comment.NodeType = "comment";  // Set node type explicitly
        comment.Metadata["commentType"] = "line";
        var stmt = new IdentifierNode("x");

        root.AddChild(comment);
        root.AddChild(stmt);

        var config = new UnparseConfiguration { IncludeComments = true };
        using var unparser = new GraphUnparser(config);

        // Act
        var result = unparser.Unparse(root);

        // Assert
        StringAssert.Contains(result, "// This is a comment");
        StringAssert.Contains(result, "x");
    }

    [TestMethod]
    public void GraphUnparser_UnparseWithoutComments_ExcludesComments()
    {
        // Arrange
        var root = new NonTerminalNode("block");
        var comment = new TerminalNode("This is a comment", "comment");
        comment.NodeType = "comment";  // Set node type explicitly
        comment.Metadata["commentType"] = "line";
        var stmt = new IdentifierNode("x");

        root.AddChild(comment);
        root.AddChild(stmt);

        var config = new UnparseConfiguration { IncludeComments = false };
        using var unparser = new GraphUnparser(config);

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsFalse(result.Contains("// This is a comment"));
        StringAssert.Contains(result, "x");
    }

    [TestMethod]
    public async Task GraphUnparser_UnparseAsync_WorksCorrectly()
    {
        // Arrange
        var root = new IdentifierNode("test");
        using var unparser = new GraphUnparser();

        // Act
        var result = await unparser.UnparseAsync(root);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "test");
    }

    [TestMethod]
    public void GraphUnparser_UnparseLiteralTypes_FormatsCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("literals");

        var stringLit = new LiteralNode("hello", "string", "hello");
        var numberLit = new LiteralNode("42", "number", 42);
        var boolLit = new LiteralNode("true", "boolean", true);

        root.AddChild(stringLit);
        root.AddChild(numberLit);
        root.AddChild(boolLit);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        StringAssert.Contains(result, "\"hello\"");
        StringAssert.Contains(result, "42");
        StringAssert.Contains(result, "true");
    }

    [TestMethod]
    public void GraphUnparser_UnparseOperators_AddsSpacing()
    {
        // Arrange
        var root = new NonTerminalNode("expression");
        var left = new IdentifierNode("a");
        var op = new TerminalNode("==", "operator");
        op.NodeType = "operator";  // Set node type explicitly
        var right = new IdentifierNode("b");

        root.AddChild(left);
        root.AddChild(op);
        root.AddChild(right);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        StringAssert.Contains(result, "a == b");
    }

    [TestMethod]
    public void GraphUnparser_CustomStrategy_UsesCustomLogic()
    {
        // Arrange
        var root = new NonTerminalNode("custom");
        root.NodeType = "custom";
        root.Metadata["text"] = "custom_content";

        using var unparser = new GraphUnparser();
        unparser.RegisterStrategy("custom", new CustomUnparseStrategy());

        // Act
        var result = unparser.Unparse(root);

        // Assert
        StringAssert.Contains(result, "CUSTOM: custom_content");
    }
}

/// <summary>
/// Custom unparsing strategy for testing.
/// </summary>
public class CustomUnparseStrategy : IUnparseStrategy
{
    public void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var text = node.Metadata.TryGetValue("text", out var value) ? value.ToString() : "";
        context.Write($"CUSTOM: {text}");
    }
}
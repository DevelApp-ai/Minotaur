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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Minotaur.Core;
using Minotaur.Unparser;

namespace Minotaur.Tests.Integration;

[TestClass]
public class ParseUnparseIntegrationTests
{
    [TestMethod]
    public void Integration_SimpleExpression_ParseUnparseRoundTrip()
    {
        // Arrange - Create a simple expression tree
        var root = new NonTerminalNode("expression");
        root.AddChild(new IdentifierNode("x"));
        root.AddChild(new TerminalNode("+", "operator"));
        root.AddChild(new LiteralNode("5", "number", 5));

        using var unparser = new GraphUnparser();

        // Act - Unparse to code
        var code = unparser.Unparse(root);

        // Assert - Code contains expected elements
        Assert.IsNotNull(code);
        StringAssert.Contains(code, "x");
        StringAssert.Contains(code, "+");
        StringAssert.Contains(code, "5");
    }

    [TestMethod]
    public void Integration_NestedExpression_UnparsesCorrectly()
    {
        // Arrange - Create nested expression: (a + b) * c
        var root = new NonTerminalNode("expression");

        var leftParen = new TerminalNode("(", "delimiter");
        var innerExpr = new NonTerminalNode("expression");
        innerExpr.AddChild(new IdentifierNode("a"));
        innerExpr.AddChild(new TerminalNode("+", "operator"));
        innerExpr.AddChild(new IdentifierNode("b"));
        var rightParen = new TerminalNode(")", "delimiter");
        var multiply = new TerminalNode("*", "operator");
        multiply.NodeType = "operator";
        var c = new IdentifierNode("c");

        root.AddChild(leftParen);
        root.AddChild(innerExpr);
        root.AddChild(rightParen);
        root.AddChild(multiply);
        root.AddChild(c);

        using var unparser = new GraphUnparser();

        // Act
        var code = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(code);
        StringAssert.Contains(code, "a");
        StringAssert.Contains(code, "b");
        StringAssert.Contains(code, "c");
    }

    [TestMethod]
    public void Integration_StatementBlock_UnparsesWithStructure()
    {
        // Arrange - Create a block with multiple statements
        var root = new NonTerminalNode("block");

        var stmt1 = new NonTerminalNode("statement");
        stmt1.AddChild(new IdentifierNode("x"));
        stmt1.AddChild(new TerminalNode("=", "operator"));
        stmt1.AddChild(new LiteralNode("10", "number", 10));
        stmt1.AddChild(new TerminalNode(";", "delimiter"));

        var stmt2 = new NonTerminalNode("statement");
        stmt2.AddChild(new IdentifierNode("y"));
        stmt2.AddChild(new TerminalNode("=", "operator"));
        stmt2.AddChild(new LiteralNode("20", "number", 20));
        stmt2.AddChild(new TerminalNode(";", "delimiter"));

        root.AddChild(stmt1);
        root.AddChild(stmt2);

        using var unparser = new GraphUnparser();

        // Act
        var code = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(code);
        StringAssert.Contains(code, "x");
        StringAssert.Contains(code, "10");
        StringAssert.Contains(code, "y");
        StringAssert.Contains(code, "20");
    }

    [TestMethod]
    public void Integration_TreeCloning_PreservesStructure()
    {
        // Arrange
        var original = new NonTerminalNode("root");
        original.AddChild(new IdentifierNode("test"));
        original.AddChild(new LiteralNode("value", "string", "value"));

        // Act - Clone the tree
        var clone = original.Clone() as NonTerminalNode;

        using var unparser = new GraphUnparser();
        var originalCode = unparser.Unparse(original);
        var clonedCode = unparser.Unparse(clone!);

        // Assert
        Assert.IsNotNull(clone);
        Assert.AreNotSame(original, clone);
        Assert.AreEqual(originalCode, clonedCode);
    }

    [TestMethod]
    public async Task Integration_AsyncUnparse_WorksWithComplexTree()
    {
        // Arrange
        var root = new NonTerminalNode("program");
        for (int i = 0; i < 50; i++)
        {
            var stmt = new NonTerminalNode("statement");
            stmt.AddChild(new IdentifierNode($"var{i}"));
            stmt.AddChild(new TerminalNode("=", "operator"));
            stmt.AddChild(new LiteralNode(i.ToString(), "number", i));
            root.AddChild(stmt);
        }

        using var unparser = new GraphUnparser();

        // Act
        var code = await unparser.UnparseAsync(root);

        // Assert
        Assert.IsNotNull(code);
        StringAssert.Contains(code, "var0");
        StringAssert.Contains(code, "var49");
    }

    [TestMethod]
    public void Integration_MetadataPreservation_ThroughRoundTrip()
    {
        // Arrange
        var root = new NonTerminalNode("expression");
        var node = new IdentifierNode("myVar");
        node.Metadata["type"] = "string";
        node.Metadata["nullable"] = true;
        root.AddChild(node);

        // Act - Clone to simulate round trip
        var cloned = root.Clone() as NonTerminalNode;
        var clonedChild = cloned!.Children[0];

        // Assert - Metadata preserved
        Assert.IsTrue(clonedChild.Metadata.ContainsKey("type"));
        Assert.AreEqual("string", clonedChild.Metadata["type"]);
        Assert.IsTrue(clonedChild.Metadata.ContainsKey("nullable"));
    }

    [TestMethod]
    public void Integration_EmptyTree_HandlesGracefully()
    {
        // Arrange
        var root = new NonTerminalNode("empty");

        using var unparser = new GraphUnparser();

        // Act
        var code = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(code);
        Assert.AreEqual(string.Empty, code.Trim());
    }

    [TestMethod]
    public void Integration_MixedNodeTypes_AllTypesSupported()
    {
        // Arrange - Create tree with all node types
        var root = new NonTerminalNode("mixed");
        root.AddChild(new TerminalNode("keyword", "keyword"));
        root.AddChild(new IdentifierNode("identifier"));
        root.AddChild(new LiteralNode("42", "number", 42));
        root.AddChild(new LiteralNode("true", "boolean", true));
        root.AddChild(new LiteralNode("text", "string", "text"));

        using var unparser = new GraphUnparser();

        // Act
        var code = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(code);
        StringAssert.Contains(code, "keyword");
        StringAssert.Contains(code, "identifier");
        StringAssert.Contains(code, "42");
        StringAssert.Contains(code, "true");
        StringAssert.Contains(code, "text");
    }
}

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

namespace Minotaur.Tests.CoreNodes;

[TestClass]
public class NodeTypeTests
{
    [TestMethod]
    public void TerminalNode_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var node = new TerminalNode("test", "identifier");

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual("test", node.Text);
        Assert.AreEqual("identifier", node.TokenType);
        Assert.AreEqual("terminal", node.NodeType);
    }

    [TestMethod]
    public void TerminalNode_Constructor_WithEmptyParameters_InitializesWithDefaults()
    {
        // Arrange & Act
        var node = new TerminalNode();

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual(string.Empty, node.Text);
        Assert.AreEqual(string.Empty, node.TokenType);
        Assert.AreEqual("terminal", node.NodeType);
    }

    [TestMethod]
    public void TerminalNode_Metadata_ContainsTextAndTokenType()
    {
        // Arrange & Act
        var node = new TerminalNode("value", "string");

        // Assert
        Assert.IsTrue(node.Metadata.ContainsKey("text"));
        Assert.IsTrue(node.Metadata.ContainsKey("tokenType"));
        Assert.AreEqual("value", node.Metadata["text"]);
        Assert.AreEqual("string", node.Metadata["tokenType"]);
    }

    [TestMethod]
    public void TerminalNode_Clone_CreatesDeepCopy()
    {
        // Arrange
        var original = new TerminalNode("original", "type");
        original.Metadata["custom"] = "value";

        // Act
        var clone = original.Clone() as TerminalNode;

        // Assert
        Assert.IsNotNull(clone);
        Assert.AreNotSame(original, clone);
        Assert.AreEqual(original.Text, clone.Text);
        Assert.AreEqual(original.TokenType, clone.TokenType);
        Assert.AreEqual(original.Metadata["custom"], clone.Metadata["custom"]);
    }

    [TestMethod]
    public void NonTerminalNode_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var node = new NonTerminalNode("expression");

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual("expression", node.RuleName);
        Assert.AreEqual("nonterminal", node.NodeType);
        Assert.IsNotNull(node.Children);
        Assert.AreEqual(0, node.Children.Count);
    }

    [TestMethod]
    public void NonTerminalNode_AddChild_AddsChildSuccessfully()
    {
        // Arrange
        var parent = new NonTerminalNode("statement");
        var child = new TerminalNode("value", "identifier");

        // Act
        parent.AddChild(child);

        // Assert
        Assert.AreEqual(1, parent.Children.Count);
        Assert.AreSame(child, parent.Children[0]);
    }

    [TestMethod]
    public void NonTerminalNode_AddChild_MultipleChildren_MaintainsOrder()
    {
        // Arrange
        var parent = new NonTerminalNode("expression");
        var child1 = new TerminalNode("a", "identifier");
        var child2 = new TerminalNode("+", "operator");
        var child3 = new TerminalNode("b", "identifier");

        // Act
        parent.AddChild(child1);
        parent.AddChild(child2);
        parent.AddChild(child3);

        // Assert
        Assert.AreEqual(3, parent.Children.Count);
        Assert.AreSame(child1, parent.Children[0]);
        Assert.AreSame(child2, parent.Children[1]);
        Assert.AreSame(child3, parent.Children[2]);
    }

    [TestMethod]
    public void NonTerminalNode_Clone_CreatesDeepCopyWithChildren()
    {
        // Arrange
        var original = new NonTerminalNode("block");
        original.AddChild(new TerminalNode("statement1", "keyword"));
        original.AddChild(new TerminalNode("statement2", "keyword"));

        // Act
        var clone = original.Clone() as NonTerminalNode;

        // Assert
        Assert.IsNotNull(clone);
        Assert.AreNotSame(original, clone);
        Assert.AreEqual(original.RuleName, clone.RuleName);
        Assert.AreEqual(original.Children.Count, clone.Children.Count);
        Assert.AreNotSame(original.Children[0], clone.Children[0]);
    }

    [TestMethod]
    public void IdentifierNode_Constructor_InitializesCorrectly()
    {
        // Arrange & Act
        var node = new IdentifierNode("myVariable");

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual("myVariable", node.Text);
        Assert.AreEqual("identifier", node.NodeType);
        Assert.IsFalse(node.IsQualified);
    }

    [TestMethod]
    public void IdentifierNode_Metadata_ContainsNamespace()
    {
        // Arrange & Act
        var node = new IdentifierNode("testId");

        // Assert
        Assert.IsTrue(node.Metadata.ContainsKey("namespace"));
        Assert.IsTrue(node.Metadata.ContainsKey("isQualified"));
    }

    [TestMethod]
    public void IdentifierNode_Clone_CreatesCorrectCopy()
    {
        // Arrange
        var original = new IdentifierNode("originalName", "MyNamespace");

        // Act
        var clone = original.Clone() as IdentifierNode;

        // Assert
        Assert.IsNotNull(clone);
        Assert.AreNotSame(original, clone);
        Assert.AreEqual(original.Text, clone.Text);
        Assert.AreEqual(original.Namespace, clone.Namespace);
    }

    [TestMethod]
    public void LiteralNode_Constructor_WithStringValue_InitializesCorrectly()
    {
        // Arrange & Act
        var node = new LiteralNode("hello", "string", "hello");

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual("hello", node.Text);
        Assert.AreEqual("string", node.LiteralType);
        Assert.AreEqual("hello", node.Value);
        Assert.AreEqual("literal", node.NodeType);
    }

    [TestMethod]
    public void LiteralNode_Constructor_WithNumericValue_InitializesCorrectly()
    {
        // Arrange & Act
        var node = new LiteralNode("42", "number", 42);

        // Assert
        Assert.IsNotNull(node);
        Assert.AreEqual("42", node.Text);
        Assert.AreEqual("number", node.LiteralType);
        Assert.AreEqual(42, node.Value);
    }

    [TestMethod]
    public void LiteralNode_Metadata_ContainsValueAndType()
    {
        // Arrange & Act
        var node = new LiteralNode("true", "boolean", true);

        // Assert
        Assert.IsTrue(node.Metadata.ContainsKey("value"));
        Assert.IsTrue(node.Metadata.ContainsKey("literalType"));
        Assert.AreEqual(true, node.Metadata["value"]);
        Assert.AreEqual("boolean", node.Metadata["literalType"]);
    }

    [TestMethod]
    public void LiteralNode_Clone_CreatesCorrectCopy()
    {
        // Arrange
        var original = new LiteralNode("123", "number", 123);

        // Act
        var clone = original.Clone() as LiteralNode;

        // Assert
        Assert.IsNotNull(clone);
        Assert.AreNotSame(original, clone);
        Assert.AreEqual(original.Text, clone.Text);
        Assert.AreEqual(original.LiteralType, clone.LiteralType);
        Assert.AreEqual(original.Value, clone.Value);
    }

    [TestMethod]
    public void CognitiveGraphNode_Metadata_CanBeModified()
    {
        // Arrange
        var node = new TerminalNode("test", "identifier");

        // Act
        node.Metadata["customKey"] = "customValue";
        node.Metadata["anotherKey"] = 123;

        // Assert
        Assert.AreEqual("customValue", node.Metadata["customKey"]);
        Assert.AreEqual(123, node.Metadata["anotherKey"]);
    }
}

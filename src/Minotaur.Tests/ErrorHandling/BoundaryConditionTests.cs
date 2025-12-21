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
using Minotaur.GrammarGeneration.Models;

namespace Minotaur.Tests.ErrorHandling;

[TestClass]
public class BoundaryConditionTests
{
    [TestMethod]
    public void BoundaryCondition_EmptyString_HandlesGracefully()
    {
        // Arrange
        var node = new TerminalNode(string.Empty, "empty");

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void BoundaryCondition_NullMetadata_DoesNotThrow()
    {
        // Arrange
        var node = new TerminalNode("test", "value");
        // Metadata is initialized by default, but we test access

        // Act & Assert - Should not throw
        var hasKey = node.Metadata.ContainsKey("nonexistent");
        Assert.IsFalse(hasKey);
    }

    [TestMethod]
    public void BoundaryCondition_VeryLongString_HandlesCorrectly()
    {
        // Arrange
        var longText = new string('a', 10000);
        var node = new TerminalNode(longText, "long");

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Length >= 10000);
    }

    [TestMethod]
    public void BoundaryCondition_SpecialCharacters_PreservesContent()
    {
        // Arrange
        var specialChars = "!@#$%^&*()[]{}|\\<>?/~`";
        var node = new TerminalNode(specialChars, "special");

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, specialChars);
    }

    [TestMethod]
    public void BoundaryCondition_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var unicode = "Hello 世界 🌍 Привет";
        var node = new TerminalNode(unicode, "unicode");

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "世界");
        StringAssert.Contains(result, "🌍");
    }

    [TestMethod]
    public void BoundaryCondition_DeeplyNestedTree_DoesNotStackOverflow()
    {
        // Arrange - Create deeply nested structure (100 levels)
        var root = new NonTerminalNode("root");
        var current = root;
        
        for (int i = 0; i < 100; i++)
        {
            var child = new NonTerminalNode($"level{i}");
            current.AddChild(child);
            current = child;
        }
        
        current.AddChild(new TerminalNode("leaf", "value"));

        using var unparser = new GraphUnparser();

        // Act & Assert - Should not throw StackOverflowException
        var result = unparser.Unparse(root);
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "leaf");
    }

    [TestMethod]
    public void BoundaryCondition_ManyChildren_HandlesCorrectly()
    {
        // Arrange - Node with 1000 children
        var root = new NonTerminalNode("parent");
        
        for (int i = 0; i < 1000; i++)
        {
            root.AddChild(new TerminalNode($"child{i}", "item"));
        }

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "child0");
        StringAssert.Contains(result, "child999");
    }

    [TestMethod]
    public void BoundaryCondition_ZeroValue_HandlesCorrectly()
    {
        // Arrange
        var node = new LiteralNode("0", "number", 0);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "0");
    }

    [TestMethod]
    public void BoundaryCondition_NegativeNumber_PreservesSign()
    {
        // Arrange
        var node = new LiteralNode("-42", "number", -42);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("-42") || (result.Contains("-") && result.Contains("42")));
    }

    [TestMethod]
    public void BoundaryCondition_EmptyGrammar_CreatesValidStructure()
    {
        // Arrange
        var grammar = new Grammar
        {
            Name = "Empty",
            Language = "None"
        };

        // Act & Assert - Should not throw
        Assert.IsNotNull(grammar);
        Assert.AreEqual("Empty", grammar.Name);
        Assert.AreEqual(0, grammar.TokenRules.Patterns.Count);
        Assert.AreEqual(0, grammar.ProductionRules.Rules.Count);
    }

    [TestMethod]
    public void BoundaryCondition_WhitespaceOnlyText_Handles()
    {
        // Arrange
        var whitespace = "   \t\n\r   ";
        var node = new TerminalNode(whitespace, "whitespace");

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
    }

    [TestMethod]
    public void BoundaryCondition_MaxIntValue_HandlesCorrectly()
    {
        // Arrange
        var maxInt = int.MaxValue;
        var node = new LiteralNode(maxInt.ToString(), "number", maxInt);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(node);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, maxInt.ToString());
    }
}

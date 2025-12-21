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

namespace Minotaur.Tests.Unparser;

[TestClass]
public class UnparserAdvancedTests
{
    [TestMethod]
    public void GraphUnparser_UnparseNestedStructures_HandlesDeeplyNested()
    {
        // Arrange
        var root = new NonTerminalNode("program");
        var level1 = new NonTerminalNode("function");
        var level2 = new NonTerminalNode("block");
        var level3 = new NonTerminalNode("statement");
        var terminal = new TerminalNode("value", "identifier");

        level3.AddChild(terminal);
        level2.AddChild(level3);
        level1.AddChild(level2);
        root.AddChild(level1);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "value");
    }

    [TestMethod]
    public void GraphUnparser_UnparseMultipleSiblings_MaintainsOrder()
    {
        // Arrange
        var root = new NonTerminalNode("list");
        root.AddChild(new TerminalNode("first", "item"));
        root.AddChild(new TerminalNode("second", "item"));
        root.AddChild(new TerminalNode("third", "item"));

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(result);
        var firstPos = result.IndexOf("first");
        var secondPos = result.IndexOf("second");
        var thirdPos = result.IndexOf("third");
        
        Assert.IsTrue(firstPos >= 0);
        Assert.IsTrue(secondPos > firstPos);
        Assert.IsTrue(thirdPos > secondPos);
    }

    [TestMethod]
    public void GraphUnparser_UnparseEmptyNonTerminal_ReturnsEmptyString()
    {
        // Arrange
        var root = new NonTerminalNode("empty");
        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual(string.Empty, result.Trim());
    }

    [TestMethod]
    public void GraphUnparser_UnparseIdentifierWithNamespace_IncludesQualifiedName()
    {
        // Arrange
        var root = new NonTerminalNode("expression");
        var qualified = new IdentifierNode("Method", "MyNamespace");
        root.AddChild(qualified);

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(result.Contains("Method"));
    }

    [TestMethod]
    public void GraphUnparser_UnparseMixedLiterals_HandlesAllTypes()
    {
        // Arrange
        var root = new NonTerminalNode("values");
        root.AddChild(new LiteralNode("42", "number", 42));
        root.AddChild(new LiteralNode("true", "boolean", true));
        root.AddChild(new LiteralNode("hello", "string", "hello"));
        root.AddChild(new LiteralNode("3.14", "float", 3.14));

        using var unparser = new GraphUnparser();

        // Act
        var result = unparser.Unparse(root);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "42");
        StringAssert.Contains(result, "true");
        StringAssert.Contains(result, "hello");
        StringAssert.Contains(result, "3.14");
    }

    [TestMethod]
    public async Task GraphUnparser_UnparseAsync_HandlesLargeTree()
    {
        // Arrange
        var root = new NonTerminalNode("large");
        for (int i = 0; i < 100; i++)
        {
            root.AddChild(new TerminalNode($"item{i}", "value"));
        }

        using var unparser = new GraphUnparser();

        // Act
        var result = await unparser.UnparseAsync(root);

        // Assert
        Assert.IsNotNull(result);
        StringAssert.Contains(result, "item0");
        StringAssert.Contains(result, "item99");
    }

    [TestMethod]
    public void GraphUnparser_Dispose_CanBeCalledMultipleTimes()
    {
        // Arrange
        var unparser = new GraphUnparser();
        var root = new TerminalNode("test", "value");

        // Act
        var result = unparser.Unparse(root);
        unparser.Dispose();
        unparser.Dispose(); // Should not throw

        // Assert
        Assert.IsNotNull(result);
    }
}

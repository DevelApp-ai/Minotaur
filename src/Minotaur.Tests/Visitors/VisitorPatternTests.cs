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
using Minotaur.Visitors;

namespace Minotaur.Tests.Visitors;

[TestClass]
public class VisitorPatternTests
{
    [TestMethod]
    public void CognitiveGraphVisitor_VisitSingleNode_CallsBeforeAndAfter()
    {
        // Arrange
        var visitor = new TestVisitor();
        var node = new TerminalNode("test", "value");

        // Act
        visitor.Visit(node);

        // Assert
        Assert.AreEqual(1, visitor.BeforeCount);
        Assert.AreEqual(1, visitor.AfterCount);
        Assert.AreEqual(1, visitor.VisitedNodes.Count);
    }

    [TestMethod]
    public void CognitiveGraphVisitor_VisitTree_TraversesDepthFirst()
    {
        // Arrange
        var visitor = new TestVisitor();
        var root = new NonTerminalNode("root");
        var child1 = new TerminalNode("child1", "value");
        var child2 = new TerminalNode("child2", "value");
        root.AddChild(child1);
        root.AddChild(child2);

        // Act
        visitor.Visit(root);

        // Assert
        Assert.AreEqual(3, visitor.VisitedNodes.Count);
        Assert.AreEqual(3, visitor.BeforeCount);
        Assert.AreEqual(3, visitor.AfterCount);
        // Root should be first in visited order
        Assert.AreSame(root, visitor.VisitedNodes[0]);
    }

    [TestMethod]
    public void CognitiveGraphVisitor_VisitNestedTree_TraversesAllLevels()
    {
        // Arrange
        var visitor = new TestVisitor();
        var root = new NonTerminalNode("root");
        var level1 = new NonTerminalNode("level1");
        var level2 = new TerminalNode("level2", "value");
        level1.AddChild(level2);
        root.AddChild(level1);

        // Act
        visitor.Visit(root);

        // Assert
        Assert.AreEqual(3, visitor.VisitedNodes.Count);
        Assert.IsTrue(visitor.VisitedNodes.Contains(root));
        Assert.IsTrue(visitor.VisitedNodes.Contains(level1));
        Assert.IsTrue(visitor.VisitedNodes.Contains(level2));
    }

    [TestMethod]
    public void CognitiveGraphVisitor_VisitEmptyNonTerminal_VisitsOnlyRoot()
    {
        // Arrange
        var visitor = new TestVisitor();
        var root = new NonTerminalNode("empty");

        // Act
        visitor.Visit(root);

        // Assert
        Assert.AreEqual(1, visitor.VisitedNodes.Count);
        Assert.AreSame(root, visitor.VisitedNodes[0]);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void CognitiveGraphVisitor_VisitNull_ThrowsException()
    {
        // Arrange
        var visitor = new TestVisitor();

        // Act
        visitor.Visit(null!);
    }

    [TestMethod]
    public void CognitiveGraphVisitor_VisitMultipleTimes_AccumulatesCounts()
    {
        // Arrange
        var visitor = new TestVisitor();
        var node1 = new TerminalNode("first", "value");
        var node2 = new TerminalNode("second", "value");

        // Act
        visitor.Visit(node1);
        visitor.Visit(node2);

        // Assert
        Assert.AreEqual(2, visitor.BeforeCount);
        Assert.AreEqual(2, visitor.AfterCount);
        Assert.AreEqual(2, visitor.VisitedNodes.Count);
    }

    [TestMethod]
    public void CognitiveGraphVisitor_VisitLargeTree_HandlesCorrectly()
    {
        // Arrange
        var visitor = new TestVisitor();
        var root = new NonTerminalNode("root");
        
        // Add 20 children
        for (int i = 0; i < 20; i++)
        {
            root.AddChild(new TerminalNode($"child{i}", "value"));
        }

        // Act
        visitor.Visit(root);

        // Assert
        Assert.AreEqual(21, visitor.VisitedNodes.Count); // root + 20 children
        Assert.AreEqual(21, visitor.BeforeCount);
        Assert.AreEqual(21, visitor.AfterCount);
    }

    [TestMethod]
    public void CognitiveGraphVisitor_VisitNodeWithAccept_UsesVisitorPattern()
    {
        // Arrange
        var visitor = new TestVisitor();
        var node = new TerminalNode("test", "value");

        // Act
        node.Accept(visitor);

        // Assert
        Assert.AreEqual(1, visitor.VisitedNodes.Count);
        Assert.AreSame(node, visitor.VisitedNodes[0]);
    }

    [TestMethod]
    public void CognitiveGraphVisitor_VisitChildren_ProcessesAllChildren()
    {
        // Arrange
        var visitor = new TestVisitor();
        var parent = new NonTerminalNode("parent");
        parent.AddChild(new TerminalNode("child1", "value"));
        parent.AddChild(new TerminalNode("child2", "value"));
        parent.AddChild(new TerminalNode("child3", "value"));

        // Act
        visitor.VisitChildren(parent);

        // Assert
        Assert.AreEqual(3, visitor.VisitedNodes.Count);
    }

    [TestMethod]
    public void CognitiveGraphVisitor_DeeplyNestedTree_HandlesRecursion()
    {
        // Arrange
        var visitor = new TestVisitor();
        var root = new NonTerminalNode("root");
        var current = root;
        
        // Create chain of 50 nested nodes
        for (int i = 0; i < 50; i++)
        {
            var child = new NonTerminalNode($"level{i}");
            current.AddChild(child);
            current = child;
        }
        
        current.AddChild(new TerminalNode("leaf", "value"));

        // Act
        visitor.Visit(root);

        // Assert
        Assert.AreEqual(52, visitor.VisitedNodes.Count); // root + 50 levels + leaf
    }
}

/// <summary>
/// Test implementation of the visitor pattern.
/// </summary>
internal class TestVisitor : CognitiveGraphVisitorBase
{
    public List<CognitiveGraphNode> VisitedNodes { get; } = new();
    public int BeforeCount { get; private set; }
    public int AfterCount { get; private set; }

    protected override void BeforeVisitNode(CognitiveGraphNode node)
    {
        BeforeCount++;
        VisitedNodes.Add(node);
    }

    protected override void AfterVisitNode(CognitiveGraphNode node)
    {
        AfterCount++;
    }
}

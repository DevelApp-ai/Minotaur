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

using Minotaur.Core;
using Minotaur.Editor;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Minotaur.Tests;

[TestClass]
public class GraphEditorTests
{
    [TestMethod]
    public void GraphEditor_CreateWithRoot_SetsRootCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("root");

        // Act
        using var editor = new GraphEditor(root);

        // Assert
        Assert.IsNotNull(editor.Root);
        Assert.AreEqual(root.Id, editor.Root.Id);
        Assert.AreEqual("root", ((NonTerminalNode)editor.Root).RuleName);
    }

    [TestMethod]
    public void GraphEditor_InsertNode_AddsNodeCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        using var editor = new GraphEditor(root);
        var child = new TerminalNode("test", "identifier");

        // Act
        var operation = editor.InsertNode(root.Id, child);

        // Assert
        Assert.IsNotNull(operation);
        Assert.AreEqual("InsertNode", operation.OperationType);
        Assert.AreEqual(1, root.Children.Count);
        Assert.AreEqual(child.Id, root.Children[0].Id);
        Assert.AreEqual(root, child.Parent);
    }

    [TestMethod]
    public void GraphEditor_RemoveNode_RemovesNodeCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        var child = new TerminalNode("test", "identifier");
        root.AddChild(child);
        using var editor = new GraphEditor(root);

        // Act
        var operation = editor.RemoveNode(child.Id);

        // Assert
        Assert.IsNotNull(operation);
        Assert.AreEqual("RemoveNode", operation.OperationType);
        Assert.AreEqual(0, root.Children.Count);
        Assert.IsNull(child.Parent);
    }

    [TestMethod]
    public void GraphEditor_UndoRedo_WorksCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        using var editor = new GraphEditor(root);
        var child = new TerminalNode("test", "identifier");

        // Act - Insert and then undo
        editor.InsertNode(root.Id, child);
        Assert.AreEqual(1, root.Children.Count);
        Assert.IsTrue(editor.CanUndo);

        editor.Undo();
        Assert.AreEqual(0, root.Children.Count);
        Assert.IsTrue(editor.CanRedo);

        editor.Redo();
        Assert.AreEqual(1, root.Children.Count);
        Assert.IsFalse(editor.CanRedo);
    }

    [TestMethod]
    public void GraphEditor_FindNode_FindsNodeCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        var child = new TerminalNode("test", "identifier");
        root.AddChild(child);
        using var editor = new GraphEditor(root);

        // Act
        var foundRoot = editor.FindNode(root.Id);
        var foundChild = editor.FindNode(child.Id);
        var notFound = editor.FindNode(Guid.NewGuid());

        // Assert
        Assert.IsNotNull(foundRoot);
        Assert.AreEqual(root.Id, foundRoot.Id);
        Assert.IsNotNull(foundChild);
        Assert.AreEqual(child.Id, foundChild.Id);
        Assert.IsNull(notFound);
    }

    [TestMethod]
    public void GraphEditor_ReplaceNode_ReplacesCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        var child = new TerminalNode("old", "identifier");
        var grandchild = new TerminalNode("grandchild", "literal");
        child.AddChild(grandchild);
        root.AddChild(child);
        using var editor = new GraphEditor(root);

        var replacement = new TerminalNode("new", "identifier");

        // Act
        var operation = editor.ReplaceNode(child.Id, replacement, preserveChildren: true);

        // Assert
        Assert.IsNotNull(operation);
        Assert.AreEqual("ReplaceNode", operation.OperationType);
        Assert.AreEqual(1, root.Children.Count);
        Assert.AreEqual(replacement.Id, root.Children[0].Id);
        Assert.AreEqual("new", ((TerminalNode)root.Children[0]).Text);
        Assert.AreEqual(1, replacement.Children.Count);
        Assert.AreEqual(grandchild.Id, replacement.Children[0].Id);
    }

    [TestMethod]
    public void GraphEditor_MoveNode_MovesCorrectly()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        var parent1 = new NonTerminalNode("parent1");
        var parent2 = new NonTerminalNode("parent2");
        var child = new TerminalNode("moveme", "identifier");

        root.AddChild(parent1);
        root.AddChild(parent2);
        parent1.AddChild(child);

        using var editor = new GraphEditor(root);

        // Act
        var operation = editor.MoveNode(child.Id, parent2.Id);

        // Assert
        Assert.IsNotNull(operation);
        Assert.AreEqual("MoveNode", operation.OperationType);
        Assert.AreEqual(0, parent1.Children.Count);
        Assert.AreEqual(1, parent2.Children.Count);
        Assert.AreEqual(child.Id, parent2.Children[0].Id);
        Assert.AreEqual(parent2, child.Parent);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GraphEditor_RemoveRoot_ThrowsException()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        using var editor = new GraphEditor(root);

        // Act
        editor.RemoveNode(root.Id);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void GraphEditor_UndoWhenEmpty_ThrowsException()
    {
        // Arrange
        var root = new NonTerminalNode("root");
        using var editor = new GraphEditor(root);

        // Act
        editor.Undo();
    }
}
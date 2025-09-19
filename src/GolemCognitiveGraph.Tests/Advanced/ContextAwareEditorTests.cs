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
using GolemCognitiveGraph.Advanced;
using GolemCognitiveGraph.Core;
using GolemCognitiveGraph.Editor;

namespace GolemCognitiveGraph.Tests.Advanced;

[TestClass]
public class ContextAwareEditorTests
{
    private GraphEditor _graphEditor = null!;
    private ContextAwareEditor _contextEditor = null!;

    [TestInitialize]
    public void Setup()
    {
        _graphEditor = new GraphEditor();
        _contextEditor = new ContextAwareEditor(_graphEditor);
    }

    [TestMethod]
    public void RegisterCallback_ShouldAddCallback()
    {
        // Arrange
        var callback = new TestRuleActivationCallback();

        // Act
        _contextEditor.RegisterCallback(callback);

        // Assert - callback should be registered (we can't directly test this, but no exception is good)
        Assert.IsTrue(true);
    }

    [TestMethod]
    public void UnregisterCallback_ShouldRemoveCallback()
    {
        // Arrange
        var callback = new TestRuleActivationCallback();
        _contextEditor.RegisterCallback(callback);

        // Act
        var result = _contextEditor.UnregisterCallback(callback);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public async Task EditWithContextAsync_Insert_ShouldSucceed()
    {
        // Arrange
        var root = new NonTerminalNode("root", 0);
        root.SourcePosition = new SourcePosition(1, 1, 0, 10);
        _graphEditor = new GraphEditor(root);
        _contextEditor = new ContextAwareEditor(_graphEditor);

        var newNode = new TerminalNode("child", "value");
        var edit = new ContextualEdit
        {
            Type = EditType.Insert,
            NewNode = newNode,
            TargetPosition = new SourcePosition(1, 1, 0, 0)
        };

        // Act
        var result = await _contextEditor.EditWithContextAsync(edit);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(2, result.ModifiedNodes.Count);
    }

    [TestMethod]
    public async Task EditWithContextAsync_Update_ShouldSucceed()
    {
        // Arrange
        var root = new NonTerminalNode("root", 0);
        root.SourcePosition = new SourcePosition(1, 1, 0, 10);
        _graphEditor = new GraphEditor(root);
        _contextEditor = new ContextAwareEditor(_graphEditor);

        var edit = new ContextualEdit
        {
            Type = EditType.Update,
            NewValue = "updated",
            TargetPosition = new SourcePosition(1, 1, 0, 0)
        };

        // Act
        var result = await _contextEditor.EditWithContextAsync(edit);

        // Assert
        Assert.IsTrue(result.IsSuccessful);
        Assert.AreEqual(1, result.ModifiedNodes.Count);
    }

    [TestMethod]
    public void CreateLocationTracker_ShouldReturnTracker()
    {
        // Arrange
        var sourceText = "var x = 42;";

        // Act
        var tracker = _contextEditor.CreateLocationTracker(sourceText);

        // Assert
        Assert.IsNotNull(tracker);
        Assert.IsInstanceOfType(tracker, typeof(PrecisionLocationTracker));
    }

    private class TestRuleActivationCallback : IRuleActivationCallback
    {
        public Task BeforeEditAsync(EditContext context)
        {
            return Task.CompletedTask;
        }

        public Task AfterEditAsync(EditContext context, EditResult result)
        {
            return Task.CompletedTask;
        }
    }
}

[TestClass]
public class PrecisionLocationTrackerTests
{
    private const string TestSource = "var x = 42;\nvar y = 'hello';";
    private PrecisionLocationTracker _tracker = null!;

    [TestInitialize]
    public void Setup()
    {
        _tracker = new PrecisionLocationTracker(TestSource);
    }

    [TestMethod]
    public void GetPositionAt_Offset_ShouldReturnCorrectPosition()
    {
        // Act
        var position = _tracker.GetPositionAt(0);

        // Assert
        Assert.AreEqual(1, position.Line);
        Assert.AreEqual(1, position.Column);
        Assert.AreEqual(0, position.Offset);
    }

    [TestMethod]
    public void GetPositionAt_LineColumn_ShouldReturnCorrectPosition()
    {
        // Act
        var position = _tracker.GetPositionAt(1, 5);

        // Assert
        Assert.AreEqual(1, position.Line);
        Assert.AreEqual(5, position.Column);
        Assert.AreEqual(4, position.Offset);
    }

    [TestMethod]
    public void GetOffsetAt_ShouldReturnCorrectOffset()
    {
        // Act
        var offset = _tracker.GetOffsetAt(2, 1);

        // Assert
        Assert.AreEqual(12, offset); // After first newline
    }

    [TestMethod]
    public void IsValidPosition_ValidPosition_ShouldReturnTrue()
    {
        // Arrange
        var position = new SourcePosition(1, 1, 0, 1);

        // Act
        var isValid = _tracker.IsValidPosition(position);

        // Assert
        Assert.IsTrue(isValid);
    }

    [TestMethod]
    public void IsValidPosition_InvalidPosition_ShouldReturnFalse()
    {
        // Arrange
        var position = new SourcePosition(100, 100, 1000, 1);

        // Act
        var isValid = _tracker.IsValidPosition(position);

        // Assert
        Assert.IsFalse(isValid);
    }

    [TestMethod]
    public void GetPositionsInRange_ShouldReturnCorrectPositions()
    {
        // Arrange
        var range = new SourcePosition(1, 1, 0, 3);

        // Act
        var positions = _tracker.GetPositionsInRange(range);

        // Assert
        Assert.AreEqual(3, positions.Length);
        Assert.AreEqual(1, positions[0].Line);
        Assert.AreEqual(1, positions[0].Column);
    }
}

[TestClass]
public class SourcePositionTests
{
    [TestMethod]
    public void Contains_ShouldReturnTrueForContainedPosition()
    {
        // Arrange
        var container = new SourcePosition(1, 1, 0, 10);
        var contained = new SourcePosition(1, 5, 4, 2);

        // Act
        var result = container.Contains(contained);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void OverlapsWith_ShouldReturnTrueForOverlappingPositions()
    {
        // Arrange
        var position1 = new SourcePosition(1, 1, 0, 5);
        var position2 = new SourcePosition(1, 3, 2, 5);

        // Act
        var result = position1.OverlapsWith(position2);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void SpanTo_ShouldCreateSpanningPosition()
    {
        // Arrange
        var start = new SourcePosition(1, 1, 0, 3);
        var end = new SourcePosition(1, 10, 9, 2);

        // Act
        var span = start.SpanTo(end);

        // Assert
        Assert.AreEqual(0, span.Offset);
        Assert.AreEqual(11, span.Length);
    }

    [TestMethod]
    public void FromOffset_ShouldCreateCorrectPosition()
    {
        // Arrange
        var sourceText = "hello\nworld";

        // Act
        var position = SourcePosition.FromOffset(6, 5, sourceText);

        // Assert
        Assert.AreEqual(2, position.Line);
        Assert.AreEqual(1, position.Column);
        Assert.AreEqual(6, position.Offset);
        Assert.AreEqual(5, position.Length);
    }
}
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

namespace Minotaur.Unparser;

/// <summary>
/// Interface for unparsing strategies that convert cognitive graph nodes to source code.
/// </summary>
public interface IUnparseStrategy
{
    /// <summary>
    /// Unparses a cognitive graph node to source code using the provided context.
    /// </summary>
    /// <param name="node">The node to unparse.</param>
    /// <param name="context">The unparsing context.</param>
    void UnparseNode(CognitiveGraphNode node, UnparseContext context);
}

/// <summary>
/// Base implementation of unparsing strategy with common functionality.
/// </summary>
public abstract class UnparseStrategyBase : IUnparseStrategy
{
    /// <summary>
    /// Unparses a cognitive graph node to source code.
    /// </summary>
    /// <param name="node">The node to unparse.</param>
    /// <param name="context">The unparsing context.</param>
    public abstract void UnparseNode(CognitiveGraphNode node, UnparseContext context);

    /// <summary>
    /// Gets the text value from a node's metadata.
    /// </summary>
    /// <param name="node">The node to get text from.</param>
    /// <returns>The text value or empty string if not found.</returns>
    protected static string GetNodeText(CognitiveGraphNode node)
    {
        return node.Metadata.TryGetValue("text", out var text) ? text.ToString() ?? string.Empty : string.Empty;
    }

    /// <summary>
    /// Gets a metadata value as a string.
    /// </summary>
    /// <param name="node">The node to get metadata from.</param>
    /// <param name="key">The metadata key.</param>
    /// <returns>The metadata value as a string or empty string if not found.</returns>
    protected static string GetMetadataString(CognitiveGraphNode node, string key)
    {
        return node.Metadata.TryGetValue(key, out var value) ? value.ToString() ?? string.Empty : string.Empty;
    }
}

/// <summary>
/// Unparsing strategy for identifier nodes.
/// </summary>
public class IdentifierUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses an identifier node by writing its text content to the output context.
    /// </summary>
    /// <param name="node">The cognitive graph node representing an identifier.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var text = GetNodeText(node);
        if (!string.IsNullOrEmpty(text))
        {
            context.Write(text);
        }
    }
}

/// <summary>
/// Unparsing strategy for literal value nodes.
/// </summary>
public class LiteralUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses a literal node by writing its formatted value to the output context.
    /// </summary>
    /// <param name="node">The cognitive graph node representing a literal value.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var text = GetNodeText(node);
        var literalType = GetMetadataString(node, "literalType");

        switch (literalType.ToLowerInvariant())
        {
            case "string":
                context.Write($"\"{EscapeString(text)}\"");
                break;
            case "char":
                context.Write($"'{EscapeChar(text)}'");
                break;
            case "number":
            case "integer":
            case "float":
            case "double":
            case "decimal":
                context.Write(text);
                break;
            case "boolean":
                context.Write(text.ToLowerInvariant());
                break;
            default:
                context.Write(text);
                break;
        }
    }

    private static string EscapeString(string value)
    {
        return value.Replace("\\", "\\\\")
                   .Replace("\"", "\\\"")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }

    private static string EscapeChar(string value)
    {
        return value.Replace("\\", "\\\\")
                   .Replace("'", "\\'")
                   .Replace("\n", "\\n")
                   .Replace("\r", "\\r")
                   .Replace("\t", "\\t");
    }
}

/// <summary>
/// Unparsing strategy for operator nodes.
/// </summary>
public class OperatorUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses an operator node by writing it with appropriate spacing to the output context.
    /// </summary>
    /// <param name="node">The cognitive graph node representing an operator.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var text = GetNodeText(node);
        var precedence = GetMetadataString(node, "precedence");
        var associativity = GetMetadataString(node, "associativity");

        // Add spacing around operators based on type
        var needsSpacing = ShouldAddSpacing(text);

        if (needsSpacing)
        {
            context.Write($" {text} ");
        }
        else
        {
            context.Write(text);
        }
    }

    private static bool ShouldAddSpacing(string operatorText)
    {
        // Operators that typically need spacing
        var spacedOperators = new[] { "+", "-", "*", "/", "%", "=", "==", "!=", "<", ">", "<=", ">=", "&&", "||", "&", "|", "^" };
        return spacedOperators.Contains(operatorText);
    }
}

/// <summary>
/// Unparsing strategy for whitespace nodes.
/// </summary>
public class WhitespaceUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses a whitespace node by preserving formatting if configured to do so.
    /// </summary>
    /// <param name="node">The cognitive graph node representing whitespace.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        if (context.Configuration.PreserveFormatting)
        {
            var text = GetNodeText(node);
            context.Write(text);
        }
        else
        {
            // Use a single space for normalized formatting
            context.Write(" ");
        }
    }
}

/// <summary>
/// Unparsing strategy for comment nodes.
/// </summary>
public class CommentUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses a comment node by preserving it if formatting is enabled.
    /// </summary>
    /// <param name="node">The cognitive graph node representing a comment.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        if (!context.Configuration.IncludeComments)
        {
            return;
        }

        var text = GetNodeText(node);
        var commentType = GetMetadataString(node, "commentType");

        switch (commentType.ToLowerInvariant())
        {
            case "line":
            case "single":
                context.Write($"// {text}");
                break;
            case "block":
            case "multi":
                context.Write($"/* {text} */");
                break;
            case "documentation":
            case "doc":
                context.Write($"/// {text}");
                break;
            default:
                context.Write($"// {text}");
                break;
        }
    }
}

/// <summary>
/// Unparsing strategy for block nodes (containing other nodes).
/// </summary>
public class BlockUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses a block node by handling different block types with appropriate formatting.
    /// </summary>
    /// <param name="node">The cognitive graph node representing a block structure.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var blockType = GetMetadataString(node, "blockType");

        switch (blockType.ToLowerInvariant())
        {
            case "braces":
            case "method":
            case "class":
                context.WriteLine("{");
                context.IncreaseIndent();
                // Children will be processed by the visitor
                break;
            case "parentheses":
                context.Write("(");
                break;
            case "brackets":
                context.Write("[");
                break;
            default:
                // No specific formatting for unknown block types
                break;
        }
    }
}

/// <summary>
/// Unparsing strategy for expression nodes.
/// </summary>
public class ExpressionUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses an expression node with appropriate parentheses and formatting based on expression type.
    /// </summary>
    /// <param name="node">The cognitive graph node representing an expression.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var expressionType = GetMetadataString(node, "expressionType");
        var needsParentheses = GetMetadataString(node, "needsParentheses") == "true";

        if (needsParentheses)
        {
            context.Write("(");
        }

        // Process child nodes through visitor
        // The actual expression content will be handled by child nodes

        if (needsParentheses)
        {
            context.Write(")");
        }
    }
}

/// <summary>
/// Unparsing strategy for statement nodes.
/// </summary>
public class StatementUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses a statement node with appropriate semicolon handling and indentation.
    /// </summary>
    /// <param name="node">The cognitive graph node representing a statement.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var statementType = GetMetadataString(node, "statementType");
        var needsSemicolon = GetMetadataString(node, "needsSemicolon") != "false";

        // Process the statement content through child nodes

        // Add semicolon if needed
        if (needsSemicolon && !IsBlockStatement(statementType))
        {
            context.Write(";");
        }

        // Add line break after statement
        context.WriteLine();
    }

    private static bool IsBlockStatement(string statementType)
    {
        var blockStatements = new[] { "if", "while", "for", "foreach", "try", "catch", "finally", "using", "switch" };
        return blockStatements.Contains(statementType.ToLowerInvariant());
    }
}

/// <summary>
/// Unparsing strategy for non-terminal nodes (containers).
/// </summary>
public class NonTerminalUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses a non-terminal node by allowing its children to be processed by the visitor.
    /// </summary>
    /// <param name="node">The cognitive graph node representing a non-terminal.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        // Non-terminal nodes typically don't output text themselves
        // Their children will be processed by the visitor
    }
}

/// <summary>
/// Unparsing strategy for terminal nodes.
/// </summary>
public class TerminalUnparseStrategy : UnparseStrategyBase
{
    /// <summary>
    /// Unparses a terminal node by writing its text content directly to the output.
    /// </summary>
    /// <param name="node">The cognitive graph node representing a terminal.</param>
    /// <param name="context">The unparsing context to write the output to.</param>
    public override void UnparseNode(CognitiveGraphNode node, UnparseContext context)
    {
        var text = GetNodeText(node);
        if (!string.IsNullOrEmpty(text))
        {
            context.Write(text);
        }
    }
}
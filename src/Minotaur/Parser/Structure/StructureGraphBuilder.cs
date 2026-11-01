/*
 * This file is part of Minotaur.
 *
 * Minotaur is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
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
using Minotaur.Parser.Structure;

namespace Minotaur.Parser.Structure;

/// <summary>
/// Builds a balanced, parent-child structure graph from a token stream.
/// Groups nested <c>{ }</c>, <c>( )</c> and <c>[ ]</c> into non-terminal
/// nodes so consumers can group tokens into invocations and argument lists.
/// Unbalanced input is an explicit error - never silent truncation
/// (Minotaur issue #136).
/// </summary>
public static class StructureGraphBuilder
{
    /// <summary>
    /// Builds the structure graph. Punctuation group boundaries are always
    /// retained; individual punctuation/comma/semicolon tokens are included as
    /// terminal leaves so no syntax structure is lost.
    /// </summary>
    /// <param name="tokens">The full token stream in source order.</param>
    /// <param name="sourceLength">The source length, used for completeness checks.</param>
    /// <param name="includePunctuation">Whether to attach punctuation leaves to the graph.</param>
    /// <param name="includeWhitespace">Whether to attach whitespace/comment leaves to the graph.</param>
    public static NonTerminalNode Build(
        IReadOnlyList<StructureToken> tokens,
        int sourceLength,
        bool includePunctuation = true,
        bool includeWhitespace = false)
    {
        var root = new NonTerminalNode("compilation_unit", 0);
        root.Metadata["graphVersion"] = StructureGraphVersions.Current;
        root.Metadata["sourceLength"] = sourceLength;

        var stack = new Stack<NonTerminalNode>();
        stack.Push(root);
        var closers = new Dictionary<char, char>
        {
            ['{'] = '}',
            ['('] = ')',
            ['['] = ']',
        };
        var groupNames = new Dictionary<char, string>
        {
            ['{'] = "block",
            ['('] = "paren_group",
            ['['] = "bracket_group",
        };

        foreach (var token in tokens)
        {
            switch (token.Kind)
            {
                case StructureTokenKind.Punctuation when token.Text.Length == 1 && closers.ContainsKey(token.Text[0]):
                    {
                        var group = new NonTerminalNode(groupNames[token.Text[0]], 0);
                        group.Metadata["openPosition"] = token.StartPosition;
                        group.SourcePosition = new SourcePosition(
                            Line: token.Line,
                            Column: token.Column,
                            Offset: token.StartPosition,
                            Length: token.Length);
                        stack.Peek().AddChild(group);
                        stack.Push(group);
                        break;
                    }

                case StructureTokenKind.Punctuation when token.Text.Length == 1 &&
                                                          (token.Text[0] == '}' || token.Text[0] == ')' || token.Text[0] == ']'):
                    {
                        if (stack.Count <= 1)
                            throw new StructureParseException(
                                $"Unbalanced '{token.Text}': no matching opening delimiter", token.StartPosition, token.Line, token.Column);
                        var top = stack.Pop();
                        if (top.RuleName is "block" && token.Text[0] != '}' ||
                            top.RuleName is "paren_group" && token.Text[0] != ')' ||
                            top.RuleName is "bracket_group" && token.Text[0] != ']')
                            throw new StructureParseException(
                                $"Mismatched delimiter '{token.Text}' for '{top.RuleName}'", token.StartPosition, token.Line, token.Column);
                        top.Metadata["closePosition"] = token.StartPosition;
                        break;
                    }

                case StructureTokenKind.Whitespace:
                case StructureTokenKind.LineComment:
                case StructureTokenKind.BlockComment:
                    {
                        if (includeWhitespace)
                            AddLeaf(stack.Peek(), token);
                        break;
                    }

                case StructureTokenKind.Punctuation:
                    {
                        if (includePunctuation)
                            AddLeaf(stack.Peek(), token);
                        break;
                    }

                default:
                    AddLeaf(stack.Peek(), token);
                    break;
            }
        }

        if (stack.Count != 1)
        {
            var unclosed = stack.Peek();
            throw new StructureParseException(
                $"Unclosed '{unclosed.RuleName}' group (opened at offset {unclosed.Metadata["openPosition"]})",
                (int)(unclosed.Metadata["openPosition"] ?? 0), 0, 0);
        }

        return root;
    }

    private static void AddLeaf(NonTerminalNode parent, StructureToken token)
    {
        // Emit typed concrete nodes so downstream consumers can query
        // identifiers and literals programmatically (Minotaur issue #136)
        // without losing the raw token stream (all leaves are TerminalNodes
        // carrying exact source positions).
        TerminalNode leaf = token.Kind switch
        {
            StructureTokenKind.Identifier => new IdentifierNode(token.Text),
            StructureTokenKind.StringLiteral => new LiteralNode(token.Text, "string", token.Text),
            StructureTokenKind.VerbatimStringLiteral => new LiteralNode(token.Text, "verbatim_string", token.Text),
            StructureTokenKind.RawStringLiteral => new LiteralNode(token.Text, "raw_string", token.Text),
            StructureTokenKind.CharLiteral => new LiteralNode(token.Text, "char", token.Text),
            StructureTokenKind.Number => new LiteralNode(token.Text, "number", token.Text),
            _ => new TerminalNode(token.Text, token.Kind.ToString().ToLowerInvariant()),
        };
        leaf.SourcePosition = new SourcePosition(
            Line: token.Line,
            Column: token.Column,
            Offset: token.StartPosition,
            Length: token.Length);
        parent.AddChild(leaf);
    }
}

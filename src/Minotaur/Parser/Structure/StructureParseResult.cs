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

namespace Minotaur.Parser.Structure;

/// <summary>
/// Result of a structure-preserving parse: the full token stream (including
/// punctuation) plus a balanced structure graph with parent-child edges
/// (Minotaur issue #136, requirement 1).
/// </summary>
public sealed class StructureParseResult
{
    /// <summary>The root of the structure graph (a <c>compilation_unit</c> non-terminal).</summary>
    public NonTerminalNode Root { get; }

    /// <summary>The complete token stream in source order, never truncated.</summary>
    public IReadOnlyList<StructureToken> Tokens { get; }

    /// <summary>The length of the parsed source text.</summary>
    public int SourceLength { get; }

    /// <summary>The structure graph format version. Identical input always
    /// produces graphs that serialize identically for a given version
    /// (Minotaur issue #136, requirement 4).</summary>
    public string GraphVersion => StructureGraphVersions.Current;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructureParseResult"/> class.
    /// </summary>
    public StructureParseResult(NonTerminalNode root, IReadOnlyList<StructureToken> tokens, int sourceLength)
    {
        Root = root;
        Tokens = tokens;
        SourceLength = sourceLength;
    }
}

/// <summary>
/// Versioning of the structure graph format emitted by
/// <see cref="StructurePreservingParser"/>. Downstream tools pin behavior by
/// package version and verify this value (Minotaur issue #136, requirement 5).
/// </summary>
public static class StructureGraphVersions
{
    /// <summary>The current structure graph format version.</summary>
    public const string Current = "1.0.0";
}

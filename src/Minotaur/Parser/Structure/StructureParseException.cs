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

namespace Minotaur.Parser.Structure;

/// <summary>
/// Thrown when source text cannot be tokenized or grouped into a structure
/// graph. Truncation and partial output are reported as explicit errors -
/// never as silently short results (Minotaur issue #136, requirement 2).
/// </summary>
public class StructureParseException : Exception
{
    /// <summary>The source offset at which the problem was detected.</summary>
    public int Position { get; }

    /// <summary>The one-based line where the problem was detected.</summary>
    public int Line { get; }

    /// <summary>The one-based column where the problem was detected.</summary>
    public int Column { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="StructureParseException"/> class.
    /// </summary>
    public StructureParseException(string message, int position, int line, int column)
        : base($"{message} (line {line}, column {column}, offset {position})")
    {
        Position = position;
        Line = line;
        Column = column;
    }
}

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

namespace Minotaur.Parser;

/// <summary>
/// Specifies the version of the cognitive graph to use for parsing.
/// </summary>
public enum CognitiveGraphVersion
{
    /// <summary>
    /// Automatically select the appropriate version based on project size.
    /// Small projects use V1, large projects use V2.
    /// </summary>
    Auto = 0,

    /// <summary>
    /// Version 1 cognitive graph - optimized for small to medium projects.
    /// Uses CognitiveGraph 1.0.x features.
    /// </summary>
    V1 = 1,

    /// <summary>
    /// Version 2 cognitive graph - optimized for large-scale project analysis.
    /// Uses CognitiveGraph 1.1.0+ features with enhanced performance and scalability.
    /// </summary>
    V2 = 2
}

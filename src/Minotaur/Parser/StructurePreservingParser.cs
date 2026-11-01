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

namespace Minotaur.Parser;

/// <summary>
/// Structure-preserving parser for C# source: returns the complete token
/// stream (including punctuation) and a balanced structure graph with
/// parent-child edges. Deterministic, never truncating - failures are
/// explicit, reportable errors (Minotaur issue #136).
/// </summary>
public sealed class StructurePreservingParser
{
    private readonly bool _includePunctuation;
    private readonly bool _includeWhitespace;

    /// <summary>
    /// Initializes a new instance of the <see cref="StructurePreservingParser"/> class.
    /// </summary>
    /// <param name="includePunctuation">Whether punctuation leaves are attached to the graph.</param>
    /// <param name="includeWhitespace">Whether whitespace/comment leaves are attached to the graph.</param>
    public StructurePreservingParser(bool includePunctuation = true, bool includeWhitespace = false)
    {
        _includePunctuation = includePunctuation;
        _includeWhitespace = includeWhitespace;
    }

    /// <summary>
    /// Parses source text into a structure-preserving result. Throws
    /// <see cref="StructureParseException"/> for malformed input; never
    /// returns silently truncated output.
    /// </summary>
    public StructureParseResult Parse(string sourceCode)
    {
        ArgumentNullException.ThrowIfNull(sourceCode);

        var tokens = CSharpStructureTokenizer.Tokenize(sourceCode);
        var root = StructureGraphBuilder.Build(tokens, sourceCode.Length, _includePunctuation, _includeWhitespace);

        return new StructureParseResult(root, tokens, sourceCode.Length);
    }

    /// <summary>
    /// Convenience conversion to the Minotaur cognitive-graph node model with
    /// provenance metadata, for use by <see cref="StepParserIntegration"/>.
    /// </summary>
    public CognitiveGraphNode ToCognitiveGraph(StructureParseResult result, string sourceCode, string language)
    {
        var root = result.Root;
        root.Metadata["sourceCode"] = sourceCode;
        root.Metadata["language"] = language;
        root.Metadata["parserType"] = "Minotaur.StructurePreservingParser";
        root.Metadata["graphVersion"] = result.GraphVersion;
        root.Metadata["tokenCount"] = result.Tokens.Count(t => t.Kind != StructureTokenKind.Whitespace);
        root.Metadata["parseSuccess"] = true;
        root.Metadata["structurePreserving"] = true;
        return root;
    }
}

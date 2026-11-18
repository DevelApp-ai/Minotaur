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

using System.Security.Cryptography;
using System.Text;

namespace Minotaur.Benchmarks.ParserRuntime;

/// <summary>The three case-study DSLs benchmarked by issue #127.</summary>
public enum CaseDsl
{
    Config,
    Query,
    Template,
}

/// <summary>Corpus size classes; approximately 1K / 10K / 100K lines.</summary>
public enum CorpusSize
{
    Small,
    Medium,
    Large,
}

/// <summary>
/// Deterministic corpus generator for the issue #127 parser-runtime
/// benchmark. Every corpus is a collection of bounded documents whose
/// content varies but whose shape is fixed, matching the engine's
/// bounded-document operating mode (see <see cref="CaseStudyDsls"/>).
/// The same seed always produces byte-identical corpora on every platform.
/// </summary>
public static class Corpus
{
    /// <summary>Master seed; recorded in the committed results.</summary>
    public const int Seed = 127;

    /// <summary>Lines of a single configuration document.</summary>
    public const int ConfigDocLines = 7;

    /// <summary>Target line count per corpus size.</summary>
    public static int TargetLines(CorpusSize size) => size switch
    {
        CorpusSize.Small => 1_000,
        CorpusSize.Medium => 10_000,
        CorpusSize.Large => 100_000,
        _ => throw new ArgumentOutOfRangeException(nameof(size)),
    };

    /// <summary>Number of documents in the corpus for the given DSL and size.</summary>
    public static int DocumentCount(CaseDsl dsl, CorpusSize size) => dsl switch
    {
        CaseDsl.Config => TargetLines(size) / ConfigDocLines,
        CaseDsl.Query => TargetLines(size),
        CaseDsl.Template => TargetLines(size),
        _ => throw new ArgumentOutOfRangeException(nameof(dsl)),
    };

    /// <summary>Generate one document of the corpus (index-based, no RNG state).</summary>
    public static string Document(CaseDsl dsl, int index)
    {
        switch (dsl)
        {
            case CaseDsl.Config:
            {
                var sb = new StringBuilder(160);
                sb.Append("[server_").Append(index).AppendLine("]");
                sb.Append("port_").Append(index).Append(" = ").Append(8000 + index % 4096).AppendLine();
                sb.Append("host_").Append(index).Append(" = worker_").Append(index % 97).AppendLine();
                sb.Append("note_").Append(index).Append(" = \"edge node ").Append(index % 512).Append(" of 10.5\"").AppendLine();
                sb.Append("tags_").Append(index).Append(" = [").Append(index % 8192).Append(", ")
                  .Append((index % 997) + 0.5).Append(", \"tag_").Append(index % 256).Append("\", enabled_")
                  .Append(index % 128).Append(']').AppendLine();
                sb.Append("weight_").Append(index).Append(" = ").Append(index % 9).AppendLine();
                sb.Append("tier_").Append(index).Append(" = tier_").Append(index % 5).AppendLine();
                return sb.ToString();
            }

            case CaseDsl.Query:
                return "select metric_" + index % 50 + " from sales_data_" + index % 20 +
                    " where region_" + index % 30 + " = \"region " + index % 100 +
                    "\" window " + (index % 48 + 1) + " unit_" + index % 6 + "\n";

            case CaseDsl.Template:
                return "Report_" + index + " {{ title_" + index % 40 + " }} done" +
                    "<style>body{color:#" + (index % 999) + "}</style>" +
                    " tail_" + index % 61 +
                    "<script>var x_" + index % 77 + " = " + index % 13 + ";</script>" +
                    "{{sql}}SELECT " + index % 9999 + " FROM t{{/sql}}" +
                    " bye_" + index % 31 + "\n";

            default:
                throw new ArgumentOutOfRangeException(nameof(dsl));
        }
    }

    /// <summary>Render the full corpus text (documents concatenated).</summary>
    public static string Render(CaseDsl dsl, CorpusSize size)
    {
        var docs = DocumentCount(dsl, size);
        var sb = new StringBuilder(docs * 96);
        for (var i = 0; i < docs; i++)
        {
            sb.Append(Document(dsl, i));
        }

        return sb.ToString();
    }

    /// <summary>Count the lines of the rendered corpus.</summary>
    public static int CountLines(string corpus) => corpus.Count(c => c == '\n');

    /// <summary>SHA-256 of the corpus text (lowercase hex).</summary>
    public static string Sha256(string corpus)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(corpus));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

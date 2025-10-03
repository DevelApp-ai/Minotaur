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

using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Minotaur.Projects.Grammar.Detectors;

/// <summary>
/// Grammar detector that analyzes file content to determine the appropriate grammar.
/// This detector can identify language-specific patterns, directives, and syntax.
/// </summary>
public partial class ContentBasedGrammarDetector : IGrammarDetector
{
    private static readonly List<ContentDetectionRule> DefaultContentRules = new()
    {
        // C# specific patterns
        new ContentDetectionRule
        {
            Name = "csharp-namespace",
            Pattern = @"^\s*namespace\s+[\w\.]+\s*[{;]",
            Mapping = new GrammarMapping { Grammar = "CSharp10.grammar", Version = "10.0", Confidence = 0.95 },
            Priority = 10
        },
        new ContentDetectionRule
        {
            Name = "csharp-using",
            Pattern = @"^\s*using\s+(?:System|Microsoft|[\w\.]+)\s*;",
            Mapping = new GrammarMapping { Grammar = "CSharp10.grammar", Version = "10.0", Confidence = 0.85 },
            Priority = 8
        },
        new ContentDetectionRule
        {
            Name = "csharp-class",
            Pattern = @"^\s*(?:public|private|internal|protected)?\s*(?:static|abstract|sealed)?\s*class\s+\w+\s*(?:[:{]|$)",
            Mapping = new GrammarMapping { Grammar = "CSharp10.grammar", Version = "10.0", Confidence = 0.9 },
            Priority = 11
        },

        // JavaScript/TypeScript patterns
        new ContentDetectionRule
        {
            Name = "typescript-interface",
            Pattern = @"^\s*(?:export\s+)?interface\s+\w+",
            Mapping = new GrammarMapping { Grammar = "TypeScript.grammar", Version = "4.9", Confidence = 0.95 },
            Priority = 10
        },
        new ContentDetectionRule
        {
            Name = "typescript-type",
            Pattern = @"^\s*(?:export\s+)?type\s+\w+\s*=",
            Mapping = new GrammarMapping { Grammar = "TypeScript.grammar", Version = "4.9", Confidence = 0.9 },
            Priority = 9
        },
        new ContentDetectionRule
        {
            Name = "es6-import",
            Pattern = @"^\s*import\s+.*\s+from\s+['""].*['""]",
            Mapping = new GrammarMapping { Grammar = "JavaScriptES2022.grammar", Version = "ES2022", Confidence = 0.8 },
            Priority = 7
        },
        new ContentDetectionRule
        {
            Name = "commonjs-require",
            Pattern = @"^\s*(?:const|var|let)\s+.*\s*=\s*require\s*\(",
            Mapping = new GrammarMapping { Grammar = "JavaScript.grammar", Confidence = 0.7 },
            Priority = 6
        },

        // Python patterns
        new ContentDetectionRule
        {
            Name = "python-import",
            Pattern = @"^\s*(?:from\s+\w+(?:\.\w+)*\s+)?import\s+\w+",
            Mapping = new GrammarMapping { Grammar = "Python311.grammar", Version = "3.11", Confidence = 0.9 },
            Priority = 9
        },
        new ContentDetectionRule
        {
            Name = "python-def",
            Pattern = @"^\s*def\s+\w+\s*\(",
            Mapping = new GrammarMapping { Grammar = "Python311.grammar", Version = "3.11", Confidence = 0.85 },
            Priority = 8
        },
        new ContentDetectionRule
        {
            Name = "python-class",
            Pattern = @"^\s*class\s+\w+(?:\([^)]*\))?\s*:",
            Mapping = new GrammarMapping { Grammar = "Python311.grammar", Version = "3.11", Confidence = 0.9 },
            Priority = 9
        },

        // Java patterns
        new ContentDetectionRule
        {
            Name = "java-package",
            Pattern = @"^\s*package\s+[\w\.]+\s*;",
            Mapping = new GrammarMapping { Grammar = "Java17.grammar", Version = "17", Confidence = 0.95 },
            Priority = 10
        },
        new ContentDetectionRule
        {
            Name = "java-public-class",
            Pattern = @"^\s*public\s+class\s+\w+\s*(?:extends|implements|\{)",
            Mapping = new GrammarMapping { Grammar = "Java17.grammar", Version = "17", Confidence = 0.9 },
            Priority = 9
        },

        // C/C++ patterns
        new ContentDetectionRule
        {
            Name = "cpp-include",
            Pattern = @"^\s*#include\s*<[^>]+>",
            Mapping = new GrammarMapping { Grammar = "Cpp20.grammar", Version = "20", Confidence = 0.8 },
            Priority = 7
        },
        new ContentDetectionRule
        {
            Name = "cpp-namespace",
            Pattern = @"^\s*namespace\s+\w+\s*{",
            Mapping = new GrammarMapping { Grammar = "Cpp20.grammar", Version = "20", Confidence = 0.9 },
            Priority = 9
        },
        new ContentDetectionRule
        {
            Name = "c-include",
            Pattern = @"^\s*#include\s*[""<][^"">]+["">\}]",
            Mapping = new GrammarMapping { Grammar = "C17.grammar", Version = "17", Confidence = 0.7 },
            Priority = 6
        },

        // Rust patterns
        new ContentDetectionRule
        {
            Name = "rust-use",
            Pattern = @"^\s*use\s+[\w:]+(?:\s*::\s*{[^}]*})?\s*;",
            Mapping = new GrammarMapping { Grammar = "Rust2021.grammar", Version = "2021", Confidence = 0.9 },
            Priority = 9
        },
        new ContentDetectionRule
        {
            Name = "rust-fn",
            Pattern = @"^\s*(?:pub\s+)?fn\s+\w+",
            Mapping = new GrammarMapping { Grammar = "Rust2021.grammar", Version = "2021", Confidence = 0.85 },
            Priority = 8
        },

        // Go patterns
        new ContentDetectionRule
        {
            Name = "go-package",
            Pattern = @"^\s*package\s+\w+",
            Mapping = new GrammarMapping { Grammar = "Go119.grammar", Version = "1.19", Confidence = 0.95 },
            Priority = 10
        },
        new ContentDetectionRule
        {
            Name = "go-func",
            Pattern = @"^\s*func\s+(?:\([^)]*\)\s+)?\w+\s*\(",
            Mapping = new GrammarMapping { Grammar = "Go119.grammar", Version = "1.19", Confidence = 0.9 },
            Priority = 9
        },

        // HTML patterns
        new ContentDetectionRule
        {
            Name = "html-doctype",
            Pattern = @"^\s*<!DOCTYPE\s+html",
            Mapping = new GrammarMapping { Grammar = "HTMLEmbedded.grammar", Confidence = 0.95 },
            Priority = 10,
            CaseSensitive = false
        },
        new ContentDetectionRule
        {
            Name = "html-tags",
            Pattern = @"<(?:html|head|body|div|span|p|h[1-6])\b",
            Mapping = new GrammarMapping { Grammar = "HTMLEmbedded.grammar", Confidence = 0.8 },
            Priority = 7,
            CaseSensitive = false
        },

        // CSS patterns
        new ContentDetectionRule
        {
            Name = "css-selector",
            Pattern = @"^\s*[.#]?[\w-]+(?:\s*[,.]\s*[.#]?[\w-]+)*\s*{",
            Mapping = new GrammarMapping { Grammar = "CSS.grammar", Confidence = 0.85 },
            Priority = 8
        },

        // JSON patterns
        new ContentDetectionRule
        {
            Name = "json-object",
            Pattern = @"^\s*{\s*""[^""]+"":",
            Mapping = new GrammarMapping { Grammar = "JSON.grammar", Confidence = 0.9 },
            Priority = 9
        },

        // XML patterns
        new ContentDetectionRule
        {
            Name = "xml-declaration",
            Pattern = @"^\s*<\?xml\s+version\s*=",
            Mapping = new GrammarMapping { Grammar = "XML.grammar", Confidence = 0.95 },
            Priority = 10,
            CaseSensitive = false
        }
    };

    private static readonly object _contentRulesLock = new();

    /// <summary>
    /// Gets the detector identifier.
    /// </summary>
    public string DetectorId => "content-based";

    /// <summary>
    /// Gets the priority of this detector (higher than extension-based).
    /// </summary>
    public int Priority => 200;

    /// <summary>
    /// Detects grammar based on file content analysis.
    /// </summary>
    /// <param name="context">The detection context.</param>
    /// <returns>A task that represents the asynchronous detection operation.</returns>
    public async Task<GrammarDetectionResult> DetectGrammarAsync(GrammarDetectionContext context)
    {
        if (!CanDetect(context))
        {
            return GrammarDetectionResult.Failure(
                "No file content available for analysis",
                DetectorId);
        }

        var content = context.FileContent?.Value ?? string.Empty;
        if (string.IsNullOrEmpty(content))
        {
            return GrammarDetectionResult.Failure(
                "File content is empty",
                DetectorId);
        }

        var metadata = new Dictionary<string, object>
        {
            ["contentLength"] = content.Length,
            ["detectionMethod"] = "content-analysis"
        };

        // Get rules from configuration first, then add default rules
        var rules = new List<ContentDetectionRule>();
        if (context.Configuration?.ContentRules != null)
        {
            rules.AddRange(context.Configuration.ContentRules);
        }

        lock (_contentRulesLock)
        {
            rules.AddRange(DefaultContentRules);
        }

        // Sort rules by priority (highest first)
        rules.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        var lines = content.Split('\n', StringSplitOptions.None);
        var bestMatch = await AnalyzeContentAsync(lines, rules);

        if (bestMatch.HasValue)
        {
            var match = bestMatch.Value;
            metadata["matchedRule"] = match.rule.Name;
            metadata["matchedPattern"] = match.rule.Pattern;
            metadata["matchLine"] = match.lineNumber;

            var version = GrammarVersion.TryParse(match.rule.Mapping.Version, out var parsedVersion) ? parsedVersion : null;

            return GrammarDetectionResult.Success(
                match.rule.Mapping.Grammar,
                version,
                match.rule.Mapping.Confidence,
                DetectorId,
                metadata,
                match.rule.Mapping.Fallbacks);
        }

        return GrammarDetectionResult.Failure(
            "No content patterns matched",
            DetectorId,
            metadata);
    }

    /// <summary>
    /// Determines if this detector can handle the given context.
    /// </summary>
    /// <param name="context">The detection context.</param>
    /// <returns>True if file content is available, false otherwise.</returns>
    public bool CanDetect(GrammarDetectionContext context)
    {
        return context.FileContent != null && context.FileExists;
    }

    private async Task<(ContentDetectionRule rule, int lineNumber)?> AnalyzeContentAsync(string[] lines, List<ContentDetectionRule> rules)
    {
        foreach (var rule in rules)
        {
            var maxLines = rule.MaxLines > 0 ? Math.Min(rule.MaxLines, lines.Length) : lines.Length;
            var regexOptions = rule.CaseSensitive ? RegexOptions.Multiline : RegexOptions.Multiline | RegexOptions.IgnoreCase;

            try
            {
                var regex = new Regex(rule.Pattern, regexOptions);

                for (int i = 0; i < maxLines; i++)
                {
                    if (regex.IsMatch(lines[i]))
                    {
                        return (rule, i + 1);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Skip invalid regex patterns
                continue;
            }
        }

        await Task.CompletedTask;
        return null;
    }

    /// <summary>
    /// Adds a custom content detection rule.
    /// </summary>
    /// <param name="rule">The content detection rule to add.</param>
    public static void AddContentRule(ContentDetectionRule rule)
    {
        lock (_contentRulesLock)
        {
            DefaultContentRules.Add(rule);
            DefaultContentRules.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }
    }

    /// <summary>
    /// Gets all default content detection rules.
    /// </summary>
    /// <returns>A read-only list of content detection rules.</returns>
    public static IReadOnlyList<ContentDetectionRule> GetDefaultRules()
    {
        lock (_contentRulesLock)
        {
            return DefaultContentRules.ToList().AsReadOnly();
        }
    }
}
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

using System.Text;
using Minotaur.Core.Models.Grammar;

namespace Minotaur.Projects.Grammar;

/// <summary>
/// A syntax error found while parsing a <c>.extension</c> file, with
/// line/column information.
/// </summary>
public class ExtensionParseError
{
    /// <summary>Gets or sets the error message.</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>Gets or sets the 1-based line number of the error.</summary>
    public int Line { get; set; }

    /// <summary>Gets or sets the 1-based column number of the error.</summary>
    public int Column { get; set; }

    /// <summary>Returns a string representation of this error.</summary>
    public override string ToString()
    {
        return $"Line {Line}, Column {Column}: {Message}";
    }
}

/// <summary>
/// The result of parsing a <c>.extension</c> file: the loaded
/// <see cref="GrammarExtension"/> (null when parsing failed) plus any errors.
/// </summary>
public class ExtensionParseResult
{
    /// <summary>Gets the parsed extension, or null when parsing failed.</summary>
    public GrammarExtension? Extension { get; init; }

    /// <summary>Gets the errors found while parsing (empty when successful).</summary>
    public IReadOnlyList<ExtensionParseError> Errors { get; init; } = Array.Empty<ExtensionParseError>();

    /// <summary>Gets a value indicating whether parsing succeeded.</summary>
    public bool Success => Extension is not null && Errors.Count == 0;
}

/// <summary>
/// Parses the <c>.extension</c> file format into <see cref="GrammarExtension"/>
/// objects (Minotaur issue #88, item 2 — the file side; the runtime merge side
/// is provided by the DevelApp.StepParser grammar composition engine).
/// <para>
/// The file format is:
/// <code>
/// Grammar: CEBNF
/// EmbeddedLanguages: JavaScript, CSS
/// ContextAware: true
/// BaseGrammar: HTMLEmbedded.grammar
/// MergeStrategy: Replace
///
/// /* Token definitions (default action: Add) */
/// TOKEN_NAME = regex_pattern
/// RULE_NAME ::= (sub_rule other_rule)
///
/// /* Explicit operations: '+NAME' add, '-NAME' remove, '~NAME' override */
/// ~OVERRIDE_ME = new_pattern
/// -REMOVE_ME
///
/// /* Context-sensitive entries */
/// @CONTEXT[CSS] {
///     CSS_SELECTOR = [.#]?[a-zA-Z][a-zA-Z0-9_-]*
/// }
///
/// /* Cross-language validation hints */
/// @VALIDATE {
///     rule_name: FROM_TOKEN -> TO_TOKEN
/// }
///
/// /* Optimization hints (advisory, captured raw) */
/// @OPTIMIZE {
///     cache_patterns: [IDENTIFIER, WHITESPACE]
/// }
/// </code>
/// </para>
/// </summary>
public static class GrammarExtensionLoader
{
    /// <summary>
    /// Loads and parses a grammar extension from a file.
    /// </summary>
    /// <param name="extensionFilePath">The path to the <c>.extension</c> file.</param>
    /// <returns>The parse result containing the extension and any errors.</returns>
    public static async Task<ExtensionParseResult> LoadFromFileAsync(string extensionFilePath)
    {
        if (string.IsNullOrWhiteSpace(extensionFilePath))
        {
            throw new ArgumentException("Extension file path cannot be null or empty", nameof(extensionFilePath));
        }

        if (!File.Exists(extensionFilePath))
        {
            throw new FileNotFoundException($"Extension file not found: {extensionFilePath}", extensionFilePath);
        }

        var content = await File.ReadAllTextAsync(extensionFilePath);
        // Strip UTF-8 BOM if present so the header line parses cleanly.
        if (content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        var result = Parse(content);
        if (result.Extension is not null)
        {
            result.Extension.FilePath = extensionFilePath;
            if (string.IsNullOrEmpty(result.Extension.Name))
            {
                result.Extension.Name = Path.GetFileNameWithoutExtension(extensionFilePath);
            }
        }

        return result;
    }

    /// <summary>
    /// Loads and parses all <c>.extension</c> files found in a directory
    /// (non-recursive).
    /// </summary>
    /// <param name="directoryPath">The directory to scan.</param>
    /// <returns>The parse results for every <c>.extension</c> file found.</returns>
    public static async Task<IReadOnlyList<ExtensionParseResult>> LoadFromDirectoryAsync(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
        {
            return Array.Empty<ExtensionParseResult>();
        }

        var results = new List<ExtensionParseResult>();
        foreach (var filePath in Directory.EnumerateFiles(directoryPath, "*.extension", SearchOption.TopDirectoryOnly)
                     .OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                results.Add(await LoadFromFileAsync(filePath));
            }
            // Record I/O failures as per-file parse errors instead of letting
            // one unreadable file abort the whole directory load.
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
            {
                results.Add(new ExtensionParseResult
                {
                    Extension = null,
                    Errors = new[] { new ExtensionParseError { Message = ex.Message, Line = 0, Column = 0 } }
                });
            }
        }

        return results;
    }

    /// <summary>
    /// Parses grammar extension content (without loading from disk).
    /// </summary>
    /// <param name="content">The <c>.extension</c> file content.</param>
    /// <param name="name">Optional name for the extension.</param>
    /// <returns>The parse result containing the extension and any errors.</returns>
    public static ExtensionParseResult Parse(string content, string name = "")
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        var extension = new GrammarExtension { Name = name };
        var errors = new List<ExtensionParseError>();

        // Strip UTF-8 BOM if present so the header line parses cleanly.
        if (content.Length > 0 && content[0] == '\uFEFF')
        {
            content = content[1..];
        }

        var lines = content.Split('\n');

        var currentContext = string.Empty;
        var currentBlockKind = BlockKind.None;
        var inBlockComment = false;

        for (var i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i].TrimEnd('\r');
            var lineNumber = i + 1;
            var line = rawLine.Trim();

            // Multi-line /* ... */ comments
            if (inBlockComment)
            {
                if (line.Contains("*/"))
                {
                    inBlockComment = false;
                    line = line[(line.IndexOf("*/", StringComparison.Ordinal) + 2)..].Trim();
                }
                else
                {
                    continue;
                }
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            // Block comment start (single-line or multi-line)
            if (line.StartsWith("/*", StringComparison.Ordinal))
            {
                if (!line.Contains("*/"))
                {
                    inBlockComment = true;
                }

                continue;
            }

            if (line.StartsWith("//", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            // Block open: @CONTEXT[lang] {, @VALIDATE {, @OPTIMIZE {
            if (currentBlockKind == BlockKind.None && line.StartsWith("@", StringComparison.Ordinal))
            {
                if (TryParseBlockOpen(line, lineNumber, extension, errors, ref currentBlockKind, ref currentContext))
                {
                    continue;
                }

                continue;
            }

            // Block close
            if (line == "}")
            {
                currentBlockKind = BlockKind.None;
                currentContext = string.Empty;
                continue;
            }

            switch (currentBlockKind)
            {
                case BlockKind.Context:
                    ParseEntry(line, lineNumber, currentContext, extension, errors);
                    break;

                case BlockKind.Validate:
                    ParseValidationRule(line, lineNumber, extension, errors);
                    break;

                case BlockKind.Optimize:
                    extension.OptimizeHints.Add(line);
                    break;

                case BlockKind.None:
                    ParseTopLevel(line, lineNumber, extension, errors);
                    break;
            }
        }

        if (currentBlockKind != BlockKind.None)
        {
            errors.Add(new ExtensionParseError
            {
                Message = $"Unclosed '@{currentBlockKind.ToString().ToUpperInvariant()}' block at end of file",
                Line = lines.Length,
                Column = 1
            });
        }

        return new ExtensionParseResult
        {
            Extension = extension,
            Errors = errors
        };
    }

    private enum BlockKind
    {
        None,
        Context,
        Validate,
        Optimize
    }

    private static bool TryParseBlockOpen(
        string line,
        int lineNumber,
        GrammarExtension extension,
        List<ExtensionParseError> errors,
        ref BlockKind blockKind,
        ref string context)
    {
        // @CONTEXT[lang] {
        if (line.StartsWith("@CONTEXT[", StringComparison.Ordinal))
        {
            var closeIndex = line.IndexOf(']', StringComparison.Ordinal);
            if (closeIndex < 0)
            {
                errors.Add(new ExtensionParseError
                {
                    Message = "Malformed @CONTEXT block: missing ']' after language name",
                    Line = lineNumber,
                    Column = line.Length + 1
                });
                return false;
            }

            context = line[9..closeIndex].Trim();
            if (string.IsNullOrEmpty(context))
            {
                errors.Add(new ExtensionParseError
                {
                    Message = "Empty language name in @CONTEXT block",
                    Line = lineNumber,
                    Column = 10
                });
                return false;
            }

            extension.IsContextAware = true;
            extension.EmbeddedLanguages.Add(context);
            blockKind = BlockKind.Context;
            extension.ContextRules[context] = new List<ExtensionEntry>();
            return true;
        }

        if (line.StartsWith("@VALIDATE", StringComparison.Ordinal))
        {
            blockKind = BlockKind.Validate;
            return true;
        }

        if (line.StartsWith("@OPTIMIZE", StringComparison.Ordinal))
        {
            blockKind = BlockKind.Optimize;
            return true;
        }

        errors.Add(new ExtensionParseError
        {
            Message = $"Unknown directive: '{line}'",
            Line = lineNumber,
            Column = 1
        });
        return false;
    }

    private static void ParseTopLevel(
        string line,
        int lineNumber,
        GrammarExtension extension,
        List<ExtensionParseError> errors)
    {
        var separatorIndex = line.IndexOf(':');
        if (separatorIndex > 0 && (line.StartsWith("Grammar:", StringComparison.OrdinalIgnoreCase) ||
                                   line.StartsWith("EmbeddedLanguages:", StringComparison.OrdinalIgnoreCase) ||
                                   line.StartsWith("ContextAware:", StringComparison.OrdinalIgnoreCase) ||
                                   line.StartsWith("BaseGrammar:", StringComparison.OrdinalIgnoreCase) ||
                                   line.StartsWith("MergeStrategy:", StringComparison.OrdinalIgnoreCase)))
        {
            ParseHeader(line, separatorIndex, lineNumber, extension, errors);
            return;
        }

        ParseEntry(line, lineNumber, string.Empty, extension, errors);
    }

    private static void ParseHeader(
        string line,
        int separatorIndex,
        int lineNumber,
        GrammarExtension extension,
        List<ExtensionParseError> errors)
    {
        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();
        var column = separatorIndex + 2;

        switch (key.ToLowerInvariant())
        {
            case "grammar":
                if (value.Equals("CEBNF", StringComparison.OrdinalIgnoreCase))
                {
                    extension.GrammarType = ExtensionGrammarType.CEBNF;
                }
                else if (value.Equals("BNF", StringComparison.OrdinalIgnoreCase))
                {
                    extension.GrammarType = ExtensionGrammarType.BNF;
                }
                else
                {
                    errors.Add(new ExtensionParseError
                    {
                        Message = $"Unknown grammar type '{value}' (expected BNF or CEBNF)",
                        Line = lineNumber,
                        Column = column
                    });
                }

                break;

            case "embeddedlanguages":
                extension.SetEmbeddedLanguagesFromHeader(value);
                break;

            case "contextaware":
                extension.IsContextAware = value.Equals("true", StringComparison.OrdinalIgnoreCase);
                break;

            case "basegrammar":
                extension.BaseGrammarRef = value;
                break;

            case "mergestrategy":
                if (value.Equals("Additive", StringComparison.OrdinalIgnoreCase))
                {
                    extension.MergeStrategy = ExtensionMergeStrategy.Additive;
                }
                else if (value.Equals("Replace", StringComparison.OrdinalIgnoreCase))
                {
                    extension.MergeStrategy = ExtensionMergeStrategy.Replace;
                }
                else if (value.Equals("PriorityBased", StringComparison.OrdinalIgnoreCase))
                {
                    extension.MergeStrategy = ExtensionMergeStrategy.PriorityBased;
                }
                else
                {
                    errors.Add(new ExtensionParseError
                    {
                        Message = $"Unknown merge strategy '{value}' (expected Additive, Replace, or PriorityBased)",
                        Line = lineNumber,
                        Column = column
                    });
                }

                break;
        }
    }

    private static void ParseEntry(
        string line,
        int lineNumber,
        string context,
        GrammarExtension extension,
        List<ExtensionParseError> errors)
    {
        // Operation prefixes: '+' add, '-' remove, '~' override (default: add).
        var action = ExtensionAction.Add;
        var body = line;
        if (line.StartsWith('+'))
        {
            action = ExtensionAction.Add;
            body = line[1..];
        }
        else if (line.StartsWith('-'))
        {
            action = ExtensionAction.Remove;
            body = line[1..];
        }
        else if (line.StartsWith('~'))
        {
            action = ExtensionAction.Override;
            body = line[1..];
        }

        var bodySpan = body.Trim();

        string name;
        string pattern;
        bool isRule;

        var ruleIndex = bodySpan.IndexOf("::=", StringComparison.Ordinal);
        var tokenIndex = bodySpan.IndexOf('=');

        if (ruleIndex >= 0)
        {
            isRule = true;
            name = bodySpan[..ruleIndex].Trim().Trim('<', '>');
            pattern = bodySpan[(ruleIndex + 3)..].Trim();
        }
        else if (tokenIndex > 0)
        {
            isRule = false;
            name = bodySpan[..tokenIndex].Trim();
            pattern = bodySpan[(tokenIndex + 1)..].Trim();
        }
        else if (action == ExtensionAction.Remove)
        {
            // Bare removal: '-NAME' or 'Remove NAME'
            isRule = false;
            name = bodySpan.TrimEnd(';', '.');
            pattern = string.Empty;
        }
        else
        {
            errors.Add(new ExtensionParseError
            {
                Message = $"Malformed entry (expected 'NAME = pattern' or 'RULE ::= production'): '{line}'",
                Line = lineNumber,
                Column = 1
            });
            return;
        }

        if (string.IsNullOrEmpty(name) || !char.IsLetterOrDigit(name[0]) && name[0] != '_')
        {
            errors.Add(new ExtensionParseError
            {
                Message = $"Invalid entry name '{name}'",
                Line = lineNumber,
                Column = 1
            });
            return;
        }

        if (action != ExtensionAction.Remove && string.IsNullOrEmpty(pattern))
        {
            errors.Add(new ExtensionParseError
            {
                Message = $"Entry '{name}' has an empty pattern (incomplete rule?)",
                Line = lineNumber,
                Column = Math.Max(1, bodySpan.IndexOf(name, StringComparison.Ordinal) + name.Length + 2)
            });
            return;
        }

        var entry = new ExtensionEntry
        {
            Name = name,
            Pattern = pattern,
            Action = action,
            Context = context,
            IsRule = isRule,
            SourceLine = lineNumber
        };

        if (string.IsNullOrEmpty(context))
        {
            extension.Entries.Add(entry);
        }
        else
        {
            extension.ContextRules[context].Add(entry);
        }
    }

    private static void ParseValidationRule(
        string line,
        int lineNumber,
        GrammarExtension extension,
        List<ExtensionParseError> errors)
    {
        var arrowIndex = line.IndexOf("->", StringComparison.Ordinal);
        var colonIndex = line.IndexOf(':', StringComparison.Ordinal);

        if (colonIndex < 0 || arrowIndex < 0 || arrowIndex < colonIndex)
        {
            // Not a validation rule line (e.g. stray comment) — advisory skip.
            return;
        }

        var name = line[..colonIndex].Trim();
        var from = line[(colonIndex + 1)..arrowIndex].Trim();
        var to = line[(arrowIndex + 2)..].Trim();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to))
        {
            errors.Add(new ExtensionParseError
            {
                Message = $"Malformed validation rule (expected 'name: FROM -> TO'): '{line}'",
                Line = lineNumber,
                Column = 1
            });
            return;
        }

        extension.ValidationRules.Add(new ExtensionValidationRule
        {
            Name = name,
            From = from,
            To = to,
            SourceLine = lineNumber
        });
    }
}

/// <summary>
/// Helper for parsing the EmbeddedLanguages header value.
/// </summary>
internal static class GrammarExtensionHeaderExtensions
{
    public static void SetEmbeddedLanguagesFromHeader(this GrammarExtension extension, string value)
    {
        foreach (var language in value.Split(
                     ',',
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                     .Where(language => !extension.EmbeddedLanguages.Contains(language, StringComparer.OrdinalIgnoreCase)))
        {
            extension.EmbeddedLanguages.Add(language);
        }
    }
}

/// <summary>
/// Renders a <see cref="GrammarExtension"/> as DevelApp.StepParser grammar
/// file content usable as an overlay in
/// <c>GrammarLoader.ComposeWithOverlayContent</c>. Removals are not part of
/// the overlay (the merge engine composes overlays additively); they are
/// applied by the caller after composition.
/// </summary>
public static class GrammarExtensionOverlayRenderer
{
    /// <summary>
    /// Renders the add/override entries of a grammar extension as overlay
    /// grammar content in DevelApp.StepParser grammar file syntax.
    /// </summary>
    /// <param name="extension">The extension to render.</param>
    /// <returns>Overlay grammar content.</returns>
    public static string RenderOverlay(GrammarExtension extension)
    {
        if (extension is null)
        {
            throw new ArgumentNullException(nameof(extension));
        }

        var sb = new StringBuilder();
        sb.AppendLine($"# Overlay rendered from grammar extension '{extension.Name}'");
        sb.AppendLine($"Grammar: {extension.Name}Overlay");
        sb.AppendLine();

        foreach (var entry in extension.AllEntries.Where(e => e.Action != ExtensionAction.Remove))
        {
            var contextSuffix = string.IsNullOrEmpty(entry.Context)
                ? string.Empty
                : $" ({entry.Context.ToLowerInvariant()}-context)";

            if (entry.IsRule)
            {
                sb.AppendLine($"<{entry.Name}{contextSuffix}> ::= {entry.Pattern}");
            }
            else
            {
                sb.AppendLine($"<{entry.Name}{contextSuffix}> ::= /{entry.Pattern}/");
            }
        }

        if (extension.OptimizeHints.Count > 0)
        {
            sb.AppendLine();
            foreach (var hint in extension.OptimizeHints)
            {
                sb.AppendLine($"# OPTIMIZE: {hint}");
            }
        }

        return sb.ToString();
    }
}

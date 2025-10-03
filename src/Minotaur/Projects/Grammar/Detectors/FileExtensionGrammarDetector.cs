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

namespace Minotaur.Projects.Grammar.Detectors;

/// <summary>
/// Grammar detector that uses file extensions to determine the appropriate grammar.
/// This is the most basic detection strategy and serves as a fallback for other detectors.
/// </summary>
public class FileExtensionGrammarDetector : IGrammarDetector
{
    private static readonly Dictionary<string, GrammarMapping> DefaultExtensionMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".cs", new GrammarMapping { Grammar = "CSharp10.grammar", Version = "10.0", Confidence = 0.9 } },
        { ".vb", new GrammarMapping { Grammar = "VisualBasic.grammar", Version = "16.0", Confidence = 0.9 } },
        { ".fs", new GrammarMapping { Grammar = "FSharp.grammar", Version = "6.0", Confidence = 0.9 } },
        { ".razor", new GrammarMapping { Grammar = "HTMLEmbedded.grammar", Confidence = 0.8, Fallbacks = ["CSharp10.grammar"] } },
        { ".cshtml", new GrammarMapping { Grammar = "HTMLEmbedded.grammar", Confidence = 0.8, Fallbacks = ["CSharp10.grammar"] } },
        { ".xaml", new GrammarMapping { Grammar = "XAML.grammar", Confidence = 0.9 } },
        { ".json", new GrammarMapping { Grammar = "JSON.grammar", Confidence = 1.0 } },
        { ".xml", new GrammarMapping { Grammar = "XML.grammar", Confidence = 0.9 } },
        { ".config", new GrammarMapping { Grammar = "XML.grammar", Confidence = 0.8 } },
        { ".js", new GrammarMapping { Grammar = "JavaScriptES2022.grammar", Version = "ES2022", Confidence = 0.7, Fallbacks = ["JavaScript.grammar"] } },
        { ".mjs", new GrammarMapping { Grammar = "JavaScriptES2022.grammar", Version = "ES2022", Confidence = 0.8 } },
        { ".ts", new GrammarMapping { Grammar = "TypeScript.grammar", Version = "4.9", Confidence = 0.9 } },
        { ".tsx", new GrammarMapping { Grammar = "TypeScript.grammar", Version = "4.9", Confidence = 0.9 } },
        { ".py", new GrammarMapping { Grammar = "Python311.grammar", Version = "3.11", Confidence = 0.9 } },
        { ".pyw", new GrammarMapping { Grammar = "Python311.grammar", Version = "3.11", Confidence = 0.9 } },
        { ".java", new GrammarMapping { Grammar = "Java17.grammar", Version = "17", Confidence = 0.9 } },
        { ".cpp", new GrammarMapping { Grammar = "Cpp20.grammar", Version = "20", Confidence = 0.9 } },
        { ".cxx", new GrammarMapping { Grammar = "Cpp20.grammar", Version = "20", Confidence = 0.9 } },
        { ".cc", new GrammarMapping { Grammar = "Cpp20.grammar", Version = "20", Confidence = 0.9 } },
        { ".hpp", new GrammarMapping { Grammar = "Cpp20.grammar", Version = "20", Confidence = 0.9 } },
        { ".hxx", new GrammarMapping { Grammar = "Cpp20.grammar", Version = "20", Confidence = 0.9 } },
        { ".h", new GrammarMapping { Grammar = "C17.grammar", Version = "17", Confidence = 0.7, Fallbacks = ["Cpp20.grammar"] } },
        { ".c", new GrammarMapping { Grammar = "C17.grammar", Version = "17", Confidence = 0.9 } },
        { ".rs", new GrammarMapping { Grammar = "Rust2021.grammar", Version = "2021", Confidence = 0.9 } },
        { ".go", new GrammarMapping { Grammar = "Go119.grammar", Version = "1.19", Confidence = 0.9 } },
        { ".html", new GrammarMapping { Grammar = "HTMLEmbedded.grammar", Confidence = 0.8 } },
        { ".htm", new GrammarMapping { Grammar = "HTMLEmbedded.grammar", Confidence = 0.8 } },
        { ".css", new GrammarMapping { Grammar = "CSS.grammar", Confidence = 0.9 } },
        { ".scss", new GrammarMapping { Grammar = "CSS.grammar", Confidence = 0.7 } },
        { ".sass", new GrammarMapping { Grammar = "CSS.grammar", Confidence = 0.7 } },
        { ".less", new GrammarMapping { Grammar = "CSS.grammar", Confidence = 0.7 } },
        { ".sql", new GrammarMapping { Grammar = "SQL.grammar", Confidence = 0.8 } }
    };

    /// <summary>
    /// Gets the detector identifier.
    /// </summary>
    public string DetectorId => "file-extension";

    /// <summary>
    /// Gets the priority of this detector (lower priority than content-based detectors).
    /// </summary>
    public int Priority => 100;

    /// <summary>
    /// Detects grammar based on file extension.
    /// </summary>
    /// <param name="context">The detection context.</param>
    /// <returns>A task that represents the asynchronous detection operation.</returns>
    public async Task<GrammarDetectionResult> DetectGrammarAsync(GrammarDetectionContext context)
    {
        if (!CanDetect(context))
        {
            return GrammarDetectionResult.Failure(
                "No file extension available for detection", 
                DetectorId);
        }

        var extension = context.FileExtension;
        var metadata = new Dictionary<string, object>
        {
            ["extension"] = extension,
            ["detectionMethod"] = "file-extension"
        };

        // Check project-specific configuration first
        var configMapping = context.Configuration?.GetMappingForExtension(extension);
        if (configMapping != null)
        {
            metadata["source"] = "project-configuration";
            var version = GrammarVersion.TryParse(configMapping.Version, out var parsedVersion) ? parsedVersion : null;
            
            return GrammarDetectionResult.Success(
                configMapping.Grammar,
                version,
                configMapping.Confidence,
                DetectorId,
                metadata,
                configMapping.Fallbacks);
        }

        // Use default extension mappings
        if (DefaultExtensionMappings.TryGetValue(extension, out var mapping))
        {
            metadata["source"] = "default-mapping";
            var version = GrammarVersion.TryParse(mapping.Version, out var parsedVersion) ? parsedVersion : null;
            
            return GrammarDetectionResult.Success(
                mapping.Grammar,
                version,
                mapping.Confidence,
                DetectorId,
                metadata,
                mapping.Fallbacks);
        }

        await Task.CompletedTask;
        return GrammarDetectionResult.Failure(
            $"No grammar mapping found for extension '{extension}'", 
            DetectorId,
            metadata);
    }

    /// <summary>
    /// Determines if this detector can handle the given context.
    /// </summary>
    /// <param name="context">The detection context.</param>
    /// <returns>True if the file has an extension, false otherwise.</returns>
    public bool CanDetect(GrammarDetectionContext context)
    {
        return !string.IsNullOrEmpty(context.FileExtension);
    }

    /// <summary>
    /// Adds or updates a default extension mapping.
    /// </summary>
    /// <param name="extension">The file extension (with or without dot).</param>
    /// <param name="mapping">The grammar mapping.</param>
    public static void AddExtensionMapping(string extension, GrammarMapping mapping)
    {
        if (!extension.StartsWith("."))
            extension = "." + extension;

        DefaultExtensionMappings[extension] = mapping;
    }

    /// <summary>
    /// Gets all supported extensions.
    /// </summary>
    /// <returns>An array of supported file extensions.</returns>
    public static string[] GetSupportedExtensions()
    {
        return DefaultExtensionMappings.Keys.ToArray();
    }

    /// <summary>
    /// Gets the default mapping for a specific extension.
    /// </summary>
    /// <param name="extension">The file extension.</param>
    /// <returns>The grammar mapping if found, null otherwise.</returns>
    public static GrammarMapping? GetDefaultMapping(string extension)
    {
        if (!extension.StartsWith("."))
            extension = "." + extension;

        return DefaultExtensionMappings.TryGetValue(extension, out var mapping) ? mapping : null;
    }
}
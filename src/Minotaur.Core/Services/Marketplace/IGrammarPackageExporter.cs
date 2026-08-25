using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Interface for exporting grammars as packages for the marketplace
    /// </summary>
    public interface IGrammarPackageExporter
    {
        /// <summary>
        /// Export a grammar as a package (.tar.gz) for publishing to the marketplace
        /// </summary>
        /// <param name="grammarDefinition">The grammar definition to export</param>
        /// <param name="outputPath">Path to save the package (optional, returns stream if null)</param>
        /// <param name="options">Export options</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Stream containing the grammar package or null if saved to file</returns>
        Task<Stream> ExportGrammarPackageAsync(
            object grammarDefinition,
            string outputPath = null,
            GrammarExportOptions options = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate a grammar definition before export
        /// </summary>
        /// <param name="grammarDefinition">The grammar definition to validate</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Validation result</returns>
        Task<GrammarValidationResult> ValidateGrammarAsync(
            object grammarDefinition,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Create metadata for a grammar package
        /// </summary>
        /// <param name="grammarDefinition">The grammar definition</param>
        /// <param name="options">Export options</param>
        /// <returns>Grammar metadata</returns>
        GrammarMetadata CreateMetadata(object grammarDefinition, GrammarExportOptions options);
    }

    /// <summary>
    /// Options for grammar export
    /// </summary>
    public class GrammarExportOptions
    {
        public string Vendor { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string Description { get; set; }
        public string License { get; set; } = "MIT";
        public string[] Tags { get; set; } = Array.Empty<string>();
        public string MinotaurVersion { get; set; }
        public string MainFile { get; set; }
        public string PricingModel { get; set; } = "free";
        public decimal Price { get; set; } = 0;
    }

    /// <summary>
    /// Result of grammar validation
    /// </summary>
    public class GrammarValidationResult
    {
        public bool IsValid { get; set; } = true;
        public string[] Errors { get; set; } = Array.Empty<string>();
        public string[] Warnings { get; set; } = Array.Empty<string>();
        public string[] MissingFields { get; set; } = Array.Empty<string>();
    }

    /// <summary>
    /// Information about an installed grammar
    /// </summary>
    public class InstalledGrammar
    {
        public string Vendor { get; set; }
        public string Name { get; set; }
        public string Version { get; set; }
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string License { get; set; }
        public string[] Tags { get; set; }
        public string Path { get; set; }
        public DateTime InstalledAt { get; set; }
        public string Source { get; set; } = "marketplace";
    }
}

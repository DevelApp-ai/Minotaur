using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Minotaur.Core.Services.Marketplace
{
    /// <summary>
    /// Default implementation of IGrammarPackageExporter for exporting grammars as packages
    /// </summary>
    public class GrammarPackageExporter : IGrammarPackageExporter
    {
        private readonly string _tempDirectory;

        /// <summary>
        /// Initializes a new instance of the GrammarPackageExporter
        /// </summary>
        public GrammarPackageExporter()
        {
            _tempDirectory = Path.Combine(
                Path.GetTempPath(),
                "MinotaurGrammarExport");
            Directory.CreateDirectory(_tempDirectory);
        }

        /// <summary>
        /// Export a grammar as a package (.tar.gz) for publishing to the marketplace
        /// </summary>
        public async Task<Stream> ExportGrammarPackageAsync(
            object grammarDefinition,
            string outputPath = null,
            GrammarExportOptions options = null,
            CancellationToken cancellationToken = default)
        {
            // Validate the grammar
            var validation = await ValidateGrammarAsync(grammarDefinition, cancellationToken);
            if (!validation.IsValid)
            {
                throw new MarketplaceException(string.Join(
                    Environment.NewLine, 
                    validation.Errors));
            }

            // Create metadata
            var metadata = CreateMetadata(grammarDefinition, options);

            // Create a temporary directory for the package
            var packageDir = Path.Combine(
                _tempDirectory,
                Guid.NewGuid().ToString());
            Directory.CreateDirectory(packageDir);

            try
            {
                // Export grammar files to the temporary directory
                await ExportGrammarFilesAsync(grammarDefinition, packageDir, cancellationToken);

                // Add metadata file
                var metadataPath = Path.Combine(packageDir, "minotaur-metadata.json");
                var metadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(metadataPath, metadataJson, cancellationToken);

                // Create the tar.gz package
                var packageStream = new MemoryStream();
                
                using (var archive = new TarArchive(packageStream, TarArchiveMode.Create))
                {
                    await AddDirectoryToTarAsync(archive, packageDir, "", cancellationToken);
                }

                packageStream.Position = 0;

                // Save to file if output path specified
                if (!string.IsNullOrEmpty(outputPath))
                {
                    using var fileStream = File.Create(outputPath);
                    await packageStream.CopyToAsync(fileStream, cancellationToken);
                    packageStream.Position = 0;
                }

                return packageStream;
            }
            finally
            {
                // Clean up temporary directory
                try
                {
                    Directory.Delete(packageDir, true);
                }
                catch
                {
                    // Ignore cleanup errors
                }
            }
        }

        /// <summary>
        /// Validate a grammar definition before export
        /// </summary>
        public async Task<GrammarValidationResult> ValidateGrammarAsync(
            object grammarDefinition,
            CancellationToken cancellationToken = default)
        {
            var result = new GrammarValidationResult { IsValid = true };

            // Check if grammar definition is null
            if (grammarDefinition == null)
            {
                result.IsValid = false;
                result.Errors = new[] { "Grammar definition cannot be null" };
                return result;
            }

            // Use reflection to check for required properties
            var type = grammarDefinition.GetType();
            var properties = type.GetProperties();

            // Check for common required properties
            var requiredProperties = new[] { "Name", "Version", "Description" };
            foreach (var propName in requiredProperties)
            {
                var prop = type.GetProperty(propName);
                if (prop == null)
                {
                    result.MissingFields = Array.Append(result.MissingFields, propName);
                    result.IsValid = false;
                }
                else
                {
                    var value = prop.GetValue(grammarDefinition);
                    if (value == null || string.IsNullOrWhiteSpace(value?.ToString()))
                    {
                        result.MissingFields = Array.Append(result.MissingFields, propName);
                        result.IsValid = false;
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Create metadata for a grammar package
        /// </summary>
        public GrammarMetadata CreateMetadata(object grammarDefinition, GrammarExportOptions options)
        {
            var type = grammarDefinition.GetType();
            var metadata = new GrammarMetadata
            {
                Name = GetPropertyValue<string>(grammarDefinition, "Name"),
                Vendor = options?.Vendor ?? GetPropertyValue<string>(grammarDefinition, "Vendor") ?? "unknown",
                DisplayName = GetPropertyValue<string>(grammarDefinition, "DisplayName"),
                Version = options?.Version ?? GetPropertyValue<string>(grammarDefinition, "Version") ?? "1.0.0",
                MinotaurVersion = options?.MinotaurVersion ?? GetPropertyValue<string>(grammarDefinition, "MinotaurVersion") ?? ">=1.0.0",
                Description = options?.Description ?? GetPropertyValue<string>(grammarDefinition, "Description") ?? string.Empty,
                License = options?.License ?? GetPropertyValue<string>(grammarDefinition, "License") ?? "MIT",
                Tags = options?.Tags ?? GetPropertyValue<string[]>(grammarDefinition, "Tags") ?? Array.Empty<string>(),
                MainFile = options?.MainFile ?? GetPropertyValue<string>(grammarDefinition, "MainFile") ?? "grammar.grammar",
                Dependencies = GetPropertyValue<Dictionary<string, string>>(grammarDefinition, "Dependencies") ?? new Dictionary<string, string>(),
                Documentation = GetPropertyValue<string>(grammarDefinition, "Documentation") ?? string.Empty,
                PricingModel = options?.PricingModel ?? "free",
                Price = options?.Price ?? 0
            };

            return metadata;
        }

        /// <summary>
        /// Export grammar files to a directory
        /// </summary>
        private async Task ExportGrammarFilesAsync(
            object grammarDefinition,
            string outputDirectory,
            CancellationToken cancellationToken)
        {
            // This is a placeholder - actual implementation would depend on the grammar definition structure
            // For now, we'll create a basic structure
            
            var type = grammarDefinition.GetType();
            
            // Create a basic grammar file
            var grammarContent = GetGrammarContent(grammarDefinition);
            var grammarFilePath = Path.Combine(outputDirectory, "grammar.grammar");
            await File.WriteAllTextAsync(grammarFilePath, grammarContent, cancellationToken);

            // Create package.json
            var packageJson = CreatePackageJson(grammarDefinition);
            var packageJsonPath = Path.Combine(outputDirectory, "package.json");
            await File.WriteAllTextAsync(packageJsonPath, packageJson, cancellationToken);
        }

        /// <summary>
        /// Get grammar content as string
        /// </summary>
        private string GetGrammarContent(object grammarDefinition)
        {
            // Placeholder - actual implementation would serialize the grammar
            // This could use CognitiveGraph serialization or other format
            return "// Grammar content would be generated here\n" +
                   "// From: " + grammarDefinition.GetType().Name + "\n" +
                   "// Version: " + GetPropertyValue<string>(grammarDefinition, "Version") + "\n";
        }

        /// <summary>
        /// Create package.json content
        /// </summary>
        private string CreatePackageJson(object grammarDefinition)
        {
            var packageInfo = new
            {
                name = GetPropertyValue<string>(grammarDefinition, "Name"),
                version = GetPropertyValue<string>(grammarDefinition, "Version"),
                description = GetPropertyValue<string>(grammarDefinition, "Description"),
                license = GetPropertyValue<string>(grammarDefinition, "License") ?? "MIT",
                minotaurVersion = GetPropertyValue<string>(grammarDefinition, "MinotaurVersion") ?? ">=1.0.0",
                keywords = GetPropertyValue<string[]>(grammarDefinition, "Tags") ?? Array.Empty<string>(),
                main = GetPropertyValue<string>(grammarDefinition, "MainFile") ?? "grammar.grammar"
            };

            return JsonSerializer.Serialize(packageInfo, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// Add a directory to a tar archive
        /// </summary>
        private async Task AddDirectoryToTarAsync(
            TarArchive archive,
            string directoryPath,
            string entryName,
            CancellationToken cancellationToken)
        {
            var files = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);
            
            foreach (var filePath in files)
            {
                cancellationToken.ThrowIfCancellationRequested();
                
                var relativePath = Path.GetRelativePath(directoryPath, filePath);
                var archiveEntryName = string.IsNullOrEmpty(entryName) 
                    ? relativePath.Replace('\\', '/')
                    : Path.Combine(entryName, relativePath).Replace('\\', '/');

                var entry = TarEntry.CreateEntryFromFile(filePath);
                entry.Name = archiveEntryName;
                await archive.WriteEntryAsync(entry, false, cancellationToken);
            }
        }

        /// <summary>
        /// Helper to get property value using reflection
        /// </summary>
        private T GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null || string.IsNullOrEmpty(propertyName))
                return default;

            var prop = obj.GetType().GetProperty(propertyName);
            if (prop == null)
                return default;

            var value = prop.GetValue(obj);
            return (T)value;
        }

        /// <summary>
        /// Simple Tar archive implementation for .NET (placeholder - would use SharpCompress or similar in production)
        /// </summary>
        private class TarArchive : IDisposable
        {
            private readonly Stream _stream;
            private readonly TarArchiveMode _mode;

            public enum TarArchiveMode { Create, Extract }

            public TarArchive(Stream stream, TarArchiveMode mode)
            {
                _stream = stream ?? throw new ArgumentNullException(nameof(stream));
                _mode = mode;
            }

            public async Task WriteEntryAsync(TarEntry entry, bool closeEntry, CancellationToken cancellationToken)
            {
                // Placeholder - actual implementation would write tar entries
                using var fileStream = File.OpenRead(entry.FilePath);
                await fileStream.CopyToAsync(_stream, cancellationToken);
            }

            public void Dispose()
            {
                _stream?.Dispose();
            }
        }

        /// <summary>
        /// Tar entry representation
        /// </summary>
        private class TarEntry
        {
            public string Name { get; set; }
            public string FilePath { get; set; }

            public static TarEntry CreateEntryFromFile(string filePath)
            {
                return new TarEntry
                {
                    FilePath = filePath,
                    Name = Path.GetFileName(filePath)
                };
            }
        }
    }
}

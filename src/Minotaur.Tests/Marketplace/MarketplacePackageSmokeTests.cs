using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Minotaur.Core.Services.Marketplace;
using Xunit;
using Xunit.Abstractions;

namespace Minotaur.Tests.Marketplace
{
    /// <summary>
    /// Package-only marketplace round-trip smoke test (Minotaur-Grammars #26):
    /// every grammar folder in a Minotaur-Grammars checkout must export through
    /// <see cref="GrammarPackageExporter"/> with zero manual edits. Env-gated on
    /// MINOTAUR_GRAMMARS_DIR (skips when unset); CI sets it to a fresh checkout
    /// of DevelApp-ai/Minotaur-Grammars@main. The publish/consume half still
    /// requires the (private) marketplace and is out of scope here.
    /// </summary>
    public class MarketplacePackageSmokeTests
    {
        private readonly ITestOutputHelper _output;

        public MarketplacePackageSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public async Task EveryRepoGrammar_PackagesWithoutManualEdits()
        {
            var dir = Environment.GetEnvironmentVariable("MINOTAUR_GRAMMARS_DIR");
            if (string.IsNullOrWhiteSpace(dir))
            {
                _output.WriteLine("MINOTAUR_GRAMMARS_DIR is not set - skipping package smoke test (offline/dev mode).");
                return;
            }

            dir = Path.GetFullPath(dir);
            var metadataFiles = Directory
                .GetFiles(dir, "minotaur-metadata.json", SearchOption.AllDirectories)
                .Where(p => !p.Replace('\\', '/').Contains("/test-grammars/"))
                .OrderBy(p => p, StringComparer.Ordinal)
                .ToArray();
            Assert.NotEmpty(metadataFiles);
            _output.WriteLine($"Packaging {metadataFiles.Length} grammars from {dir}");

            var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            serializerOptions.Converters.Add(new FlexibleDependenciesConverter());

            var exporter = new GrammarPackageExporter();
            var failures = 0;
            foreach (var metadataFile in metadataFiles)
            {
                var grammarDir = Path.GetDirectoryName(metadataFile)!;
                var relative = Path.GetRelativePath(dir, grammarDir);
                try
                {
                    // 1. Metadata must deserialize into the marketplace DTO as-is.
                    var info = JsonSerializer.Deserialize<GrammarInfo>(
                        await File.ReadAllTextAsync(metadataFile), serializerOptions);
                    Assert.NotNull(info);
                    Assert.False(string.IsNullOrWhiteSpace(info!.Name), $"{relative}: Name required at publish time");
                    Assert.False(string.IsNullOrWhiteSpace(info.Version), $"{relative}: Version required at publish time");
                    Assert.False(string.IsNullOrWhiteSpace(info.License), $"{relative}: License required at publish time");
                    Assert.False(string.IsNullOrWhiteSpace(info.MainFile), $"{relative}: MainFile required at publish time");
                    Assert.False(string.IsNullOrWhiteSpace(info.PricingModel), $"{relative}: PricingModel required at publish time");

                    // 2. The declared main grammar file must exist in the folder.
                    Assert.True(File.Exists(Path.Combine(grammarDir, info.MainFile)),
                        $"{relative}: MainFile {info.MainFile} missing from grammar folder");

                    // 3. The exporter must validate and package it (no manual edits).
                    using var package = await exporter.ExportGrammarPackageAsync(info);
                    Assert.True(package.Length > 0, $"{relative}: empty package");

                    // The tar stream stores entry names in clear text.
                    var bytes = await ReadAllAsync(package);
                    var text = Encoding.ASCII.GetString(bytes);
                    Assert.Contains("minotaur-metadata.json", text);
                    Assert.Contains("grammar.grammar", text);
                }
                catch (Exception ex)
                {
                    failures++;
                    _output.WriteLine($"PACKAGE FAIL {relative}: {ex.Message}");
                }
            }

            _output.WriteLine($"Packaged {metadataFiles.Length - failures}/{metadataFiles.Length} grammars cleanly.");
            Assert.True(failures == 0, $"{failures} grammar(s) failed to package from the unmodified checkout");
        }

        private static async Task<byte[]> ReadAllAsync(Stream stream)
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms);
            return ms.ToArray();
        }
    }
}

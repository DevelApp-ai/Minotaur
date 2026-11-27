using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace Minotaur.Labyrinth;

/// <summary>
/// The manifest of a binary Labyrinth rule pack (Minotaur issue #283, P1):
/// pack identity, the target grammar the pack was compiled against, and the
/// SHA-256 of the exact grammar content used at build time. The grammar hash
/// is the staleness guard — a pack that loads against a different grammar
/// silently changes matching semantics, so the loader refuses the mismatch.
/// </summary>
public sealed class LabyrinthRulePackManifest
{
    /// <summary>The binary format version this pack was written with.</summary>
    public const int CurrentFormatVersion = 1;

    /// <summary>Pack identity (from <c>pack build --id</c>, or the rules directory name).</summary>
    public string PackId { get; init; } = string.Empty;

    /// <summary>The target grammar directory name (e.g. <c>programming-languages/csharp10</c>).</summary>
    public string TargetGrammarName { get; init; } = string.Empty;

    /// <summary>Lowercase hex SHA-256 of the UTF-8 grammar content the pack was compiled against.</summary>
    public string GrammarSha256 { get; init; } = string.Empty;

    /// <summary>The number of rules in the pack.</summary>
    public int RuleCount { get; init; }

    /// <summary>UTC ticks when the pack was built; 0 when absent.</summary>
    public long CreatedUtcTicks { get; init; }
}

/// <summary>
/// The outcome of a binary pack build or load (issue #283): the compiled rules,
/// the manifest, and one error per problem when <see cref="IsValid"/> is false.
/// </summary>
public sealed class LabyrinthRulePackFileResult
{
    /// <summary>The compiled rules; empty when <see cref="IsValid"/> is false.</summary>
    public IReadOnlyList<CompiledLabyrinthRule> Rules { get; init; } = Array.Empty<CompiledLabyrinthRule>();

    /// <summary>The pack manifest; null when the pack could not be read.</summary>
    public LabyrinthRulePackManifest? Manifest { get; init; }

    /// <summary>One error per problem, in pack order.</summary>
    public IReadOnlyList<string> Errors { get; init; } = Array.Empty<string>();

    /// <summary>True when the pack was built/loaded completely.</summary>
    public bool IsValid => Errors.Count == 0;
}

/// <summary>
/// Builds and reads the binary Labyrinth rule pack format (issue #283, P1/P2).
/// <para>
/// The pack stores the <b>post-grammar-parse</b> rule set — every pattern
/// already parsed into its <see cref="LabyrinthPatternAst"/> mini-AST — so
/// loading a pack skips the YAML parse, the per-pattern StepLexer/StepParser
/// tokenization and the pattern mini-AST build entirely. Delegates are NOT
/// serialized (Expression trees have no built-in serialization on modern
/// .NET): they compile at first load and are cached in-process by the host
/// (<see cref="ScanHost"/>). The rule payload is gzip-compressed, so a pack
/// is also compact enough to ship as an update artifact.
/// </para>
/// <para>
/// File layout: 8-byte magic <c>MLBPK1\0\0</c>, int32 format version, int64
/// uncompressed payload length, then the gzip-compressed BinaryWriter payload
/// (manifest first, then rules). Values are ordinal UTF-8 strings,
/// length-prefixed as by <see cref="BinaryWriter"/>.
/// </para>
/// <para>
/// Round-trip limitation (v1): YAML <c>metadata:</c> values are serialized as
/// their invariant string form and read back as strings — nested maps/lists
/// do not survive a pack round trip.
/// </para>
/// </summary>
public static class LabyrinthRulePack
{
    private const string Magic = "MLBPK1\0\0";

    /// <summary>Lowercase hex SHA-256 of the UTF-8 encoding of <paramref name="grammarContent"/>.</summary>
    public static string GrammarSha256(string grammarContent)
    {
        ArgumentNullException.ThrowIfNull(grammarContent);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(grammarContent))).ToLowerInvariant();
    }

    /// <summary>
    /// Builds a binary pack from a directory of YAML rule files: loads and
    /// validates the directory, compiles every rule against the target grammar
    /// (patterns parse here, once, at build time), and writes the pack file.
    /// </summary>
    public static LabyrinthRulePackFileResult Build(
        string rulesDirectory, string grammarContent, string grammarName, string outputPath, string? packId = null)
    {
        ArgumentNullException.ThrowIfNull(rulesDirectory);
        ArgumentNullException.ThrowIfNull(grammarContent);
        ArgumentNullException.ThrowIfNull(outputPath);

        var loaded = new LabyrinthRulePackLoader().TryLoadDirectory(rulesDirectory);
        if (!loaded.IsValid)
        {
            return new LabyrinthRulePackFileResult { Errors = loaded.Errors };
        }

        List<CompiledLabyrinthRule> compiled;
        try
        {
            // Compile with ONE shared pattern parser: grammar load + pattern
            // tokenization happen once per pack build, not once per rule.
            using var parser = new LabyrinthPatternParser(grammarContent, grammarName);
            compiled = new List<CompiledLabyrinthRule>(loaded.RuleSet.Rules.Count);
            foreach (var rule in loaded.RuleSet.Rules)
            {
                compiled.Add(LabyrinthRuleCompiler.Compile(rule, parser));
            }
        }
        catch (LabyrinthRuleException ex)
        {
            return new LabyrinthRulePackFileResult { Errors = new[] { ex.Message } };
        }

        var manifest = new LabyrinthRulePackManifest
        {
            PackId = string.IsNullOrWhiteSpace(packId) ? new DirectoryInfo(rulesDirectory).Name : packId,
            TargetGrammarName = grammarName,
            GrammarSha256 = GrammarSha256(grammarContent),
            RuleCount = compiled.Count,
            CreatedUtcTicks = DateTime.UtcNow.Ticks,
        };

        Write(outputPath, manifest, compiled);
        return new LabyrinthRulePackFileResult { Rules = compiled, Manifest = manifest };
    }

    /// <summary>
    /// Reads a binary pack. When <paramref name="expectedTargetGrammarName"/>
    /// and/or <paramref name="expectedGrammarSha256"/> are given (the scan
    /// host always passes both), the pack is refused unless it was built for
    /// exactly that grammar content — the staleness guard of issue #283.
    /// </summary>
    public static LabyrinthRulePackFileResult TryRead(
        string packPath, string? expectedTargetGrammarName = null, string? expectedGrammarSha256 = null)
    {
        ArgumentNullException.ThrowIfNull(packPath);
        if (!File.Exists(packPath))
        {
            return new LabyrinthRulePackFileResult { Errors = new[] { $"Rule pack file does not exist: '{packPath}'." } };
        }

        // Header parsing reads raw bytes only (no BufferedStream/BinaryReader
        // in front of the file): a buffering reader could read ahead into the
        // gzip payload and desynchronize the decompressor.
        byte[] file;
        try
        {
            file = File.ReadAllBytes(packPath);
        }
        catch (IOException ex)
        {
            return new LabyrinthRulePackFileResult { Errors = new[] { $"Rule pack '{packPath}' could not be read: {ex.Message}" } };
        }

        const int headerLength = 20; // 8 magic + 4 version + 8 payload length
        if (file.Length < headerLength || !file.AsSpan(0, Magic.Length).SequenceEqual(Encoding.ASCII.GetBytes(Magic)))
        {
            return new LabyrinthRulePackFileResult { Errors = new[] { $"Rule pack '{packPath}' is not a Labyrinth rule pack (bad magic)." } };
        }

        try
        {
            var version = BitConverter.ToInt32(file, Magic.Length);
            if (version != LabyrinthRulePackManifest.CurrentFormatVersion)
            {
                return new LabyrinthRulePackFileResult
                {
                    Errors = new[] { $"Rule pack '{packPath}' uses unsupported format version {version} (expected {LabyrinthRulePackManifest.CurrentFormatVersion})." },
                };
            }

            var expectedLength = BitConverter.ToInt64(file, Magic.Length + sizeof(int));
            using var payload = new MemoryStream(file, headerLength, file.Length - headerLength);
            using var gzip = new GZipStream(payload, CompressionMode.Decompress);
            using var decompressed = new MemoryStream();
            gzip.CopyTo(decompressed);
            if (decompressed.Length != expectedLength)
            {
                return new LabyrinthRulePackFileResult
                {
                    Errors = new[] { $"Rule pack '{packPath}' is corrupt: payload length mismatch (header {expectedLength}, decompressed {decompressed.Length})." },
                };
            }

            decompressed.Position = 0;
            using var content = new BinaryReader(decompressed, Encoding.UTF8, leaveOpen: true);
            return ReadPack(content, expectedTargetGrammarName, expectedGrammarSha256);
        }
        catch (EndOfStreamException)
        {
            return new LabyrinthRulePackFileResult { Errors = new[] { $"Rule pack '{packPath}' is truncated." } };
        }
        catch (InvalidDataException ex)
        {
            return new LabyrinthRulePackFileResult { Errors = new[] { $"Rule pack '{packPath}' is invalid: {ex.Message}" } };
        }
        catch (LabyrinthRuleException ex)
        {
            return new LabyrinthRulePackFileResult { Errors = new[] { ex.Message } };
        }
    }

    private static LabyrinthRulePackFileResult ReadPack(
        BinaryReader content, string? expectedGrammarName, string? expectedGrammarHash)
    {
        var manifest = ReadManifest(content);

        var errors = new List<string>();
        if (!string.IsNullOrEmpty(expectedGrammarName)
            && !string.Equals(manifest.TargetGrammarName, expectedGrammarName, StringComparison.Ordinal))
        {
            errors.Add(
                $"Rule pack was built for target grammar '{manifest.TargetGrammarName}' but is used with '{expectedGrammarName}'.");
        }

        if (!string.IsNullOrEmpty(expectedGrammarHash)
            && !string.Equals(manifest.GrammarSha256, expectedGrammarHash, StringComparison.Ordinal))
        {
            errors.Add(
                $"Rule pack was built against grammar content {manifest.GrammarSha256} but the current grammar content hashes to {expectedGrammarHash}: rebuild the pack against the current grammar.");
        }

        if (errors.Count > 0)
        {
            return new LabyrinthRulePackFileResult { Manifest = manifest, Errors = errors };
        }

        var rules = new List<CompiledLabyrinthRule>(manifest.RuleCount);
        for (var i = 0; i < manifest.RuleCount; i++)
        {
            rules.Add(ReadCompiledRule(content));
        }

        return new LabyrinthRulePackFileResult { Rules = rules, Manifest = manifest };
    }

    private static void Write(string outputPath, LabyrinthRulePackManifest manifest, IReadOnlyList<CompiledLabyrinthRule> rules)
    {
        using var payload = new MemoryStream();
        using (var content = new BinaryWriter(payload, Encoding.UTF8, leaveOpen: true))
        {
            WriteManifest(content, manifest);
            foreach (var rule in rules)
            {
                WriteCompiledRule(content, rule);
            }

            content.Flush();
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(outputPath))!);
        using var output = File.Create(outputPath);
        using var writer = new BinaryWriter(output, Encoding.UTF8, leaveOpen: true);
        writer.Write(Encoding.ASCII.GetBytes(Magic));
        writer.Write(LabyrinthRulePackManifest.CurrentFormatVersion);
        writer.Write(payload.Length);
        writer.Flush();
        using var gzip = new GZipStream(output, CompressionLevel.Optimal, leaveOpen: true);
        payload.Position = 0;
        payload.CopyTo(gzip);
    }

    private static void WriteManifest(BinaryWriter w, LabyrinthRulePackManifest manifest)
    {
        w.Write(manifest.PackId);
        w.Write(manifest.TargetGrammarName);
        w.Write(manifest.GrammarSha256);
        w.Write(manifest.RuleCount);
        w.Write(manifest.CreatedUtcTicks);
    }

    private static LabyrinthRulePackManifest ReadManifest(BinaryReader r) => new()
    {
        PackId = r.ReadString(),
        TargetGrammarName = r.ReadString(),
        GrammarSha256 = r.ReadString(),
        RuleCount = r.ReadInt32(),
        CreatedUtcTicks = r.ReadInt64(),
    };

    private static void WriteString(BinaryWriter w, string? value)
    {
        w.Write(value ?? string.Empty);
    }

    private static string? ReadNullableString(BinaryReader r)
    {
        var value = r.ReadString();
        return value.Length == 0 ? null : value;
    }

    private static void WriteAst(BinaryWriter w, LabyrinthPatternAst ast)
    {
        w.Write(ast.Pattern);
        w.Write(ast.Nodes.Count);
        foreach (var node in ast.Nodes)
        {
            w.Write((byte)node.Kind);
            w.Write(node.TokenType);
            w.Write(node.Value);
            w.Write(node.StartPosition);
            w.Write(node.Length);
        }
    }

    private static LabyrinthPatternAst ReadAst(BinaryReader r)
    {
        var pattern = r.ReadString();
        var count = r.ReadInt32();
        var nodes = new LabyrinthPatternNode[count];
        for (var i = 0; i < count; i++)
        {
            var kind = (LabyrinthPatternNodeKind)r.ReadByte();
            nodes[i] = new LabyrinthPatternNode(kind, r.ReadString(), r.ReadString(), r.ReadInt32(), r.ReadInt32());
        }

        return new LabyrinthPatternAst(pattern, nodes);
    }

    private static void WriteAstList(BinaryWriter w, IReadOnlyList<LabyrinthPatternAst> asts)
    {
        w.Write(asts.Count);
        foreach (var ast in asts)
        {
            WriteAst(w, ast);
        }
    }

    private static IReadOnlyList<LabyrinthPatternAst> ReadAstList(BinaryReader r)
    {
        var count = r.ReadInt32();
        var asts = new LabyrinthPatternAst[count];
        for (var i = 0; i < count; i++)
        {
            asts[i] = ReadAst(r);
        }

        return asts;
    }

    private static void WriteRule(BinaryWriter w, LabyrinthRule rule)
    {
        WriteString(w, rule.Id);
        w.Write(rule.Severity);
        w.Write(rule.Message);
        w.Write(rule.Type);
        WriteString(w, rule.Fix);
        WriteString(w, rule.FocusMetavariable);
        WriteString(w, rule.Source);
        WriteString(w, rule.SourceUrl);
        WriteString(w, rule.License);
        WriteMetadata(w, rule.Metadata);
        WritePathFilter(w, rule.Paths);
    }

    private static LabyrinthRule ReadRule(BinaryReader r)
    {
        var rule = new LabyrinthRule
        {
            Id = r.ReadString(),
            Severity = r.ReadString(),
            Message = r.ReadString(),
            Type = r.ReadString(),
        };
        rule.Fix = ReadNullableString(r);
        rule.FocusMetavariable = ReadNullableString(r);
        rule.Source = ReadNullableString(r);
        rule.SourceUrl = ReadNullableString(r);
        rule.License = ReadNullableString(r);
        rule.Metadata = ReadMetadata(r);
        rule.Paths = ReadPathFilter(r);
        return rule;
    }

    private static void WriteMetadata(BinaryWriter w, Dictionary<string, object?>? metadata)
    {
        w.Write(metadata is null ? 0 : metadata.Count);
        if (metadata is null)
        {
            return;
        }

        foreach (var (key, value) in metadata)
        {
            w.Write(key);
            w.Write(value switch
            {
                null => string.Empty,
                bool b => b ? "true" : "false",
                _ => Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            });
        }
    }

    private static Dictionary<string, object?>? ReadMetadata(BinaryReader r)
    {
        var count = r.ReadInt32();
        if (count == 0)
        {
            return null;
        }

        var metadata = new Dictionary<string, object?>(count, StringComparer.Ordinal);
        for (var i = 0; i < count; i++)
        {
            metadata[r.ReadString()] = r.ReadString();
        }

        return metadata;
    }

    private static void WritePathFilter(BinaryWriter w, LabyrinthPathFilter? filter)
    {
        w.Write(filter is not null);
        if (filter is null)
        {
            return;
        }

        w.Write(filter.Include.Count);
        foreach (var glob in filter.Include)
        {
            w.Write(glob ?? string.Empty);
        }

        w.Write(filter.Exclude.Count);
        foreach (var glob in filter.Exclude)
        {
            w.Write(glob ?? string.Empty);
        }
    }

    private static LabyrinthPathFilter? ReadPathFilter(BinaryReader r)
    {
        if (!r.ReadBoolean())
        {
            return null;
        }

        var filter = new LabyrinthPathFilter();
        var include = r.ReadInt32();
        for (var i = 0; i < include; i++)
        {
            filter.Include.Add(r.ReadString());
        }

        var exclude = r.ReadInt32();
        for (var i = 0; i < exclude; i++)
        {
            filter.Exclude.Add(r.ReadString());
        }

        return filter;
    }

    private static void WriteCompiledRule(BinaryWriter w, CompiledLabyrinthRule compiled)
    {
        WriteRule(w, compiled.Rule);
        w.Write(compiled.SearchPattern is not null);
        if (compiled.SearchPattern is not null)
        {
            WriteAst(w, compiled.SearchPattern);
        }

        WriteString(w, compiled.SearchCondition);
        WriteAstList(w, compiled.SearchNotPatterns);
        WriteAstList(w, compiled.SearchInsidePatterns);
        WriteString(w, compiled.FocusMetavariable);
        WriteEntries(w, compiled.Sources);
        WriteEntries(w, compiled.Sinks);
        WriteEntries(w, compiled.Sanitizers);

        w.Write(compiled.Propagators.Count);
        foreach (var propagator in compiled.Propagators)
        {
            WriteAst(w, propagator.Pattern);
            w.Write(propagator.From);
            w.Write(propagator.To);
            WriteString(w, propagator.Condition);
        }

        w.Write(compiled.Metavariables.Count);
        foreach (var name in compiled.Metavariables)
        {
            w.Write(name);
        }

        w.Write(compiled.MetavariableOccurrences.Count);
        foreach (var (name, count) in compiled.MetavariableOccurrences)
        {
            w.Write(name);
            w.Write(count);
        }
    }

    private static CompiledLabyrinthRule ReadCompiledRule(BinaryReader r)
    {
        var rule = ReadRule(r);
        LabyrinthPatternAst? searchPattern = null;
        if (r.ReadBoolean())
        {
            searchPattern = ReadAst(r);
        }

        var searchCondition = ReadNullableString(r);
        var searchNotPatterns = ReadAstList(r);
        var searchInsidePatterns = ReadAstList(r);
        var focusMetavariable = ReadNullableString(r);
        var sources = ReadEntries(r);
        var sinks = ReadEntries(r);
        var sanitizers = ReadEntries(r);

        var propagatorCount = r.ReadInt32();
        var propagators = new List<CompiledLabyrinthPropagator>(propagatorCount);
        for (var i = 0; i < propagatorCount; i++)
        {
            var pattern = ReadAst(r);
            var from = r.ReadString();
            var to = r.ReadString();
            var condition = ReadNullableString(r);
            propagators.Add(new CompiledLabyrinthPropagator(pattern, from, to, condition));
        }

        var metavariableCount = r.ReadInt32();
        var metavariables = new List<string>(metavariableCount);
        for (var i = 0; i < metavariableCount; i++)
        {
            metavariables.Add(r.ReadString());
        }

        var occurrenceCount = r.ReadInt32();
        var occurrences = new Dictionary<string, int>(occurrenceCount, StringComparer.Ordinal);
        for (var i = 0; i < occurrenceCount; i++)
        {
            occurrences[r.ReadString()] = r.ReadInt32();
        }

        return new CompiledLabyrinthRule(
            rule, searchPattern, searchCondition, searchNotPatterns, searchInsidePatterns,
            focusMetavariable, sources, sinks, sanitizers, propagators, metavariables, occurrences);
    }

    private static void WriteEntries(BinaryWriter w, IReadOnlyList<CompiledLabyrinthPatternEntry> entries)
    {
        w.Write(entries.Count);
        foreach (var entry in entries)
        {
            w.Write(entry.Alternatives.Count);
            foreach (var alternative in entry.Alternatives)
            {
                WriteAst(w, alternative.Pattern);
                WriteString(w, alternative.Condition);
                WriteAstList(w, alternative.NotPatterns);
                WriteAstList(w, alternative.InsidePatterns);
            }

            WriteString(w, entry.Condition);
        }
    }

    private static IReadOnlyList<CompiledLabyrinthPatternEntry> ReadEntries(BinaryReader r)
    {
        var entryCount = r.ReadInt32();
        var entries = new List<CompiledLabyrinthPatternEntry>(entryCount);
        for (var i = 0; i < entryCount; i++)
        {
            var alternativeCount = r.ReadInt32();
            var alternatives = new List<CompiledLabyrinthAlternative>(alternativeCount);
            for (var k = 0; k < alternativeCount; k++)
            {
                var pattern = ReadAst(r);
                var condition = ReadNullableString(r);
                var nots = ReadAstList(r);
                var insides = ReadAstList(r);
                alternatives.Add(new CompiledLabyrinthAlternative(pattern, condition, nots, insides));
            }

            var entryCondition = ReadNullableString(r);
            entries.Add(new CompiledLabyrinthPatternEntry(alternatives, entryCondition));
        }

        return entries;
    }
}

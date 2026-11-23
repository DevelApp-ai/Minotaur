using Minotaur.Benchmarks.Labyrinth;
using Xunit;

namespace Minotaur.Tests.Labyrinth;

/// <summary>
/// Tests for the deterministic Labyrinth detection-corpus generator
/// (Minotaur issue #246, Track GSE L3). The corpus is the ground truth of the
/// measurement harness: these tests pin the properties the harness relies on:
/// determinism (no seed, byte-identical regeneration), exact ground-truth
/// lines within the emitted files, and template rotation coverage so the
/// corpus exercises every source/sanitizer/sink variant of each rule.
/// </summary>
public sealed class LabyrinthCorpusGeneratorTests
{
    [Fact]
    public void GenerationIsDeterministic()
    {
        var first = CorpusGenerator.Generate(7);
        var second = CorpusGenerator.Generate(7);

        Assert.Equal(first.Count, second.Count);
        foreach (var (a, b) in first.Zip(second))
        {
            Assert.Equal(a.RuleId, b.RuleId);
            Assert.Equal(a.Files.Count, b.Files.Count);
            foreach (var (fa, fb) in a.Files.Zip(b.Files))
            {
                Assert.Equal(fa.FileName, fb.FileName);
                Assert.Equal(fa.Content, fb.Content);
                Assert.Equal(fa.IsVulnerable, fb.IsVulnerable);
            }

            Assert.Equal(a.ExpectedFindings, b.ExpectedFindings);
            Assert.Equal(a.ExpectedLineCounts, b.ExpectedLineCounts);
        }
    }

    [Fact]
    public void EveryRuleHasMatchingPairCounts()
    {
        const int pairs = 5;
        var cases = CorpusGenerator.Generate(pairs);

        Assert.Equal(CorpusGenerator.RuleIds.Length, cases.Count);
        Assert.Equal(CorpusGenerator.RuleIds.OrderBy(i => i), cases.Select(c => c.RuleId).OrderBy(i => i));
        foreach (var kase in cases)
        {
            Assert.Equal(pairs, kase.Files.Count(f => f.IsVulnerable));
            Assert.Equal(pairs, kase.Files.Count(f => !f.IsVulnerable));
            // One expected finding per vulnerable program, none for safe ones.
            Assert.Equal(pairs, kase.ExpectedFindings.Count);
            Assert.All(kase.ExpectedFindings, e =>
                Assert.Matches(@"-v\d+\.cs$", e.File));
            Assert.All(kase.Files.Where(f => !f.IsVulnerable), f => Assert.DoesNotContain(
                (f.FileName, 0), kase.ExpectedFindings.Select(e => (e.File, e.Line))));
        }
    }

    [Fact]
    public void GroundTruthLinesAreInsideTheirFiles()
    {
        foreach (var kase in CorpusGenerator.Generate(4))
        {
            var byName = kase.Files.ToDictionary(f => f.FileName);
            foreach (var (file, line) in kase.ExpectedFindings)
            {
                Assert.Contains(file, byName);
                var content = byName[file].Content;
                var lineCount = content.Count(ch => ch == '\n');
                Assert.InRange(line, 1, lineCount);
                // The 1-based line in the ground truth is exactly the flagged source line.
                var lineText = content.Split('\n')[line - 1];
                Assert.True(lineText.Contains("return") || lineText.Contains("="),
                    $"expected line {line} of {file} should carry the violation: '{lineText.Trim()}'");
            }
        }
    }

    [Fact]
    public void PairCountRotationCoversEverySourceAndSinkVariant()
    {
        // Null-deref rotates 3 source constructors x 5 sinks x 3 safe guards
        // => full coverage within 15 pairs; as-cast rotates 4 cast types x 5
        // sinks x 3 safe guards => 15 pairs as well. Divide/modulo use two
        // safe families each.
        var cases = CorpusGenerator.Generate(15).ToDictionary(c => c.RuleId);

        var nullDeref = cases["csharp-null-deref-unguarded-maybe-null"];
        var vuln = string.Join("\n", nullDeref.Files.Where(f => f.IsVulnerable).Select(f => f.Content));
        Assert.Contains(".FirstOrDefault()", vuln);
        Assert.Contains(".SingleOrDefault()", vuln);
        Assert.Contains(".Find(null)", vuln);
        Assert.Contains("ToString()", vuln);
        Assert.Contains("Equals(null)", vuln);
        Assert.Contains("GetHashCode()", vuln);
        Assert.Contains("Clone()", vuln);
        Assert.Contains("Dispose()", vuln);
        var safeNull = string.Join("\n", nullDeref.Files.Where(f => !f.IsVulnerable).Select(f => f.Content));
        Assert.Contains("ArgumentNullException.ThrowIfNull(item);", safeNull);
        Assert.Contains("item = item ??", safeNull);
        Assert.Contains("item?.ToString();", safeNull);

        var asCast = cases["csharp-unsafe-as-cast-deref"];
        var vulnCast = string.Join("\n", asCast.Files.Where(f => f.IsVulnerable).Select(f => f.Content));
        Assert.Contains("value as string", vulnCast);
        Assert.Contains("value as System.IComparable", vulnCast);
        Assert.Contains("value as object", vulnCast);
        Assert.Contains("value as System.Text.StringBuilder", vulnCast);

        foreach (var ruleId in new[] { "csharp-divide-by-constant-zero", "csharp-modulo-by-constant-zero" })
        {
            var rule = cases[ruleId];
            var text = string.Join("\n", rule.Files.Select(f => f.Content));
            Assert.Contains(ruleId == "csharp-divide-by-constant-zero" ? "/ 0;" : "% 0;", text);
        }

        var arrayIndex = cases["csharp-array-index-unvalidated-input"];
        var safeArray = string.Join("\n", arrayIndex.Files.Where(f => !f.IsVulnerable).Select(f => f.Content));
        Assert.Contains("int.Parse(input)", safeArray);
        Assert.Contains("int.TryParse(input, out index2);", safeArray);
    }

    [Fact]
    public void VulnerableVariantsCarryNoSanitizer()
    {
        // If a vulnerable template ever grew a guard pattern, ground truth
        // would break (the rule no longer fires), so pin it here explicitly.
        foreach (var kase in CorpusGenerator.Generate(3))
        {
            foreach (var file in kase.Files.Where(f => f.IsVulnerable))
            {
                Assert.DoesNotContain("ArgumentNullException.ThrowIfNull", file.Content);
                Assert.DoesNotContain("item?.", file.Content);
            }
        }
    }

    [Fact]
    public void FileNamesAreUniqueAcrossTheCorpus()
    {
        var cases = CorpusGenerator.Generate(3);
        var names = cases.SelectMany(c => c.Files).Select(f => f.FileName).ToList();
        Assert.Equal(names.Count, names.Distinct().Count());
    }
}

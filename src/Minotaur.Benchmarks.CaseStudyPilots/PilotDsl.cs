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

// Issue #128 case-study pilot: configuration DSL, grammar side. The grammar
// (Grammars/ConfigurationDSL.grammar) turns each line into ONE anchored
// absorb token (engine discipline: unanchored classes that let skippable
// whitespace float stall the lexer); the document model is rebuilt from the
// resolved tokens by this walker. The hand side lives in
// HandWritten/HandConfigParser.cs; the spec, corpus and battery below are
// shared by both sides.

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Diagnostics;
using System.Text.Json.Nodes;
using DevelApp.StepParser;
using DevelApp.StepLexer;

public sealed class MinotaurConfigParser
{
    private readonly string grammarText;

    public MinotaurConfigParser(string grammarText)
    {
        this.grammarText = grammarText;
    }

    public (bool Valid, ConfigDoc Doc) Parse(string text)
    {
        var result = Pilot.TryParseDocument(this.grammarText, text);
        var doc = DslWalker.DocFromTokens(text, result?.ResolvedTokens);
        return (result is not null && doc.Errors.Length == 0, doc);
    }
}

internal static class DslWalker
{
    public static ConfigDoc DocFromTokens(string text, System.Collections.Generic.IReadOnlyList<StepToken>? tokens)
    {
        var environments = new List<ConfigEnv>();
        var includes = new List<string>();
        var errors = new List<HandParseError>();
        string? openName = null;
        var openSettings = new List<ConfigSetting>();
        var openBlock = false;

        if (tokens is null)
        {
            return new ConfigDoc([], [], [new HandParseError(1, 1, string.Empty, "parse failed")]);
        }

        foreach (var token in tokens)
        {
            var value = Pilot.Slice(text, token);
            switch (token.Type)
            {
                case "header":
                    if (openName is not null)
                    {
                        errors.Add(new HandParseError(0, 1, value, "environment block not closed before the next environment header"));
                        break;
                    }

                    openName = Pilot.HeaderName(value);
                    openSettings.Clear();
                    openBlock = true;
                    break;
                case "include_line":
                    includes.Add(Pilot.IncludePath(value));
                    break;
                case "set_line":
                    var eq = value.IndexOf('=');
                    var key = value[..eq];
                    var settingValue = value[(eq + 1)..];
                    if (string.IsNullOrEmpty(settingValue))
                    {
                        errors.Add(new HandParseError(0, eq + 2, value, "setting without a value"));
                        break;
                    }

                    if (!openBlock)
                    {
                        errors.Add(new HandParseError(0, 1, value, "setting outside an environment block"));
                        break;
                    }

                    openSettings.Add(new ConfigSetting(key, settingValue, ConfigModels.ClassifyValue(settingValue)));
                    break;
                case "rbrace":
                    if (openName is not null)
                    {
                        environments.Add(new ConfigEnv(openName, [.. openSettings]));
                        openName = null;
                    }

                    openBlock = false;
                    break;
                default:
                    // structural tokens (ws, nl, comment) never resolve.
                    break;
            }
        }

        if (openName is not null || openBlock)
        {
            errors.Add(new HandParseError(0, 1, string.Empty, "environment block not closed at end of document"));
        }

        return new ConfigDoc([.. environments], [.. includes], [.. errors]);
    }
}

/// <summary>Deterministic corpus + battery for the DSL pilot
/// (seed 1337).Documents: 2-4 environment blocks, 3-10 settings per block,
/// 30% comment headers, 15% include-bearing documents.</summary>
internal static class DslCorpus
{
    public const int Seed = 1337;

    public const int DocumentCount = 1000;

    private static readonly string[] Envs = ["dev", "staging", "prod"];

    private static readonly string[] Keys =
    [
        "retries", "timeout_ms", "host", "port", "feature_flags", "db_pool",
        "label", "group.name", "cache_ttl", "max_size", "base_url", "worker_count",
    ];

    private static readonly string[] Templates = ["${env.db_host}", "${svc.api_url}", "${cluster.secrets_path}", "${runtime.log_level}"];

    private static readonly string[] Words = ["fast", "shared", "primary", "canary", "strict", "batched"];

    private static readonly string[] Comments = [" provisioning profile", " incident 4428 hotfix section", " tuned for load test", " migrated from legacy"];

    public static string Document(int index)
    {
        var rnd = new Random(unchecked(Seed + index * 7919));
        var sb = new System.Text.StringBuilder();
        if (rnd.Next(100) < 15)
        {
            sb.AppendLine($"include \"shared/{rnd.Next(1, 9):D2}_base.cfg\"");
        }

        var blockCount = 2 + rnd.Next(3);
        for (var b = 0; b < blockCount; b++)
        {
            var env = Envs[rnd.Next(Envs.Length)];
            if (rnd.Next(100) < 30)
            {
                sb.AppendLine($"#{Comments[rnd.Next(Comments.Length)]}");
            }

            sb.AppendLine($"environment {env} {{");
            var settingCount = 3 + rnd.Next(8);
            for (var s = 0; s < settingCount; s++)
            {
                var key = Keys[rnd.Next(Keys.Length)];
                var kind = rnd.Next(3);
                var value = kind switch
                {
                    0 => rnd.Next(1, 60000).ToString(System.Globalization.CultureInfo.InvariantCulture),
                    1 => rnd.NextDouble() < 0.5
                        ? $"299{(char)rnd.Next(97, 123)}{rnd.Next(10)}"
                        : (rnd.Next(1000, 100000) / 1000.0).ToString("0.000", System.Globalization.CultureInfo.InvariantCulture),
                    _ => rnd.NextDouble() < 0.5
                        ? Templates[rnd.Next(Templates.Length)]
                        : Words[rnd.Next(Words.Length)],
                };
                sb.AppendLine($"  {key}={value}");
            }

            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public sealed record BatteryCase(string Id, string Text, bool ExpectedValid);

    public static BatteryCase[] Battery()
    {
        return
        [
            new("V1", """
                environment dev {
                  retries=5
                  timeout_ms=2500
                }

                """, true),
            new("V2", """
                environment dev {
                  host=worker-1
                }
                environment prod {
                  port=443
                }

                """, true),
            new("V3", """
                # provisioning profile
                environment staging {
                  label=canary
                }

                """, true),
            new("V4", """
                include "shared/01_base.cfg"
                environment dev {
                  db_pool=${env.db_host}
                }

                """, true),
            new("V5", """
                environment prod {
                  worker_count=12
                  base_url=${svc.api_url}
                  label=strict
                  cache_ttl=7.500
                }

                """, true),
            new("V6", """
                environment dev {
                  retries=3
                  # tuned for load test
                  timeout_ms=1500
                }

                """, true),
            new("V7", """
                environment dev {
                  feature_flags=fast
                }
                environment staging {
                  group.name=value
                }
                environment prod {
                  max_size=4096
                }

                """, true),
            new("V8", """
                environment prod {
                  retries=2
                  timeout_ms=900
                  port=8080
                  cache_ttl=300
                  label=primary
                  host=node-a
                }

                """, true),
            new("V9", """
                environment staging {
                  db_pool=${cluster.secrets_path}
                }

                """, true),
            new("V10", """
                environment dev {
                  scale=1.250
                  drift=0.100
                }
                environment prod {
                  scale=2.500
                }

                """, true),
            new("I1", """
                retries=5

                """, false),
            new("I2", """
                environment qa {
                  retries=5
                }

                """, false),
            new("I3", """
                environment dev {
                  retries=5

                """, false),
            new("I4", """
                environment dev {
                  retries
                }

                """, false),
            new("I5", """
                include shared/01_base.cfg

                """, false),
            new("I6", """
                }

                """, false),
            new("I7", """
                environment {
                  retries=5
                }

                """, false),
        ];
    }
}

public static class PilotDsl
{
    public static int Run(string outDir, string commit, out (double MinotaurMeanUs, double HandMeanUs, double Slowdown, int Docs, long Bytes) headline)
    {
        Console.WriteLine("== pilot: configuration DSL (issue #128) ==");
        var grammar = Pilot.LoadGrammar("ConfigurationDSL.grammar");
        var minotaur = new MinotaurConfigParser(grammar);
        var hand = new HandConfigParser();

        // ---- Correctness gate: 17-case battery (10 valid, 7 invalid).
        var batteryRows = new List<(string Case, bool Expected, bool Minotaur, bool Hand, bool ModelEqual, bool Pass)>();
        var batteryGreen = true;
        foreach (var testCase in DslCorpus.Battery())
        {
            var mc = minotaur.Parse(testCase.Text);
            var hc = hand.Parse(testCase.Text);
            var modelEqual = mc.Valid && hc.Valid && ConfigModels.Equal(mc.Doc, hc.Doc);
            var pass = mc.Valid == testCase.ExpectedValid && hc.Valid == testCase.ExpectedValid
                && (!testCase.ExpectedValid || modelEqual);
            batteryGreen &= pass;
            batteryRows.Add((testCase.Id, testCase.ExpectedValid, mc.Valid, hc.Valid, modelEqual, pass));
            Console.WriteLine($"  dsl {testCase.Id} expected={testCase.ExpectedValid} minotaur={mc.Valid} hand={hc.Valid} modelEqual={modelEqual} pass={pass}");
        }

        // ---- Deterministic corpus: 1000 documents, seed 1337.
        var docs = new string[DslCorpus.DocumentCount];
        long corpusBytes = 0;
        var largestDoc = 0;
        for (var i = 0; i < docs.Length; i++)
        {
            docs[i] = DslCorpus.Document(i);
            corpusBytes += System.Text.Encoding.UTF8.GetByteCount(docs[i]);
            largestDoc = Math.Max(largestDoc, System.Text.Encoding.UTF8.GetByteCount(docs[i]));
        }

        // Warmup both parse paths (JIT).
        for (var i = 0; i < 20; i++)
        {
            _ = minotaur.Parse(docs[i]);
            _ = hand.Parse(docs[i]);
        }

        // Latency: per document, fresh session per document (includes session
        // construction, the only supported multi-document mode, #127 finding).
        var minotaurSamples = new double[docs.Length];
        var handSamples = new double[docs.Length];
        var failures = 0;
        var mismatches = 0;

        var wall = Stopwatch.StartNew();
        for (var i = 0; i < docs.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            var mc = minotaur.Parse(docs[i]);
            sw.Stop();
            minotaurSamples[i] = sw.Elapsed.TotalMilliseconds * 1000;

            var hc = hand.Parse(docs[i]);
            handSamples[i] = 0; // measured in the second pass below

            if (!mc.Valid || !hc.Valid)
            {
                failures++;
            }
            else if (!ConfigModels.Equal(mc.Doc, hc.Doc))
            {
                mismatches++;
            }
        }

        wall.Stop();
        var minotaurThroughput = docs.Length / wall.Elapsed.TotalSeconds;

        // Hand-side measured on its own pass.
        wall.Restart();
        for (var i = 0; i < docs.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            var hc = hand.Parse(docs[i]);
            sw.Stop();
            handSamples[i] = sw.Elapsed.TotalMilliseconds * 1000;
        }

        wall.Stop();
        var handThroughput = docs.Length / wall.Elapsed.TotalSeconds;

        var minotaurMean = Mean(minotaurSamples);
        var handMean = Mean(handSamples);
        var minotaurP95 = Quantile(minotaurSamples, 0.95);
        var handP95 = Quantile(handSamples, 0.95);
        var slowdown = handThroughput / minotaurThroughput;

        Console.WriteLine($"  measured {docs.Length} docs ({corpusBytes} bytes), failures={failures}, mismatches={mismatches}");
        Console.WriteLine($"  minotaur mean {minotaurMean:F1}us vs hand {handMean:F1}us (x{slowdown:F1})");

        headline = (minotaurMean, handMean, slowdown, docs.Length, corpusBytes);

        if (!batteryGreen || failures != 0 || mismatches != 0)
        {
            return 1;
        }

        // ---- Artifacts.
        var batteryCsv = new List<string> { "case,expected,minotaur,hand,model_equal,pass" };
        batteryCsv.AddRange(batteryRows.Select(r => $"{r.Case},{r.Expected},{r.Minotaur},{r.Hand},{r.ModelEqual},{r.Pass}"));
        File.WriteAllLines(Path.Combine(outDir, "battery.csv"), batteryCsv);

        var json = new JsonObject
        {
            ["commit"] = commit,
            ["corpus"] = new JsonObject
            {
                ["documents"] = docs.Length,
                ["bytes"] = corpusBytes,
                ["largestDocBytes"] = largestDoc,
                ["seed"] = DslCorpus.Seed,
            },
            ["battery"] = new JsonObject
            {
                ["cases"] = batteryRows.Count,
                ["pass"] = batteryGreen,
            },
            ["minotaur"] = new JsonObject
            {
                ["meanUsPerDoc"] = Math.Round(minotaurMean, 1),
                ["p95UsPerDoc"] = Math.Round(minotaurP95, 1),
                ["docsPerSecond"] = Math.Round(minotaurThroughput, 0),
                ["parseFailures"] = failures,
                ["modelMismatches"] = mismatches,
            },
            ["hand"] = new JsonObject
            {
                ["meanUsPerDoc"] = Math.Round(handMean, 1),
                ["p95UsPerDoc"] = Math.Round(handP95, 1),
                ["docsPerSecond"] = Math.Round(handThroughput, 0),
            },
            ["slowdownFactor"] = Math.Round(slowdown, 1),
            ["timeline"] = TimeLogData.TimelineJson(),
        };
        File.WriteAllText(Path.Combine(outDir, "results.json"), json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(outDir, "RESULTS.md"), Program.DslResultsMd(commit, batteryRows, json));
        Console.WriteLine($"  wrote {outDir}/results.json and RESULTS.md");
        return 0;
    }

    internal static double Mean(double[] samples)
    {
        double sum = 0;
        foreach (var s in samples)
        {
            sum += s;
        }

        return samples.Length == 0 ? 0 : sum / samples.Length;
    }

    internal static double Quantile(double[] samples, double q)
    {
        if (samples.Length == 0)
        {
            return 0;
        }

        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        var index = (int)Math.Round(q * (sorted.Length - 1));
        return sorted[index];
    }
}

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

// Issue #132 case-study pilot (1/3): FIX 4.4, grammar vs hand scanner.
// One SOH-delimited field per token; message semantics (8=FIX.4.4 first,
// 9=<body length> second, 10=<checksum> last; checksum = sum of the message
// bytes preceding "10=" mod 256, zero-padded to 3 digits) are enforced
// identically on the resolved tokens and in the hand scanner. The corpus
// is a deterministic 100,000-message stream (seed 421337, 15% heartbeat,
// 35% NewOrderSingle, 35% ExecutionReport, 10% MarketDataSnapshot, 5% Logon).

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Diagnostics;
using System.Text;
using System.Text.Json.Nodes;
using DevelApp.StepParser;
using DevelApp.StepLexer;

public sealed class MinotaurFixParser
{
    private readonly string grammarText;

    public MinotaurFixParser(string grammarText)
    {
        this.grammarText = grammarText;
    }

    public ((int Tag, string Value)[]? Fields, int[]? StartPositions) ParseTokens(string message)
    {
        var result = Pilot.TryParseDocument(this.grammarText, message);
        if (result is null)
        {
            return (null, null);
        }

        var fieldTokens = result.ResolvedTokens.Where(t => t.Type == "field").ToArray();
        if (fieldTokens.Length < 3)
        {
            return (null, null);
        }

        var fields = new (int Tag, string Value)[fieldTokens.Length];
        var startPositions = new int[fieldTokens.Length];
        for (var i = 0; i < fieldTokens.Length; i++)
        {
            var raw = Pilot.Slice(message, fieldTokens[i]);
            if (raw.EndsWith('\u0001'))
            {
                raw = raw[..^1];
            }

            var eq = raw.IndexOf('=');
            if (eq <= 0 || !int.TryParse(raw[..eq], out var tag))
            {
                return (null, null);
            }

            fields[i] = (tag, raw);
            startPositions[i] = fieldTokens[i].StartPosition;
        }

        return (fields, startPositions);
    }
}

public static class FixModels
{
    public static bool Equal((int Tag, string Value)[] left, (int Tag, string Value)[]? right)
    {
        if (right is null || right.Length != left.Length)
        {
            return false;
        }

        for (var i = 0; i < left.Length; i++)
        {
            if (left[i].Tag != right[i].Tag || left[i].Value != right[i].Value)
            {
                return false;
            }
        }

        return true;
    }
}

public static class FixSemantics
{
    /// <summary>Message-level semantics over one SOH-delimited field list.
    /// startPositions is the byte (char) offset of each field's tag; the
    /// corpus is pure ASCII so chars are bytes.</summary>
    public static bool Valid(string message, int[] startPositions, (int Tag, string Value)[] fields)
    {
        if (fields.Length < 3 || startPositions.Length != fields.Length)
        {
            return false;
        }

        if (fields[0].Tag != 8 || fields[0].Value != "8=FIX.4.4")
        {
            return false;
        }

        if (fields[1].Tag != 9 || !int.TryParse(fields[1].Value["9=".Length..], out var bodyLength))
        {
            return false;
        }

        var last = fields[^1];
        if (last.Tag != 10)
        {
            return false;
        }

        var checksumText = last.Value["10=".Length..];
        if (checksumText.Length != 3 || !int.TryParse(checksumText, out var checksum) || checksum > 255)
        {
            return false;
        }

        // Body length: bytes after the SOH terminating the 9 field through
        // the SOH preceding the 10 field (inclusive on both ends).
        var startOf10 = startPositions[^1];
        var startOfBody = startPositions[2];
        if (startOf10 - startOfBody != bodyLength)
        {
            return false;
        }

        // Checksum: sum of the message bytes preceding "10=" mod 256.
        var sum = 0;
        for (var i = 0; i < startOf10; i++)
        {
            sum += message[i];
        }

        return sum % 256 == checksum;
    }
}

/// <summary>Hand-written incumbent: a character scanner splitting on SOH,
/// mirroring how FIX feeds are typically consumed in production.</summary>
public sealed class HandFixScanner
{
    public (bool Valid, (int Tag, string Value)[]? Fields) Parse(string message)
    {
        var segments = message.Split('\u0001');
        // every field must be SOH-terminated: a trailing segment means the
        // message did not end with SOH.
        if (segments[^1].Length != 0)
        {
            return (false, null);
        }

        var rawFields = segments[..^1];
        if (rawFields.Length < 3)
        {
            return (false, null);
        }

        var fields = new (int Tag, string Value)[rawFields.Length];
        var startPositions = new int[rawFields.Length];
        var offset = 0;
        for (var i = 0; i < rawFields.Length; i++)
        {
            var raw = rawFields[i];
            var eq = raw.IndexOf('=');
            if (eq <= 0 || !int.TryParse(raw[..eq], out var tag))
            {
                return (false, null);
            }

            fields[i] = (tag, raw);
            startPositions[i] = offset;
            offset += raw.Length + 1;
        }

        return (FixSemantics.Valid(message, startPositions, fields), fields);
    }
}

internal static class FixCorpus
{
    public const int Seed = 421337;

    public const int MessageCount = 100_000;

    private static readonly string[] Symbols = ["AAPL", "MSFT", "EURUSD", "BTCUSD", "SPX500"];

    public static string Message(int index)
    {
        var rnd = new Random(unchecked(Seed + index * 104729));
        var roll = rnd.Next(100);
        string body;
        if (roll < 15)
        {
            body = $"35=0";
        }
        else if (roll < 50)
        {
            body = $"35=D|11=CL{100000 + index:D7}|55={Symbols[rnd.Next(Symbols.Length)]}|54={(rnd.Next(2) == 0 ? "1" : "2")}|38={rnd.Next(1, 900)}|44={(rnd.Next(10000, 200000) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}";
        }
        else if (roll < 85)
        {
            body = $"35=8|37=EX{200000 + index:D7}|17={rnd.Next(100000, 999999)}|150={(rnd.Next(2) == 0 ? "0" : "2")}|39={(rnd.Next(2) == 0 ? "0" : "2")}|55={Symbols[rnd.Next(Symbols.Length)]}|38={rnd.Next(1, 500)}|151=0";
        }
        else if (roll < 95)
        {
            body = $"35=W|262=MD{300000 + index:D7}|55={Symbols[rnd.Next(Symbols.Length)]}|268=2|277={rnd.Next(100, 105)}|270={(rnd.Next(10000, 200000) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}|271={rnd.Next(10, 900)}|279=1|270={(rnd.Next(10000, 200000) / 100.0).ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)}|271={rnd.Next(10, 900)}";
        }
        else
        {
            body = $"35=A|98=0|108=30|141={(rnd.Next(2) == 0 ? "Y" : "N")}|78={rnd.Next(1, 32)}";
        }

        body = body.Replace('|', '\u0001');
        return WithEnvelope(body);
    }

    public static string WithEnvelope(string body)
    {
        // BodyLength (9) counts the bytes after the SOH terminating the 9
        // field through the SOH preceding the 10 field (inclusive), i.e. the
        // ASCII byte count of the body plus its terminating SOH.
        var prefix = $"8=FIX.4.4\u00019={Encoding.ASCII.GetByteCount(body) + 1}\u0001";
        var raw = prefix + body + "\u0001";
        var sum = 0;
        foreach (var ch in raw)
        {
            sum += ch;
        }

        return raw + $"10={(sum % 256):D3}\u0001";
    }

    public sealed record BatteryCase(string Id, string Text, bool ExpectedValid);

    public static BatteryCase[] Battery()
    {
        var heartbeat = WithEnvelope("35=0");
        var order = WithEnvelope("35=D|11=CL1000042|55=AAPL|54=1|38=100|44=150.25");
        var report = WithEnvelope("35=8|37=EX2000023|17=812345|150=2|39=2|55=MSFT|38=200|151=0");
        var replaceChecksum = string.Join("\u0001", order.Split('\u0001').Select(s => s.StartsWith("10=", StringComparison.Ordinal) ? "10=000" : s));
        var replaceLength = string.Join("\u0001", order.Split('\u0001').Select(s => s.StartsWith("9=", StringComparison.Ordinal) ? "9=1" : s));
        var noEnvelope = "35=A\u000198=0\u0001108=30\u0001";
        var dropChecksum = string.Join("\u0001", order.Split('\u0001').Where(s => s.Length > 0 && !s.StartsWith("10=", StringComparison.Ordinal))) + "\u0001";

        return
        [
            new("V1", heartbeat, true),
            new("V2", order, true),
            new("V3", report, true),
            new("I1", replaceChecksum, false),
            new("I2", replaceLength, false),
            new("I3", noEnvelope, false),
            new("I4", dropChecksum, false),
        ];
    }
}

public static class PilotFix
{
    public const int WarmupMessages = 200;

    public static int Run(string outDir, string commit, out (double MinotaurP50Us, double HandP50Us, double Slowdown, int Messages, long Bytes) headline)
    {
        Console.WriteLine();
        Console.WriteLine("== pilot: FIX 4.4 (issue #132) ==");
        var grammar = Pilot.LoadGrammar("FIX44.grammar");
        var minotaur = new MinotaurFixParser(grammar);
        var hand = new HandFixScanner();

        // ---- Correctness gate: 7-case battery.
        var batteryCsv = new List<string> { "case,expected,minotaur,hand,pass" };
        var batteryGreen = true;
        foreach (var testCase in FixCorpus.Battery())
        {
            var mc = minotaur.ParseTokens(testCase.Text);
            var hc = hand.Parse(testCase.Text);
            var minotaurValid = mc.Fields is not null && FixSemantics.Valid(testCase.Text, mc.StartPositions!, mc.Fields);
            var handValid = hc.Valid;
            var modelEqual = mc.Fields is not null && hc.Fields is not null && FixModels.Equal(mc.Fields, hc.Fields);
            var pass = minotaurValid == testCase.ExpectedValid && handValid == testCase.ExpectedValid
                && (!testCase.ExpectedValid || modelEqual);
            batteryGreen &= pass;
            batteryCsv.Add($"{testCase.Id},{testCase.ExpectedValid},{minotaurValid},{handValid},{pass}");
            Console.WriteLine($"  {testCase.Id} expected={testCase.ExpectedValid} minotaur={minotaurValid} hand={handValid} pass={pass}");
        }

        // ---- Deterministic corpus: 100,000 messages, seed 421337.
        var messages = new string[FixCorpus.MessageCount];
        long corpusBytes = 0;
        for (var i = 0; i < messages.Length; i++)
        {
            messages[i] = FixCorpus.Message(i);
            corpusBytes += Encoding.ASCII.GetByteCount(messages[i]);
        }

        // Warmup (JIT) then a full measured pass per side, single session
        // reused per system is NOT possible for Minotaur (per-buffer object,
        // fresh session per message), so the minotaur side pays session
        // construction per message, which is the engine's supported mode.
        var minotaurSamples = new double[messages.Length];
        var handSamples = new double[messages.Length];
        var invalidVerdicts = 0;

        for (var i = 0; i < WarmupMessages; i++)
        {
            _ = minotaur.ParseTokens(messages[i]);
        }

        var wall = Stopwatch.StartNew();
        for (var i = 0; i < messages.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            var mc = minotaur.ParseTokens(messages[i]);
            sw.Stop();
            minotaurSamples[i] = sw.Elapsed.TotalMilliseconds * 1000;
            if (mc.Fields is null || !FixSemantics.Valid(messages[i], mc.StartPositions!, mc.Fields))
            {
                invalidVerdicts++;
            }
        }

        wall.Stop();
        var minotaurThroughput = messages.Length / wall.Elapsed.TotalSeconds;

        for (var i = 0; i < WarmupMessages; i++)
        {
            _ = hand.Parse(messages[i]);
        }

        wall.Restart();
        for (var i = 0; i < messages.Length; i++)
        {
            var sw = Stopwatch.StartNew();
            var hc = hand.Parse(messages[i]);
            sw.Stop();
            handSamples[i] = sw.Elapsed.TotalMilliseconds * 1000;
            if (!hc.Valid)
            {
                invalidVerdicts++;
            }
        }

        wall.Stop();
        var handThroughput = messages.Length / wall.Elapsed.TotalSeconds;

        var minotaurP50 = PilotDsl.Quantile(minotaurSamples, 0.50);
        var minotaurP95 = PilotDsl.Quantile(minotaurSamples, 0.95);
        var minotaurP99 = PilotDsl.Quantile(minotaurSamples, 0.99);
        var handP50 = PilotDsl.Quantile(handSamples, 0.50);
        var handP95 = PilotDsl.Quantile(handSamples, 0.95);
        var handP99 = PilotDsl.Quantile(handSamples, 0.99);
        var slowdown = handThroughput / minotaurThroughput;

        Console.WriteLine($"  measured {messages.Length} messages ({corpusBytes} bytes), invalid={invalidVerdicts}");
        Console.WriteLine($"  minotaur p50={minotaurP50:F2}us hand p50={handP50:F2}us (x{slowdown:F1})");

        headline = (minotaurP50, handP50, slowdown, messages.Length, corpusBytes);

        if (!batteryGreen || invalidVerdicts != 0)
        {
            return 1;
        }

        // ---- Artifacts.
        File.WriteAllLines(Path.Combine(outDir, "fix-battery.csv"), batteryCsv);

        var json = new JsonObject
        {
            ["commit"] = commit,
            ["corpus"] = new JsonObject
            {
                ["messages"] = messages.Length,
                ["bytes"] = corpusBytes,
                ["seed"] = FixCorpus.Seed,
                ["mix"] = new JsonObject
                {
                    ["heartbeat"] = "15%",
                    ["newOrderSingle"] = "35%",
                    ["executionReport"] = "35%",
                    ["marketDataSnapshot"] = "10%",
                    ["logon"] = "5%",
                },
            },
            ["minotaur"] = new JsonObject
            {
                ["p50Us"] = Math.Round(minotaurP50, 2),
                ["p95Us"] = Math.Round(minotaurP95, 2),
                ["p99Us"] = Math.Round(minotaurP99, 2),
                ["messagesPerSecond"] = Math.Round(minotaurThroughput, 0),
            },
            ["hand"] = new JsonObject
            {
                ["p50Us"] = Math.Round(handP50, 2),
                ["p95Us"] = Math.Round(handP95, 2),
                ["p99Us"] = Math.Round(handP99, 2),
                ["messagesPerSecond"] = Math.Round(handThroughput, 0),
            },
            ["slowdownFactor"] = Math.Round(slowdown, 1),
            ["invalidVerdicts"] = invalidVerdicts,
        };
        File.WriteAllText(Path.Combine(outDir, "fix-results.json"), json.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        File.WriteAllText(Path.Combine(outDir, "fix-RESULTS.md"), Program.FixResultsMd(commit, batteryCsv, json));
        Console.WriteLine($"  wrote {outDir}/fix-results.json and fix-RESULTS.md");
        return 0;
    }
}

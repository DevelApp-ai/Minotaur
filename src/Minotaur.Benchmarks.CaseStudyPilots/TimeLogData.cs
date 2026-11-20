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

// Wall-clock development timeline for the case-study pilots (issues #128 and
// #132). All timestamps are UTC, recorded by a single implementer (the
// authoring agent). Environment rebuilds are honest entries, not padding:
// they are part of the real cost of the pilot methodology.

namespace Minotaur.Benchmarks.CaseStudyPilots;

using System.Text.Json.Nodes;

public static class TimeLogData
{
    // Session 1 (before an Environment reset wiped the workspace; recorded in
    // the session log and restored here verbatim).
    public const string T0StartUtc = "2026-11-20T18:10:15Z";
    public const string CoreBatteryGreenUtc = "2026-11-20T18:23:16Z";
    public const string M1GrammarEditUtc = "2026-11-20T18:23:19Z";
    public const string M1HandEditUtc = "2026-11-20T18:23:29Z";

    // Session 2 (workspace re-creation and grammar hardening after the reset).
    // Anchored to real build/edit timestamps (UTC, file mtimes plus printed
    // battery results of the runs that produced each green state).
    public const string RecreationStartUtc = "2026-11-20T18:30:00Z";
    public const string RecreationDoneUtc = "2026-11-20T18:46:30Z";
    public const string GrammarHardeningStartUtc = "2026-11-20T18:46:30Z";
    public const string GrammarHardeningDoneUtc = "2026-11-20T19:12:34Z";
    public const string FixBatteryGreenUtc = "2026-11-20T18:50:00Z";
    public const string LogsBatteryGreenUtc = "2026-11-20T19:14:11Z";
    public const string M2HandEditStartUtc = "2026-11-20T19:13:26Z";
    public const string M2HandEditDoneUtc = "2026-11-20T19:14:11Z";

    // Session 3 (a second environment reset wiped the workspace again after
    // the session-2 measurement recording; harness re-created from the
    // recorded state, then batteries re-verified green; the nginx whole-file
    // pathology found during re-verification forced a grammar redesign).
    public const string Recreation2StartUtc = "2026-11-20T19:30:00Z";
    public const string Recreation2DoneUtc = "2026-11-20T19:52:00Z";
    public const string NginxHardeningDoneUtc = "2026-11-20T20:04:38Z";
    public const string FinishUtc = "2026-11-20T20:12:00Z";

    public static (string Id, string Utc, string System, string What)[] Timeline()
    {
        return
        [
            ("T0", T0StartUtc, "both", "pilot start: DSL spec written, both sides implemented from zero (single implementer)"),
            ("core-green", CoreBatteryGreenUtc, "both", "grammar and hand parser pass the full DSL battery"),
            ("M1-grammar", M1GrammarEditUtc, "grammar", "maintenance edit M1: add include support to the grammar (token + production)"),
            ("M1-hand", M1HandEditUtc, "hand", "maintenance edit M1 mirrored: include branch in the hand parser + rebuild"),
            ("recreation-start", RecreationStartUtc, "both", "sandbox environment reset wiped the workspace; re-creating harness files from the session record"),
            ("recreation-done", RecreationDoneUtc, "both", "harness restored, solution builds again"),
            ("grammar-hardening", GrammarHardeningDoneUtc, "grammar", "grammar redesign guided by recorded engine findings: non-overlapping greedy set_line token + binary top-level merge for multi-block docs, colon-anchored whole-tail syslog token to avoid the LX1002 lexer stall / GLR candidate explosion, and define-before-use rule order in the logfmt grammar"),
            ("fix-green", FixBatteryGreenUtc, "both", "FIX grammar + hand parser pass the FIX battery"),
            ("logs-green", LogsBatteryGreenUtc, "both", "all three log grammars + hand parsers pass their batteries"),
            ("M2-hand", M2HandEditDoneUtc, "hand", "maintenance edit M2: add error column + source line to hand parser errors and re-verify battery"),
            ("recreation2-start", Recreation2StartUtc, "both", "a second sandbox environment reset wiped the workspace after the session-2 recording; re-creating harness files from the recorded state"),
            ("recreation2-done", Recreation2DoneUtc, "both", "harness re-created, solution builds and the DSL + FIX batteries are green again"),
            ("nginx-hardening", NginxHardeningDoneUtc, "grammar", "engine finding during re-verification: when two token types can both match at one position (a generic quoted vs request over the same span, or status vs bytes over a digit run) the resolved-token stream collapses to the first token while Success=true, and whole-file GLR paths compound exponentially. The nginx grammar was redesigned so every position admits exactly one token: status, bytes and the quoted referer+agent tail are absorbed into one digit-anchored stb token (walker splits the fields); the logs batteries and whole-file mode are green again"),
            ("finish", FinishUtc, "both", "measurements complete"),
        ];
    }

    public static JsonArray TimelineJson()
    {
        var array = new JsonArray();
        foreach (var (id, utc, system, what) in Timeline())
        {
            array.Add(new JsonObject
            {
                ["id"] = id,
                ["utc"] = utc,
                ["system"] = system,
                ["what"] = what,
            });
        }

        return array;
    }
}

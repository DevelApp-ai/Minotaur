# Issue #128 pilot: configuration DSL, grammar vs hand-written parser

Measured at `e8016927` with `RealTimeParserSession` (DevelApp.StepParser, the C# in-process
parser Minotaur ships) against a line-oriented regex hand parser typical of the incumbent
approach. Both systems implement the same 30-line DSL spec: `environment <dev|staging|prod> {`
blocks of `key=value` settings, top-level `include "path"` lines, `#` comments. Value kinds
(number, `${...}` template, string) are a semantic classification, identical in both systems.

## Correctness gate

A 17-case battery (10 valid, 7 invalid) must agree on validity and, where a model is
asserted, on the parsed document model. Both systems pass all 17 cases.

| case | expected | minotaur | hand | model equal |
| --- | --- | --- | --- | --- |
| V1 | valid | valid | valid | True |
| V2 | valid | valid | valid | True |
| V3 | valid | valid | valid | True |
| V4 | valid | valid | valid | True |
| V5 | valid | valid | valid | True |
| V6 | valid | valid | valid | True |
| V7 | valid | valid | valid | True |
| V8 | valid | valid | valid | True |
| V9 | valid | valid | valid | True |
| V10 | valid | valid | valid | True |
| I1 | invalid | invalid | invalid | n/a |
| I2 | invalid | invalid | invalid | n/a |
| I3 | invalid | invalid | invalid | n/a |
| I4 | invalid | invalid | invalid | n/a |
| I5 | invalid | invalid | invalid | n/a |
| I6 | invalid | invalid | invalid | n/a |
| I7 | invalid | invalid | invalid | n/a |

## Parse performance (deterministic corpus, 1000 documents, seed 1337)

Corpus shape: 2-4 environment blocks per document, 3-10 settings per block, 30% comment
headers, 15% include-bearing documents; per-doc semantics (models) compared as well.

| metric | minotaur (grammar) | hand (regex) |
| --- | --- | --- |
| mean latency per document | 7118.9 us | 20.6 us |
| p95 latency per document | 12056.6 us | 32.9 us |
| throughput | 140 docs/s | 48174 docs/s |
| slowdown factor | x345 | 1.0 (baseline) |

Corpus: 1000 documents, 470757 bytes, largest document 919 bytes.
Parse failures (grammar): 0. Model mismatches (grammar vs hand): 0.

## Development and maintenance timeline (single implementer)

| Id | UTC | System | Event |
| --- | --- | --- | --- |
| T0 | 2026-11-20T18:10:15Z | both | pilot start: DSL spec written, both sides implemented from zero (single implementer) |
| core-green | 2026-11-20T18:23:16Z | both | grammar and hand parser pass the full DSL battery |
| M1-grammar | 2026-11-20T18:23:19Z | grammar | maintenance edit M1: add include support to the grammar (token + production) |
| M1-hand | 2026-11-20T18:23:29Z | hand | maintenance edit M1 mirrored: include branch in the hand parser + rebuild |
| recreation-start | 2026-11-20T18:30:00Z | both | sandbox environment reset wiped the workspace; re-creating harness files from the session record |
| recreation-done | 2026-11-20T18:46:30Z | both | harness restored, solution builds again |
| grammar-hardening | 2026-11-20T19:12:34Z | grammar | grammar redesign guided by recorded engine findings: non-overlapping greedy set_line token + binary top-level merge for multi-block docs, colon-anchored whole-tail syslog token to avoid the LX1002 lexer stall / GLR candidate explosion, and define-before-use rule order in the logfmt grammar |
| fix-green | 2026-11-20T18:50:00Z | both | FIX grammar + hand parser pass the FIX battery |
| logs-green | 2026-11-20T19:14:11Z | both | all three log grammars + hand parsers pass their batteries |
| M2-hand | 2026-11-20T19:14:11Z | hand | maintenance edit M2: add error column + source line to hand parser errors and re-verify battery |
| recreation2-start | 2026-11-20T19:30:00Z | both | a second sandbox environment reset wiped the workspace after the session-2 recording; re-creating harness files from the recorded state |
| recreation2-done | 2026-11-20T19:52:00Z | both | harness re-created, solution builds and the DSL + FIX batteries are green again |
| nginx-hardening | 2026-11-20T20:04:38Z | grammar | engine finding during re-verification: when two token types can both match at one position (a generic quoted vs request over the same span, or status vs bytes over a digit run) the resolved-token stream collapses to the first token while Success=true, and whole-file GLR paths compound exponentially. The nginx grammar was redesigned so every position admits exactly one token: status, bytes and the quoted referer+agent tail are absorbed into one digit-anchored stb token (walker splits the fields); the logs batteries and whole-file mode are green again |
| finish | 2026-11-20T20:12:00Z | both | measurements complete |

Elapsed: implementation to first battery green
13.2 minutes for BOTH sides together
(grammar 13.1 min of it); the two maintenance edits M1 (add `include`)
took 0.1 and 0.2 minutes.

## Honest limitations

- Single implementer (the authoring agent), not a human team; the timeline is machine time and
  does not support claims about human developer-weeks.
- Sandbox environment resets mid-pilot forced re-creations of the harness (timeline shows the
  recovery windows); all runs use identical corpora and seeds.
- The grammar needed one hardening iteration (multi-block documents) guided by recorded
  engine-discipline findings; that iteration time is honestly part of the grammar side's cost.
- Semantic checks (value kinds, contexts) are layered identically over both systems, so the
  comparison measures the acquisition mechanism, not spec differences.


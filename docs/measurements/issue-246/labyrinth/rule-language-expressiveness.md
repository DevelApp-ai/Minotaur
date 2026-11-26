# Labyrinth rule-language expressiveness matrix (Track LAB, issue #246 / #263)

**Honest scope:** this is an *engine coverage map* — what the Labyrinth rule
language supports today, key by key, compared against the Semgrep rule schema
that `docs/SAST Rule Language Evaluation.md` §4.3 adopts as the usability
benchmark. It is **not** a measured cross-engine comparison; per the
labyrinth-sast-rules article, an empirical expressiveness/analysis-cost
comparison against Semgrep/CodeQL remains future work and will only be
claimed from reproducible runs (the CI measurement lane emits
`surface-report.json` per run — see `Minotaur.Benchmarks.Labyrinth`).

Engine-side measurements that already exist: per-rule feature usage, compiled
delegate counts and compile cost for the shipped `rules/csharp10` pack are
emitted as `surface-report.json` by every article-lane CI run into
`measurement-artifacts/issue-246` (harness wired in #265; primitive in #264).

## Feature matrix (Semgrep YAML rule keys → Labyrinth support)

| Semgrep feature | Labyrinth status | Notes / PR |
| --- | --- | --- |
| `pattern` (search rules) | ✅ supported | target-grammar snippet, compiled to a native delegate (#102) |
| metavariables `$X` + cross-pattern unification | ✅ supported | one unification scope per rule, reference equality (#102/#103) |
| ellipsis `...` | ✅ supported | any sibling sequence, incl. prefix/suffix forms |
| taint `sources` / `sinks` / `sanitizers` / `propagators` | ✅ supported | fixed-point DFG traversal, sanitizers prune, propagators transfer (#103) |
| `pattern-either` | ✅ supported | entry matches when ANY alternative matches (#256) |
| `pattern-not` | ✅ supported | entry fails when the filter matches the same node (#258) |
| `pattern-inside` | ✅ supported | requires a matching ancestor in the match-node tree (#258) |
| `condition` / `metavariable-condition` | ✅ supported (as `condition:`) | Dynamic LINQ on the matched node, per entry and per rule (#104) |
| `focus-metavariable` | ✅ supported | finding reported at the bound node (#259) |
| `paths:` include/exclude | ✅ supported | gitignore-flavored globs; exclude wins (#260) |
| `fix` | ✅ supported, console-only | interpolated `$NAME` snippet on the finding; SARIF `fixes` not yet emitted (#261) |
| `metadata` | ✅ supported | free-form map, surfaced into SARIF rule `properties` (#262) |
| `severity`, `message`, `id` | ✅ supported | drives SARIF level and result message |
| `languages:` | ⚠️ partial | one target grammar per scan (`--grammar` / target dir), not a per-rule `languages:` list |
| `pattern-not-inside` | ❌ not yet | pattern-not + pattern-inside compose manually |
| `pattern-regex` / `pattern-not-regex` | ❌ not yet | regex over node text; `condition:` covers field-level checks |
| `pattern-comby`, `metavariable-pattern`, `pattern-inside` nesting | ❌ not yet | |
| taint `pattern-sanitizers` by regex, `options:` | ❌ not yet | |

## Pattern shapes (v2, issue #247)

Call patterns with argument lists and ellipsis; member calls with receiver
binding `$X.Method(...)` (and `$X?.Method(...)` null-conditional marker);
index access `$ARR[$I]`; binary operator nodes (`op:divide`, `op:modulo`,
`op:as`, `op:coalesce`); assignment RHS extended to bare metavariables,
identifiers, member calls, index access and operators. Branch guards
(`if (x == null) return;`) are **not** materialized — documented limitation.

## How to refresh the measured side

```
# every article-lane CI run writes these into measurement-artifacts/issue-246:
#   results.json (incl. ruleSurface), surface-report.json, RESULTS.md, ground-truth.json
dotnet run -c Release --project src/Minotaur.Benchmarks.Labyrinth -- <outdir> --pairs 10 --reps 3
```

The matrix above changes only with engine PRs (each row cites its PR); the
measured numbers change per commit and live in the CI artifacts.

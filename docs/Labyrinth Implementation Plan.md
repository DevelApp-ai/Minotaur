# Labyrinth Implementation Plan

Status plan for the Labyrinth SAST rule engine (epic [#100]), tracking the
layered architecture from `docs/SAST Rule Language Evaluation.md` §5.
Every delivery lands as a pull request; this file is updated with each layer.

| Layer | Scope | Issue | Status |
| :--- | :--- | :--- | :--- |
| 1 — Rule schema & pattern parsing | Semgrep-style YAML schema, metavariable/ellipsis tokens, target-grammar pattern parsing via StepLexer/StepParser | #101 | Done |
| 2 — Expression-tree compilation | Pattern mini-ASTs compiled to native delegates (`Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>`) with delegate caching | #102 | Done |
| 3 — Taint traversal engine | Fixed-point worklist over the DFG: source identification, propagation, sanitizer pruning, propagator transfer, sink confirmation with full source→sink paths, pooled metavariable bindings, interprocedural flow via host-provided edges | #103 | Done |
| 4 — Dynamic LINQ conditions | `condition:` escape hatch on search rules and per source/sink/sanitizer/propagator entry, parsed via System.Linq.Dynamic.Core at initialization and appended to the pattern expression tree before compilation; malformed conditions fail fast with clear rule errors | #104 | Done |
| 5 — Reporting & CI/CD | Finding model consumers: console output, SARIF for GitHub code scanning, exit codes/baselines, rule-pack directory loading, rule-author guide | #105 | Done |

## Layer 3 design notes (#103)

- **Engine**: `Minotaur.Labyrinth.LabyrinthTaintEngine` consumes one compiled
  taint rule (`LabyrinthCompiledMatchers`, Layer 2) plus an
  `ILabyrinthDataFlowGraph` (nodes + DFG successors, materialized by the host
  from the CognitiveGraph — zero serialization). Interprocedural flow is
  supported by including interprocedural edges in the successor set.
- **Fixed point**: per node, one taint state per taint origin (source node);
  a state change (new origin or new metavariable binding) re-enqueues the
  node; the worklist drains to equilibrium, so loops and mutual recursion
  terminate without a caller-side iteration bound.
- **Metavariable unification**: bindings are carried along each taint path in
  a value-type `LabyrinthTaintBindings` struct with pooled backing arrays;
  matcher calls are seeded with the carried bindings, so a `$DATA` bound at a
  source only matches a sink consuming the very same node.
- **Paths**: stored as linked path steps (O(nodes) memory); the full
  source→sink path is materialized only when a finding is reported
  (`LabyrinthTaintFinding`), the shortest path winning per (source, sink).
- **GC pressure**: the hot loop allocates only path steps and pooled binding
  arrays; a 20,002-node chain traversal completes with bounded allocations
  (see `LabyrinthTaintEngineTests.LargeGraph_TerminatesPromptly_WithBoundedAllocations`).

## Layer 5 design notes (#105)

- **Console reporting**: `LabyrinthConsoleReporter` writes one block per
  finding (severity, rule id, sink location, message, indented source→sink
  path chain via `LabyrinthPathRenderer`) plus a summary line, and returns
  the process exit code directly.
- **SARIF**: `LabyrinthSarifWriter` emits a SARIF 2.1.0 log (System.Text.Json,
  no external dependency) with results, severity→level mapping
  (error/warning/note), sink locations, tool driver rule metadata, and the
  full data-flow path as a `codeFlow` threadFlow — ready for GitHub code
  scanning upload.
- **Exit codes**: `LabyrinthExitCodes` — `0` success, `1` findings,
  `2` configuration error (grep/codeql convention).
- **Baselines**: `LabyrinthBaseline` persists SHA-256 fingerprints
  (rule id + severity + source/sink identity incl. CognitiveGraph location
  via `ILabyrinthFindingLocation`); `Apply` partitions findings into
  reportable vs. baseline-suppressed so CI only fails on new findings.
- **Rule packs**: `LabyrinthRulePackLoader.TryLoadDirectory` collects
  validation errors *per rule* (prefixed with file and rule index) instead of
  failing fast; valid rules still load; duplicate rule ids across files are
  reported. `LoadDirectory` keeps the throwing contract with all errors
  aggregated.
- **Docs**: `docs/Labyrinth Rule Author Guide.md` — rule schema, metavariables,
  ellipsis, Dynamic LINQ conditions, loading, reporting, exit codes and
  baselines.

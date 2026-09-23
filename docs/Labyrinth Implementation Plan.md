# Labyrinth Implementation Plan

Status plan for the Labyrinth SAST rule engine (epic [#100]), tracking the
layered architecture from `docs/SAST Rule Language Evaluation.md` §5.
Every delivery lands as a pull request; this file is updated with each layer.

| Layer | Scope | Issue | Status |
| :--- | :--- | :--- | :--- |
| 1 — Rule schema & pattern parsing | Semgrep-style YAML schema, metavariable/ellipsis tokens, target-grammar pattern parsing via StepLexer/StepParser | #101 | Done |
| 2 — Expression-tree compilation | Pattern mini-ASTs compiled to native delegates (`Func<ILabyrinthMatchNode, LabyrinthMatchContext, bool>`) with delegate caching | #102 | Done |
| 3 — Taint traversal engine | Fixed-point worklist over the DFG: source identification, propagation, sanitizer pruning, propagator transfer, sink confirmation with full source→sink paths, pooled metavariable bindings, interprocedural flow via host-provided edges | #103 | Done |
| 4 — Dynamic LINQ conditions | `condition:` escape hatch on patterns via System.Linq.Dynamic.Core, merged into the compiled pipeline | #104 | Planned |
| 5 — Reporting & CI/CD | Finding model consumers: console output, SARIF for GitHub code scanning, exit codes/baselines, rule-pack directory loading, rule-author guide | #105 | Planned |

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

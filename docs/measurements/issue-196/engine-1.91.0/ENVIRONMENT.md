# Engine 1.91.0 re-measurement environment

Re-run of the Minotaur#196 live-editing latency harness on the dev container
after the engine upgrade 1.83.0 -> 1.91.0 and the greedy-mode wiring of
`LiveEditorSession` (see the PR).

- Host: disposable dev container (Debian 13 trixie), 1 CPU, not a fixed
  benchmark machine: absolute numbers are indicative; ratios within one
  seeded run are meaningful, cross-host comparisons are not.
- Runtime: .NET 8.0.34 (X64), Release, single process.
- Engine: DevelApp.StepLexer/StepParser 1.91.0 (NuGet.org), session
  `LexingMode = GreedyLongestMatch`.
- Harness: `Minotaur.Benchmarks.LiveEditing`, seed 196, 10 warm-up + 75
  measured edits, corpora 150/800/3500 lines x 4 edit types, unchanged.
- The committed `../results.json` + `../RESULTS.md` are the reference run
  on engine 1.83.0 (different host, exploratory mode, non-overlapping
  grammar so tokens are identical). The p50 speedups there (27.9x-87.2x,
  reuse 0.945-0.997) and this run's (24.4x-96.4x) agree within host
  noise; the ~2x speedup quoted on the Minotaur#196 thread belongs to
  the earlier 1.75.0 exploration, before the engine's windowed re-lex
  (#152/#153, first shipped in 1.83.0).
- Deviations vs the reference run to expect from the engine delta
  1.83.0 -> 1.91.0 (parse-level SPI default-off, ML-assist default-off):
  none at token level; the 22-test LiveEditing suite including the
  incremental-vs-fresh token equivalence over 200 seeded edits passes.

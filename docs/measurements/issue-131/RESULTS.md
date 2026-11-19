# Issue #131 — Grammar compilation time by rule-count bin (synthetic corpus)

Measured by `Minotaur.Benchmarks.GrammarCompilation`: compiling a
GrammarForge grammar into the engine's shipped `RealTimeParserSession`
(DevelApp.StepParser, the C#-process target Minotaur ships). Cold = first
construction in a fresh process (includes JIT); warm = median of 10
in-process re-compilations. Parser footprint = the live managed-heap delta
of the compiled session after a full collection (approximate).

Machine: Ubuntu 26.04.1 LTS

- Runtime: .NET 8.0.34 (X64, 4 CPUs)
- Engine: DevelApp.StepLexer `1.83.0.0` / DevelApp.StepParser `1.83.0.0`
- Grammars: DevelApp-ai/Minotaur-Grammars @ `main`
- Minotaur commit: `1cff9be6c9cad54607e19e3860435a9d3e9e7ca7`
- Harness: measured 10 warm constructs per class

| Grammar Size | Grammar | Rules | Tokens | Cold | Warm median | Parser footprint | Grammar file |
|-------------|---------|-------|--------|------|-------------|------------------|--------------|
| Bin 20 rules | `rules-020.grammar` | 24 | 22 | 16.4 ms | 0.2 ms | 111.6 KB | 0.7 KB |
| Bin 60 rules | `rules-060.grammar` | 64 | 62 | 17.3 ms | 0.5 ms | 134.2 KB | 1.8 KB |
| Bin 180 rules | `rules-180.grammar` | 184 | 182 | 19.1 ms | 1.7 ms | 201.2 KB | 5.2 KB |
| Bin 320 rules | `rules-320.grammar` | 324 | 322 | 22.1 ms | 3.9 ms | 287.6 KB | 9.3 KB |

Measured compilation times are recorded, never asserted: re-running the
harness regenerates these artifacts on the current machine. Compare against
the article's projected tables and replace the projections with these values.

Full per-class detail (rule split, keyword counts, all warm samples, smoke
parse results) is in `results.json` next to this file.

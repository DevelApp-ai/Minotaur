# Issue #126 — Grammar compilation performance by grammar size

Measured by `Minotaur.Benchmarks.GrammarCompilation`: compiling a
GrammarForge grammar into the engine's shipped `RealTimeParserSession`
(DevelApp.StepParser, the C#-process target Minotaur ships). Cold = first
construction in a fresh process (includes JIT); warm = median of 10
in-process re-compilations. Parser footprint = the live managed-heap delta
of the compiled session after a full collection (approximate).

Machine: Debian GNU/Linux 13 (trixie)

- Runtime: .NET 8.0.34 (X64, 1 CPUs)
- Engine: DevelApp.StepLexer `1.83.0.0` / DevelApp.StepParser `1.83.0.0`
- Grammars: DevelApp-ai/Minotaur-Grammars @ `85360f6ff733`
- Minotaur commit: `fe7cdda27162c39fbadca02976ef7677978a2710`
- Harness: measured 10 warm constructs per class

| Grammar Size | Grammar | Rules | Tokens | Cold | Warm median | Parser footprint | Grammar file |
|-------------|---------|-------|--------|------|-------------|------------------|--------------|
| Small DSL | `KDL.grammar` | 20 | 1 | 26.4 ms | 0.4 ms | 137.8 KB | 1.2 KB |
| Medium DSL | `SQL.grammar` | 54 | 6 | 25.0 ms | 1.4 ms | 184.7 KB | 4.7 KB |
| Large DSL | `C17.grammar` | 189 | 69 | 32.5 ms | 3.2 ms | 280.1 KB | 14.9 KB |
| Complex DSL | `CSharp10.grammar` | 647 | 177 | 47.6 ms | 15.7 ms | 846.7 KB | 48.8 KB |

All four rows use real grammars from the pinned Minotaur-Grammars corpus;
no synthetic grammar was needed. The measured compilation times are in the
milliseconds range, several orders of magnitude below the article's projected
targets; the article table must be updated to these measured values.

Full per-class detail (rule split, keyword counts, all warm samples, smoke
parse results) is in `results.json` next to this file.

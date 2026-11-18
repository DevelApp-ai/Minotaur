# Issue #127 — Generated-parser runtime vs hand-written and ANTLR parsers

Measured by `Minotaur.Benchmarks.ParserRuntime`: the engine's shipped
`RealTimeParserSession` (DevelApp.StepParser, the C#-process target Minotaur
ships, a fresh session per document: the session is a per-buffer object
with no reset API, and re-initializing one session across documents
accumulates retained memory without bound, so parsing N independent
documents requires N sessions and the per-document cost includes
session construction) against hand-written single-pass parsers and ANTLR-generated C#
parsers (Antlr4.Runtime.Standard 4.13.1, fresh lexer/stream/parser per
document). All engines parse identical deterministic corpora of bounded
documents (seed 127; 1K / 10K / 100K lines per size class). Latency p50/p95
are per-document parse times over 10 reps x up to 500 sampled documents;
throughput is one full-corpus pass. A document counts as parsed for the
Minotaur engine only when parsing succeeds AND the last resolved token ends
at the end of the document (full consumption; `Success=True` alone is not
accepted). Allocations are the managed bytes allocated per document over
the full pass.

Machine: Debian GNU/Linux 13 (trixie)

- Runtime: .NET 8.0.34 (X64, 1 CPUs)
- Minotaur commit: `18e0c8eb` (measurement working tree base)
- Corpora: seed 127; small + medium committed under `docs/corpus/issue-127`, large generated
  at runtime (SHA-256 in `results.json`)

## config DSL

| Engine | Small p50/p95 (µs) | Medium p50/p95 (µs) | Large p50/p95 (µs) | Throughput small/medium/large (MB/s) | Allocated/doc small/medium/large (bytes) | Full-parse rate |
|--------|--------------------|--------------------|-------------------|-------------------------------------|-----------------------------------------|-----------------|
| minotaur | 2167.4 / 3026.1 | 2359.6 / 2830.1 | 2535.1 / 3043.2 | 0.1 / 0.1 / 0.1 | 1241274 / 1256927 / 1248210 | 100.0 % |
| hand-written | 1.9 / 6.2 | 1.9 / 6.1 | 1.9 / 6.0 | 52.8 / 86.6 / 68.2 | 2560 / 2630 / 2697 | 100.0 % |
| antlr | 43.3 / 92.3 | 42.7 / 78.0 | 40.9 / 62.9 | 3.6 / 3.9 / 4.2 | 8777 / 8745 / 8760 | 100.0 % |

## query DSL

| Engine | Small p50/p95 (µs) | Medium p50/p95 (µs) | Large p50/p95 (µs) | Throughput small/medium/large (MB/s) | Allocated/doc small/medium/large (bytes) | Full-parse rate |
|--------|--------------------|--------------------|-------------------|-------------------------------------|-----------------------------------------|-----------------|
| minotaur | 456.4 / 592.7 | 461.4 / 632.9 | 469.7 / 631.8 | 0.2 / 0.1 / 0.3 | 336431 / 336430 / 336879 | 100.0 % |
| hand-written | 1.0 / 4.2 | 1.1 / 4.5 | 1.1 / 4.2 | 56.5 / 46.4 / 65.0 | 747 / 743 / 743 | 100.0 % |
| antlr | 17.9 / 35.2 | 17.2 / 21.5 | 17.7 / 21.0 | 4.4 / 4.8 / 5.2 | 4485 / 4480 / 4479 | 100.0 % |

## template DSL

| Engine | Small p50/p95 (µs) | Medium p50/p95 (µs) | Large p50/p95 (µs) | Throughput small/medium/large (MB/s) | Allocated/doc small/medium/large (bytes) | Full-parse rate |
|--------|--------------------|--------------------|-------------------|-------------------------------------|-----------------------------------------|-----------------|
| minotaur | 259.4 / 382.3 | 260.2 / 384.9 | 262.8 / 343.0 | 0.5 / 0.5 / 0.6 | 225681 / 225702 / 226304 | 100.0 % |
| hand-written | 0.6 / 0.7 | 0.5 / 0.8 | 0.6 / 0.9 | 246.1 / 148.0 / 263.6 | 8 / 1 / 0 | 100.0 % |
| antlr | 19.6 / 24.9 | 19.3 / 32.9 | 19.6 / 29.6 | 7.2 / 7.2 / 7.9 | 4912 / 4916 / 4918 | 100.0 % |

## Cold start (fresh process, includes JIT)

| DSL | Minotaur construction | Session footprint | Minotaur first parse | Hand-written first parse | ANTLR first parse |
|-----|-----------------------|-------------------|----------------------|--------------------------|------------------|
| config | 30.6 ms | 113.5 KB | 37.6 ms | 0.62 ms | 31.4 ms |
| query | 35.0 ms | 111.0 KB | 36.6 ms | 0.99 ms | 22.6 ms |
| template | 33.5 ms | 108.9 KB | 36.5 ms | 0.27 ms | 24.9 ms |

## Findings

The measurements above replace the article's projected runtime table
(config 2.3 / 4.7 / 2.8 ms, query 15.4 / 28.1 / 16.9 ms, template 8.7 /
19.3 / 9.2 ms for hand-written / ANTLR / Minotaur respectively); the
projected values must not be published. Two caveats keep the comparison
honest:

1. **Per-buffer session model.** The engine's session is a per-buffer
   object (live editing applies incremental edits in place); it has no
   reset or disposal API, and re-initializing one session across
   documents accumulates retained memory without bound. Every Minotaur
   measurement therefore constructs a fresh session per document and
   includes session construction in the per-document cost; this is the
   engine's supported model, not a benchmark artifact.
2. **Bounded-document operating mode.** The shipped engine explores a
   bounded GLR path budget per document, so the benchmark feeds every
   engine a stream of bounded, fixed-shape documents (the live-editing
   usage the engine is shipped for: one document per editor buffer). The
   baselines accept unbounded documents; the generated Minotaur grammars
   deliberately encode the bounded document skeleton (see the grammar
   discipline documented in `CaseStudyDsls.cs`). Throughput over a large
   corpus is therefore per-document parse cost multiplied out, which is
   exactly the number a live editor experiences per keystroke reparse.
3. **Per-document timestamping.** Latency samples bracket each document
   with two `Stopwatch` timestamps (~tens of nanoseconds); for the
   microsecond hand-written baselines this adds a small fixed overhead
   to every sample, identically for all engines.

Full per-cell detail (p50/p95/mean latency, throughput, allocations,
working set, elapsed wall time, full-parse rate, cold-start rows, corpus
SHA-256 hashes) is in `results.json` next to this file.

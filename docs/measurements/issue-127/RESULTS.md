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
- Engine: DevelApp.StepLexer `1.83.0.0` / DevelApp.StepParser `1.83.0.0` (the C#-process target measured here)
- Minotaur commit: `4057af288c2fe9bf32b7e4d213d209d2528ea88f`
- Corpora: seed 127; small + medium committed under `docs/corpus/issue-127`, large generated
  at runtime (SHA-256 in `results.json`)

## config DSL

| Engine | Small p50/p95 (µs) | Medium p50/p95 (µs) | Large p50/p95 (µs) | Throughput small/medium/large (MB/s) | Allocated/doc small/medium/large (bytes) | Full-parse rate |
|--------|--------------------|--------------------|-------------------|-------------------------------------|-----------------------------------------|-----------------|
| minotaur | 2959.8 / 3694.0 | 3412.3 / 3932.4 | 3393.6 / 3865.4 | 0.0 / 0.1 / 0.1 | 1258813 / 1259980 / 1245327 | 100.0 % |
| hand-written | 2.6 / 7.5 | 3.6 / 9.8 | 2.6 / 7.6 | 37.4 / 43.6 / 53.2 | 2560 / 2630 / 2697 | 100.0 % |
| antlr | 53.5 / 76.5 | 54.4 / 84.3 | 55.5 / 87.7 | 3.0 / 3.1 / 3.1 | 8777 / 8745 / 8760 | 100.0 % |

## query DSL

| Engine | Small p50/p95 (µs) | Medium p50/p95 (µs) | Large p50/p95 (µs) | Throughput small/medium/large (MB/s) | Allocated/doc small/medium/large (bytes) | Full-parse rate |
|--------|--------------------|--------------------|-------------------|-------------------------------------|-----------------------------------------|-----------------|
| minotaur | 622.7 / 769.6 | 627.7 / 802.1 | 624.6 / 846.5 | 0.1 / 0.1 / 0.2 | 336541 / 336541 / 336952 | 100.0 % |
| hand-written | 1.4 / 5.0 | 1.3 / 4.8 | 1.4 / 5.1 | 45.5 / 41.2 / 50.3 | 747 / 743 / 743 | 100.0 % |
| antlr | 22.4 / 39.8 | 22.4 / 27.6 | 22.4 / 27.6 | 3.4 / 3.6 / 3.5 | 4485 / 4480 / 4479 | 100.0 % |

## template DSL

| Engine | Small p50/p95 (µs) | Medium p50/p95 (µs) | Large p50/p95 (µs) | Throughput small/medium/large (MB/s) | Allocated/doc small/medium/large (bytes) | Full-parse rate |
|--------|--------------------|--------------------|-------------------|-------------------------------------|-----------------------------------------|-----------------|
| minotaur | 349.1 / 472.0 | 353.5 / 469.7 | 362.9 / 462.4 | 0.4 / 0.3 / 0.5 | 225791 / 225888 / 226128 | 100.0 % |
| hand-written | 0.7 / 0.9 | 0.8 / 1.0 | 0.7 / 1.1 | 181.7 / 113.8 / 222.8 | 8 / 1 / 0 | 100.0 % |
| antlr | 24.8 / 47.3 | 25.9 / 44.2 | 25.1 / 33.5 | 5.3 / 5.4 / 5.0 | 4912 / 4916 / 4918 | 100.0 % |

## Cold start (fresh process, includes JIT)

| DSL | Minotaur construction | Session footprint | Minotaur first parse | Hand-written first parse | ANTLR first parse |
|-----|-----------------------|-------------------|----------------------|--------------------------|------------------|
| config | 34.0 ms | 113.5 KB | 45.2 ms | 0.75 ms | 36.4 ms |
| query | 38.1 ms | 111.0 KB | 44.4 ms | 1.15 ms | 27.9 ms |
| template | 34.6 ms | 108.9 KB | 82.0 ms | 0.33 ms | 28.9 ms |

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
1. **Bounded-document operating mode.** The shipped engine explores a
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

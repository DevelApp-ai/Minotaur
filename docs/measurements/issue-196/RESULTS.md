# Live editing latency measurements (Minotaur issue #196)

Keystroke-to-token-update latency of `RealTimeParserSession.ApplyEdit` (incremental)
vs a full re-parse baseline (`RealTimeParserSession.Initialize`), measured by
`src/Minotaur.Benchmarks.LiveEditing` with seed 196, 10 warm-up edits and 75
measured edits per scenario. Identical edit streams are applied to both modes.

Raw per-edit microsecond samples: `results.json`.

| Corpus (lines) | Edit type | Inc p50 | Inc p95 | Inc p99 | Full p50 | Full p95 | Full p99 | p50 speedup | Mean reuse | Inc alloc (MB) | Full alloc (MB) | Inc CPU (ms) | Full CPU (ms) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 150 | single-char-insert | 3.79 ms | 52.44 ms | 59.07 ms | 5.22 ms | 54.93 ms | 59.62 ms | 1.4x | 0.990 | 122.12 | 212.87 | 270 | 390 |
| 150 | word-completion | 2.69 ms | 49.71 ms | 54.99 ms | 4.96 ms | 54.46 ms | 56.38 ms | 1.8x | 0.994 | 108.58 | 207.87 | 200 | 400 |
| 150 | line-insertion | 5.62 ms | 12.21 ms | 12.60 ms | 12.01 ms | 15.13 ms | 16.37 ms | 2.1x | 0.997 | 182.07 | 347.59 | 420 | 860 |
| 150 | block-paste | 10.13 ms | 28.73 ms | 40.22 ms | 25.72 ms | 35.71 ms | 79.42 ms | 2.5x | 0.998 | 367.94 | 778.72 | 880 | 1940 |
| 800 | single-char-insert | 15.32 ms | 26.52 ms | 38.66 ms | 22.19 ms | 49.01 ms | 62.73 ms | 1.4x | 0.999 | 565.12 | 1085.47 | 1080 | 1650 |
| 800 | word-completion | 4.54 ms | 7.89 ms | 9.46 ms | 7.33 ms | 18.86 ms | 19.72 ms | 1.6x | 0.992 | 619.77 | 1079.63 | 340 | 690 |
| 800 | line-insertion | 4.18 ms | 8.32 ms | 17.58 ms | 8.40 ms | 19.00 ms | 19.89 ms | 2.0x | 0.998 | 584.68 | 1216.30 | 330 | 700 |
| 800 | block-paste | 6.07 ms | 18.12 ms | 21.54 ms | 12.12 ms | 28.74 ms | 42.96 ms | 2.0x | 0.999 | 866.89 | 1644.59 | 570 | 1170 |
| 3500 | single-char-insert | 23.54 ms | 52.69 ms | 66.95 ms | 52.88 ms | 70.47 ms | 72.40 ms | 2.2x | 1.000 | 2439.67 | 4702.90 | 1960 | 3850 |
| 3500 | word-completion | 29.30 ms | 53.57 ms | 72.98 ms | 54.08 ms | 71.36 ms | 82.93 ms | 1.8x | 1.000 | 2563.21 | 4698.57 | 2080 | 3890 |
| 3500 | line-insertion | 15.53 ms | 59.75 ms | 66.11 ms | 54.73 ms | 73.14 ms | 75.02 ms | 3.5x | 1.000 | 2292.84 | 4835.02 | 1780 | 3990 |
| 3500 | block-paste | 28.04 ms | 59.48 ms | 68.67 ms | 58.26 ms | 77.21 ms | 79.04 ms | 2.1x | 1.000 | 2981.82 | 5259.33 | 2320 | 4320 |

Notes:

- Latency is the wall-clock time of the single `ApplyEdit`/`Initialize` call that
  produces the updated token stream (the session's own stopwatch).
- Allocation delta is `GC.GetTotalAllocatedBytes` over the measured window only;
  CPU is `Process.TotalProcessorTime` over the same window.
- Reuse ratio is the engine-reported share of tail tokens reused by `ApplyEdit`.
- Text is ASCII-only by design: the engine documents UTF-8 byte offsets, and
  `TextEditCalculator` enforces the ASCII assumption.


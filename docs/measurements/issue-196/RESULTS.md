# Live editing latency measurements (Minotaur issue #196)

Keystroke-to-token-update latency of `RealTimeParserSession.ApplyEdit` (incremental)
vs a full re-parse baseline (`RealTimeParserSession.Initialize`), measured by
`src/Minotaur.Benchmarks.LiveEditing` with seed 196, 10 warm-up edits and 75
measured edits per scenario. Identical edit streams are applied to both modes.

Raw per-edit microsecond samples: `results.json`.

Engine: DevelApp.StepLexer `1.83.0.0` / DevelApp.StepParser `1.83.0.0` (the C#-process target the session provides).

| Corpus (lines) | Edit type | Inc p50 | Inc p95 | Inc p99 | Full p50 | Full p95 | Full p99 | p50 speedup | Mean reuse | Inc alloc (MB) | Full alloc (MB) | Inc CPU (ms) | Full CPU (ms) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 150 | single-char-insert | 191.3 us | 247.2 us | 471.2 us | 5.34 ms | 9.64 ms | 13.84 ms | 27.9x | 0.949 | 8.21 | 212.88 | 10 | 430 |
| 150 | word-completion | 135.7 us | 186.1 us | 207.6 us | 5.01 ms | 5.49 ms | 7.40 ms | 36.9x | 0.945 | 7.78 | 207.87 | 10 | 380 |
| 150 | line-insertion | 221.0 us | 302.0 us | 2.61 ms | 8.24 ms | 10.62 ms | 14.72 ms | 37.3x | 0.971 | 13.91 | 347.60 | 20 | 630 |
| 150 | block-paste | 517.4 us | 700.3 us | 746.9 us | 18.37 ms | 38.07 ms | 39.96 ms | 35.5x | 0.978 | 33.19 | 778.73 | 40 | 1530 |
| 800 | single-char-insert | 424.7 us | 685.4 us | 946.7 us | 37.05 ms | 55.90 ms | 76.96 ms | 87.2x | 0.988 | 30.81 | 1085.46 | 30 | 2810 |
| 800 | word-completion | 492.9 us | 637.0 us | 761.9 us | 18.26 ms | 32.87 ms | 65.76 ms | 37.0x | 0.978 | 32.16 | 1079.64 | 40 | 1710 |
| 800 | line-insertion | 449.1 us | 694.3 us | 795.3 us | 17.10 ms | 54.75 ms | 59.47 ms | 38.1x | 0.985 | 35.21 | 1216.32 | 30 | 1350 |
| 800 | block-paste | 376.3 us | 703.3 us | 738.5 us | 12.37 ms | 28.80 ms | 33.55 ms | 32.9x | 0.988 | 57.68 | 1644.59 | 40 | 1080 |
| 3500 | single-char-insert | 1.03 ms | 1.69 ms | 7.99 ms | 52.91 ms | 73.60 ms | 86.21 ms | 51.5x | 0.997 | 126.56 | 4702.91 | 100 | 3920 |
| 3500 | word-completion | 1.02 ms | 3.08 ms | 4.89 ms | 52.42 ms | 69.67 ms | 86.10 ms | 51.1x | 0.997 | 130.53 | 4698.58 | 110 | 3900 |
| 3500 | line-insertion | 990.6 us | 1.93 ms | 8.60 ms | 59.12 ms | 80.84 ms | 90.32 ms | 59.7x | 0.997 | 125.63 | 4835.03 | 100 | 4330 |
| 3500 | block-paste | 1.38 ms | 4.28 ms | 8.02 ms | 61.63 ms | 92.58 ms | 106.98 ms | 44.7x | 0.997 | 161.56 | 5259.34 | 140 | 4720 |

Notes:

- Latency is the wall-clock time of the single `ApplyEdit`/`Initialize` call that
  produces the updated token stream (the session's own stopwatch).
- Allocation delta is `GC.GetTotalAllocatedBytes` over the measured window only;
  CPU is `Process.TotalProcessorTime` over the same window.
- Reuse ratio is the engine-reported share of tail tokens reused by `ApplyEdit`.
- Text is ASCII-only by design: the engine documents UTF-8 byte offsets, and
  `TextEditCalculator` enforces the ASCII assumption.


# Live editing latency measurements (Minotaur issue #196)

Keystroke-to-token-update latency of `RealTimeParserSession.ApplyEdit` (incremental)
vs a full re-parse baseline (`RealTimeParserSession.Initialize`), measured by
`src/Minotaur.Benchmarks.LiveEditing` with seed 196, 10 warm-up edits and 75
measured edits per scenario. Identical edit streams are applied to both modes.

Raw per-edit microsecond samples: `results.json`.

Engine: DevelApp.StepLexer `1.91.0.0` / DevelApp.StepParser `1.91.0.0` (the C#-process target the session provides).

| Corpus (lines) | Edit type | Inc p50 | Inc p95 | Inc p99 | Full p50 | Full p95 | Full p99 | p50 speedup | Mean reuse | Inc alloc (MB) | Full alloc (MB) | Inc CPU (ms) | Full CPU (ms) |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| 150 | single-char-insert | 193.7 us | 257.1 us | 435.1 us | 5.31 ms | 6.38 ms | 7.87 ms | 27.4x | 0.949 | 8.21 | 212.88 | 20 | 420 |
| 150 | word-completion | 130.8 us | 183.4 us | 207.1 us | 5.05 ms | 6.30 ms | 7.20 ms | 38.6x | 0.945 | 7.78 | 207.87 | 10 | 380 |
| 150 | line-insertion | 243.1 us | 317.5 us | 2.93 ms | 8.49 ms | 56.95 ms | 58.55 ms | 34.9x | 0.971 | 13.91 | 347.60 | 20 | 660 |
| 150 | block-paste | 670.9 us | 902.2 us | 976.5 us | 23.13 ms | 40.65 ms | 51.94 ms | 34.5x | 0.978 | 33.19 | 778.73 | 60 | 1830 |
| 800 | single-char-insert | 372.3 us | 503.7 us | 524.9 us | 35.90 ms | 53.97 ms | 83.24 ms | 96.4x | 0.988 | 30.81 | 1085.48 | 30 | 2810 |
| 800 | word-completion | 443.4 us | 640.9 us | 661.7 us | 10.90 ms | 22.47 ms | 37.22 ms | 24.6x | 0.978 | 32.16 | 1079.64 | 40 | 930 |
| 800 | line-insertion | 450.0 us | 723.9 us | 1.20 ms | 10.96 ms | 57.00 ms | 62.53 ms | 24.4x | 0.985 | 35.21 | 1216.31 | 40 | 1170 |
| 800 | block-paste | 378.1 us | 662.1 us | 3.71 ms | 12.31 ms | 32.38 ms | 37.98 ms | 32.6x | 0.988 | 57.68 | 1644.59 | 30 | 1150 |
| 3500 | single-char-insert | 997.3 us | 3.93 ms | 7.70 ms | 52.63 ms | 67.06 ms | 84.30 ms | 52.8x | 0.997 | 126.56 | 4702.92 | 100 | 3840 |
| 3500 | word-completion | 1.04 ms | 1.84 ms | 9.32 ms | 59.46 ms | 71.87 ms | 76.51 ms | 57.1x | 0.997 | 130.53 | 4698.59 | 110 | 4100 |
| 3500 | line-insertion | 931.9 us | 1.69 ms | 8.11 ms | 59.01 ms | 68.23 ms | 77.10 ms | 63.3x | 0.997 | 125.63 | 4835.04 | 100 | 4020 |
| 3500 | block-paste | 1.35 ms | 2.32 ms | 8.59 ms | 64.07 ms | 80.36 ms | 160.46 ms | 47.6x | 0.997 | 161.57 | 5259.35 | 120 | 4610 |

Notes:

- Latency is the wall-clock time of the single `ApplyEdit`/`Initialize` call that
  produces the updated token stream (the session's own stopwatch).
- Allocation delta is `GC.GetTotalAllocatedBytes` over the measured window only;
  CPU is `Process.TotalProcessorTime` over the same window.
- Reuse ratio is the engine-reported share of tail tokens reused by `ApplyEdit`.
- Text is ASCII-only by design: the engine documents UTF-8 byte offsets, and
  `TextEditCalculator` enforces the ASCII assumption.


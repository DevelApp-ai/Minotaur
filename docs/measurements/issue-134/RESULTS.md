# Issue #134 — Project-load time and memory by project scale

Measured by `Minotaur.Benchmarks.ProjectLoad`: synthetic mixed-language
project trees (deterministic content per file index; mix recorded per row)
loaded through `ProjectLoader.LoadFolderAsync`.

Machine: Ubuntu 26.04.1 LTS

- Runtime: .NET 8.0.34 (X64, 4 CPUs)
- Minotaur commit: `539585d31104f362698fd1be9e197b001b1e9008`
- Grammars dir: `/home/runner/work/Minotaur/Minotaur/grammars`

| Files | Discover (ms) | Classify (ms) | Total load median (ms) | Unclassified | Heap delta (MB) | WS delta (MB) |
|------:|--------------:|--------------:|-----------------------:|-------------:|----------------:|--------------:|
| 100 | 1 | 76 | 27 | 32/100 | 0.0 | 0.0 |
| 1000 | 1 | 360 | 379 | 332/1000 | 0.0 | 0.3 |
| 10000 | 8 | 1499 | 1492 | 3332/10000 | 3.4 | 4.5 |
| 50000 | 34 | 6934 | 6974 | 16666/50000 | 15.1 | 15.3 |

Notes:
- `discover` is `Directory.GetFiles` recursive (the call the loader makes);
  `classify` replays the loader's per-file `DetectGrammarAsync` calls (the
  loader exposes no phase timings); `total` is end-to-end `LoadFolderAsync`.
- Per-file symbol/dependency extraction in the shipped `ProjectLoader` is
  stubbed (`ExtractSymbolsFromGraph` returns empty), so the per-file parse
  phase currently contributes no work — the total curve measures
  discover + classify + assemble. CognitiveGraph V1/V2 threshold validation
  is tracked with CognitiveGraph (see #134 item 3) and is not measured here.

Full detail (file-type mix, all rep samples, allocation) is in `results.json`;
the curve is in `load-curve.csv`.

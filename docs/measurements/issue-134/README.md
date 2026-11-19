# Issue #134 measurement harness — project-load curves

Supports `articles/polyglot-code-analysis/content/main.md` (measurement
backlog: DevelApp-ai/Minotaur#134).

## What is measured

`Minotaur.Benchmarks.ProjectLoad` generates deterministic mixed-language
project trees (100 / 1K / 10K files by default; 50K with
`PROJECTLOAD_INCLUDE_50K=1`; override with `PROJECTLOAD_SCALES`) and measures
per scale:

- **discover** — recursive `Directory.GetFiles` (the call `ProjectLoader` makes),
- **classify** — per-file `GrammarDetectionManager.DetectGrammarAsync`, replayed
  by the harness because the loader exposes no phase timings,
- **total** — end-to-end `ProjectLoader.LoadFolderAsync` (median of reps),
- **memory** — managed-heap delta after full GC, `GC.GetAllocatedBytesForCurrentThread`,
  process working-set delta,
- **outcomes** — loaded file count, per-`FileType` mix, unclassified (no grammar)
  count, extracted symbol count.

Outputs: `results.json` + `load-curve.csv` + `RESULTS.md`. The
`article-measurements` job in `.github/workflows/ci-cd-enhanced.yml` runs it
on every PR/push (grammars checkout at `MINOTAUR_GRAMMARS_DIR`) and uploads the
`article-measurements` artifact.

## Findings the harness already documents

- The shipped `ProjectLoader.AnalyzeSourceFile` stubs per-file symbol and
  dependency extraction (`ExtractSymbolsFromGraph` returns an empty list), so
  the per-file **parse** phase currently contributes no work: the measured
  total curve is discover + classify + assemble. End-to-end load-time numbers
  including real per-file parsing require that stub to be implemented first.
- CognitiveGraph V1/V2 version-selection thresholds (#134 item 3) live in the
  CognitiveGraph package and are validated there, not here.

## How to run

```bash
MINOTAUR_GRAMMARS_DIR=/path/to/Minotaur-Grammars \
  dotnet run --project src/Minotaur.Benchmarks.ProjectLoad -- docs/measurements/issue-134
PROJECTLOAD_INCLUDE_50K=1 PROJECTLOAD_SCALES=100,1000,10000,50000 \
  dotnet run --project src/Minotaur.Benchmarks.ProjectLoad -- out-50k
```

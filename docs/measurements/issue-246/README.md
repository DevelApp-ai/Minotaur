# Issue 246 measurement artifacts (Track GSE L3)

L3 of the Track GSE epic (Minotaur #246) turns the Labyrinth csharp10 rule
pack (L1/L2) into measured engineering artifacts: detection performance
against an exact ground truth, per-file parse and scan cost over the shipped
scan path, and per-rule cost attribution.

Contents:

- `ground-truth.json`: the exact (rule, file, line) triples the corpus
  generator injects at the committed scale (10 vulnerable/safe pairs per
  rule, 5 rules, 100 programs, 50 expected findings). Deterministic, no
  seed; regenerating with the harness always yields the same triples.
- `RESULTS.md` + `results.json`: the committed reference measurement.
- `ENVIRONMENT.md`: hardware, OS, runtime and commit of the reference run.

## Harness

`src/Minotaur.Benchmarks.Labyrinth` generates the corpus deterministically
(no seed; byte-identical across machines), scans every program through the
production `minotaur labyrinth scan` path (`ScanHost.BuildGraph` +
`ScanHost.RunRules`, which resolves and compiles the target grammar once per
file, exactly as the shipped CLI does per file), and compares findings
against the ground truth. It exits non-zero when detection is below 100% or
any safe program reports a finding. Useful modes:

```
# committed-scale reference measurement (in CI: the article-measurements
# lane runs exactly this and posts RESULTS.md to the PR)
dotnet run -c Release --project src/Minotaur.Benchmarks.Labyrinth \
  -- <outdir> --pairs 10 --reps 3

# regenerate the committed ground truth without scanning
dotnet run -c Release --project src/Minotaur.Benchmarks.Labyrinth \
  -- <outdir> --pairs 10 --corpus-only

# fast single-rule smoke of the same protocol
dotnet run -c Release --project src/Minotaur.Benchmarks.Labyrinth \
  -- <outdir> --pairs 2 --reps 1 --only-rule csharp-null-deref-unguarded-maybe-null
```

The corpus programs themselves are not committed: `--corpus-only` (or
`--write-corpus` on a full run) recreates them byte-identically on demand.

Per-file cost note: the production scan path compiles the grammar per file,
so one parse+scan pair costs seconds per file on a single core; that cost is
part of what L3 measures (it is the real per-file cost of the shipped
`labyrinth scan`), and pairs 10 x reps 3 keeps a full run inside the CI
lane's budget.

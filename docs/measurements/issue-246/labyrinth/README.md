# Track LAB measurement artifacts (issue #246, sub-issue #263)

Engine-side expressiveness and analysis-cost artifacts for the
labyrinth-sast-rules article lane:

- `rule-language-expressiveness.md`: the Semgrep-vs-Labyrinth feature
  coverage matrix (engine coverage map, honestly scoped — not a measured
  cross-engine comparison) plus how to refresh the measured side.
- `surface-report.json`: committed reference of the per-rule rule-surface
  report (features used, entries/patterns/metavariables, compiled delegates,
  compile ms) for the shipped `rules/csharp10` pack — extracted from a green
  article-lane CI run (`measurement-artifacts/issue-246` artifact, harness
  `src/Minotaur.Benchmarks.Labyrinth`, wired in #265). Refreshed per commit
  by CI; the committed copy records the reference commit in its sibling
  `surface-report.commit.txt`.

Regenerate locally:

```
dotnet run -c Release --project src/Minotaur.Benchmarks.Labyrinth \
  -- <outdir> --pairs 10 --reps 3
# then copy <outdir>/surface-report.json here
```

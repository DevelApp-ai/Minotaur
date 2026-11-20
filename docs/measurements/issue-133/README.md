# Issue #133 measurement harness — extension-classification accuracy

Supports `articles/polyglot-code-analysis/content/main.md` (measurement
backlog: DevelApp-ai/Minotaur#133).

## What is measured

`Minotaur.Benchmarks.ClassificationAccuracy` downloads four pinned real-world
polyglot repositories (redis @ 8.0.2, curl @ 8.23.0, git @ 2.56.1, bootstrap @
5.3.9; tarballs cached under `CLASSACC_CACHE_DIR`), scores every file that has
an honest single-language adjudicated label, and measures top-1 classification
accuracy of three systems on the identical corpus:

- **minotaur-composite** — the default `GrammarDetectionManager()` pipeline as
  `ProjectLoader` uses it,
- **minotaur-extension** — the extension registry alone
  (`CompositeGrammarDetector(new[]{ new FileExtensionGrammarDetector() })`),
- **linguist-extension** — the extension map parsed from github-linguist
  `languages.yml` @ v9.7.0 (top-1 by first-claim order, plus the full
  candidate set).

Files are bucketed into the issue's categories (standard / ambiguous /
generated / container) per agreed ground-truth rules recorded in the harness
source; excluded files (binaries, VCS config, lockfiles, build templates, test
fixtures, extensionless) are never scored and are counted with reasons in
`results.json`. Besides top-1 accuracy the outputs add a **set-hit** metric
(the truth appearing anywhere in the system's candidate set) and an **error
taxonomy** (top unclassified extensions, top wrong pairs, composite-vs-extension
outcome deltas).

Outputs: `results.json` + `accuracy.csv` + `classification-detail.csv` (one row
per scored file) + `RESULTS.md`. The `article-measurements` job in
`.github/workflows/ci-cd-enhanced.yml` runs it on every PR/push, uploads the
`article-measurements` artifact and, on pull-request runs, posts the fresh
RESULTS.md + accuracy.csv as a PR comment.

Issue-item 5 (embedded-language detection at parse time) is explicitly out of
scope for this detection-phase harness.

## How to run

```bash
dotnet run --project src/Minotaur.Benchmarks.ClassificationAccuracy -- docs/measurements/issue-133
# corpus cache override (default: <tmp>/minotaur-classacc-corpus):
CLASSACC_CACHE_DIR=/path/to/cache dotnet run --project src/Minotaur.Benchmarks.ClassificationAccuracy -- out
```

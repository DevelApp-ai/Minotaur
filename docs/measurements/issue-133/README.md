# Issue #133 measurement harnesses — classification accuracy + embedded-language detection

Supports `articles/polyglot-code-analysis/content/main.md` (measurement
backlog: DevelApp-ai/Minotaur#133).

## Harness 1: extension-classification accuracy

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

## Harness 2 (issue-item 5): embedded-language detection at parse time

`Minotaur.Benchmarks.EmbeddedDetection` evaluates issue item 5 —
"Embedded-language detection (structural, at parse time) evaluated separately
on the polyglot-container subset" — on the same pinned corpus, restricted to
the container extensions `.html`/`.htm`/`.xhtml`/`.md`/`.markdown`/
`.mdx`/`.astro`.

Detection is scoped at region level: a detection is credited when
grammar-driven structural parsing recognizes an embedded region (script/style
block, code fence) as a coherent structural span, regardless of the inner
tokenization. Truth is adjudicated by markers, with exclusions recorded
(external scripts, empty blocks, template/html/text typed scripts, vbscript).
Because the engine cannot always finish an adversarial parse inside one
process, every parse runs in a child worker process with a request/ack/result
line protocol and is killed after the per-file budget
(`EMBEDDED_PER_FILE_TIMEOUT_MS`, default 1000 ms); outcomes are classified
honestly (`success | parse-fail | timeout | parse-crash | startup-fail |
engine-oom | engine-error | no-grammar | read-fail`).

Headline findings (run at main `4b07a7bb`, 1222 container files; see
`embedded-RESULTS.md` for detail):

- **Zero region-level detections.** Whole-file structural parsing almost never
  completes on real container files: 985/987 markdown files time out in the
  GLR blowup (probe: a 25-line/409-byte file needs 41–43 s and then fails with
  `PS2003: OutOfMemoryException`), all 19 HTML files end in worker process
  death, and `.mdx`/`.astro` (216 files) have no shipped grammar at all.
  Only 1 of 987 markdown files (a 279-byte callout without fences) parses
  successfully.
- **The skeleton finding.** `HTMLEmbedded.grammar` references
  `<script-content>`/`<style-content>` productions with empty bodies; the
  engine rejects them at load (`Rejected zero-width token rule ... (issue
  #111)`), so the container grammar ships without the embedded-language
  sub-grammars it implies — script/style delegation is skeleton-only.
- **Registry mapping gaps.** `.md`/`.markdown` have no mapping:
  `Markdown.grammar` exists under `specialized/markdown/` but is not
  referenced by `grammar-index.json` (the harness loads it by direct path and
  records the miss); `.xhtml` has no mapping (harness-assigned to
  `HTMLEmbedded.grammar`); `.mdx`/`.astro` have no shipped grammar of any
  kind. The tagged-fence inventory audit lists every fence info-tag language
  found and whether a monorepo grammar is shipped (e.g. `scss`, `diff`,
  `plaintext` fences have none).

Outputs: `embedded-results.json` + `embedded-files.csv` (one row per container
file) + `embedded-RESULTS.md`, with `embedded-partial-cache.jsonl` as an
append-mode resume cache so interrupted runs can continue. The
`article-measurements` job builds and runs this harness too (issue-133-embedded
artifact).

## How to run

```bash
dotnet run --project src/Minotaur.Benchmarks.ClassificationAccuracy -- docs/measurements/issue-133
# corpus cache override (default: <tmp>/minotaur-classacc-corpus):
CLASSACC_CACHE_DIR=/path/to/cache dotnet run --project src/Minotaur.Benchmarks.ClassificationAccuracy -- out

# item 5 (requires a Minotaur-Grammars checkout):
MINOTAUR_GRAMMARS_DIR=/path/to/Minotaur-Grammars \
CLASSACC_CACHE_DIR=/path/to/cache \
dotnet run --project src/Minotaur.Benchmarks.EmbeddedDetection -- docs/measurements/issue-133
```

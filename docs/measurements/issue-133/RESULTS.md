# Issue #133 — extension-based classification accuracy on real-world polyglot repositories

Measured 8136 scored files (plus 3404 excluded files, excluded only with an explicit reason recorded in results.json).

## Corpus

| repo | pin |
|---|---|
| redis | redis/redis @ 8.0.2 (1681 files) |
| curl | curl/curl @ curl-8_23_0 (4581 files) |
| git | git/git @ v2.56.1 (4858 files) |
| bootstrap | twbs/bootstrap @ v5.3.9 (796 files) |

Linguist baseline data: [languages.yml @ v9.7.0](https://raw.githubusercontent.com/github-linguist/linguist/v9.7.0/lib/linguist/languages.yml), extension map only.

## Ground truth

Per-file language labels adjudicated against this exact corpus (labels use
Linguist language names so both systems share the label space). Extension rules
are recorded in the harness source (`src/Minotaur.Benchmarks.ClassificationAccuracy/Program.cs`);
adjudicated rulings on the genuinely ambiguous cases:

- `.h` — every `.h` header across the four repos is a C header (redis, curl, git, and no C++-only headers sampled): truth `C`, category `ambiguous`.
- `.ts` — all bootstrap `.ts` files are TypeScript source: truth `TypeScript`, category `ambiguous`.
- `.inc` — curl's `Makefile.inc` fragments are Makefile: truth `Makefile`, category `ambiguous`.
- `.map` — source maps under `js/dist/`: truth `JSON`, category `generated`.
- Excluded (never scored): binaries/images, VCS/tool config, lockfiles, build templates (`.in`), test fixtures (`.expect`, `.test`, `.side`, `.patch`, `.diff`), extensionless files, and extensions without an honest single-language label. Full counts in `results.json`.

## Accuracy

Outcome per (system, file): top-1 prediction equals the adjudicated truth (`correct`),
prediction exists but differs (`wrong`), or the system has no mapping entry at
all (`unclassified`; counted against accuracy and coverage).

Set-hit accuracy additionally counts the ground-truth language appearing anywhere
among the system's candidate set (fallback grammars for the Minotaur systems, all
claiming languages for Linguist) even when top-1 differs.

### Category: standard

| system | n | correct | wrong | unclassified | top-1 accuracy | set-hit accuracy | answered accuracy | coverage |
|---|---|---|---|---|---|---|---|---|
| minotaur-composite | 5932 | 2478 | 673 | 2781 | 41.8 % | 41.8 % | 78.6 % | 53.1 % |
| minotaur-extension | 5932 | 2588 | 122 | 3222 | 43.6 % | 43.6 % | 95.5 % | 45.7 % |
| linguist-extension | 5932 | 5663 | 205 | 64 | 95.5 % | 98.8 % | 96.5 % | 98.9 % |

### Category: ambiguous

| system | n | correct | wrong | unclassified | top-1 accuracy | set-hit accuracy | answered accuracy | coverage |
|---|---|---|---|---|---|---|---|---|
| minotaur-composite | 923 | 745 | 166 | 12 | 80.7 % | 80.7 % | 81.8 % | 98.7 % |
| minotaur-extension | 923 | 911 | 0 | 12 | 98.7 % | 98.7 % | 100.0 % | 98.7 % |
| linguist-extension | 923 | 911 | 12 | 0 | 98.7 % | 98.7 % | 98.7 % | 100.0 % |

### Category: generated

| system | n | correct | wrong | unclassified | top-1 accuracy | set-hit accuracy | answered accuracy | coverage |
|---|---|---|---|---|---|---|---|---|
| minotaur-composite | 59 | 56 | 3 | 0 | 94.9 % | 94.9 % | 94.9 % | 100.0 % |
| minotaur-extension | 59 | 11 | 0 | 48 | 18.6 % | 18.6 % | 100.0 % | 18.6 % |
| linguist-extension | 59 | 11 | 0 | 48 | 18.6 % | 18.6 % | 100.0 % | 18.6 % |

### Category: container

| system | n | correct | wrong | unclassified | top-1 accuracy | set-hit accuracy | answered accuracy | coverage |
|---|---|---|---|---|---|---|---|---|
| minotaur-composite | 1222 | 19 | 708 | 495 | 1.6 % | 1.6 % | 2.6 % | 59.5 % |
| minotaur-extension | 1222 | 19 | 0 | 1203 | 1.6 % | 1.6 % | 100.0 % | 1.6 % |
| linguist-extension | 1222 | 217 | 1005 | 0 | 17.8 % | 100.0 % | 17.8 % | 100.0 % |

### Category: all

| system | n | correct | wrong | unclassified | top-1 accuracy | set-hit accuracy | answered accuracy | coverage |
|---|---|---|---|---|---|---|---|---|
| minotaur-composite | 8136 | 3298 | 1550 | 3288 | 40.5 % | 40.5 % | 68.0 % | 59.6 % |
| minotaur-extension | 8136 | 3529 | 122 | 4485 | 43.4 % | 43.4 % | 96.7 % | 44.9 % |
| linguist-extension | 8136 | 6802 | 1222 | 112 | 83.6 % | 98.4 % | 84.8 % | 98.6 % |

## Error taxonomy (top entries)

### minotaur-extension-unclassifiedByExtension

| key | count |
|---|---|
| .sh | 1403 |
| .md | 986 |
| .adoc | 953 |
| .tcl | 255 |
| .mdx | 110 |
| .astro | 106 |
| .yml | 89 |
| .pl | 80 |
| .txt | 70 |
| .po | 59 |
| .map | 48 |
| .build | 38 |
| .pm | 35 |
| .cmake | 30 |
| .perl | 27 |

### minotaur-extension-wrongPairs

| key | count |
|---|---|
| SCSS -> CSS | 122 |

### minotaur-composite-unclassifiedByExtension

| key | count |
|---|---|
| .sh | 1382 |
| .adoc | 941 |
| .md | 458 |
| .yml | 88 |
| .txt | 70 |
| .po | 59 |
| .build | 38 |
| .cmake | 29 |
| .mdx | 27 |
| .am | 22 |
| .cocci | 20 |
| .lua | 20 |
| .com | 19 |
| .m4 | 19 |
| .inc | 12 |

### minotaur-composite-wrongPairs

| key | count |
|---|---|
| Markdown -> C++ | 513 |
| Tcl -> CSS | 218 |
| SCSS -> CSS | 122 |
| C -> C++ | 121 |
| Perl -> Rust | 96 |
| JavaScript -> Python | 55 |
| MDX -> HTML | 49 |
| Python -> C# | 40 |
| Perl -> Go | 38 |
| Astro -> Python | 34 |
| MDX -> CSS | 33 |
| Astro -> TypeScript | 27 |
| C -> CSS | 27 |
| Tcl -> C# | 19 |
| Astro -> CSS | 18 |

### minotaur-composite-set-vs-extension

| key | count |
|---|---|
| composite=wrong extension=unclassified | 1139 |
| composite=wrong extension=correct | 289 |
| composite=correct extension=unclassified | 58 |

### linguist-extension-unclassifiedByExtension

| key | count |
|---|---|
| .map | 48 |
| .build | 38 |
| .am | 22 |
| .ac | 3 |
| .manifest | 1 |

### linguist-extension-wrongPairs

| key | count |
|---|---|
| Markdown -> GCC Machine Description | 986 |
| YAML -> MiniYAML | 93 |
| Text -> Adblock Filter List | 70 |
| M4Sugar -> M4 | 24 |
| HTML -> Ecmarkup | 19 |
| Makefile -> Assembly | 12 |
| Rust -> RenderScript | 11 |
| XML -> Microsoft Visual Studio Solution | 5 |
| Shell -> Tcsh | 1 |
| Text -> Roff | 1 |

## Limitations / scope

- This harness measures the extension/detection phase only. Issue-item 5
  (embedded-language detection, structural, at parse time) is not measured here.
- The Linguist column is the extension map of languages.yml alone (no heuristics,
  no content classifier). Top-1 on a multi-claim extension uses first-claim order
  in the file — an extension-only strategy is genuinely underspecified there, which
  is itself a measured finding.
- Corpus gap: `.m` and `.v` ambiguity (issue text) does not occur in these four
  repos; the ambiguous category is carried by `.h`, `.ts`, `.inc`.
- Timings/values are recorded, never asserted; reruns on the pinned corpus are
  deterministic.


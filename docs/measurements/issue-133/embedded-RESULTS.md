# Issue #133 item 5: embedded-language detection (structural, at parse time)

Harness: `Minotaur.Benchmarks.EmbeddedDetection`. Same pinned corpus as the classification-accuracy harness (issue #133) - redis 8.0.2, curl curl-8_23_0, git v2.56.1, bootstrap v5.3.9 - over the polyglot-container extensions `.html`/`.htm`/`.xhtml`/`.md`/`.markdown`/`.mdx`/`.astro`.

Detection is scoped at region level (matching the classification-accuracy harness's region-level framing): a detection is credited when grammar-driven structural parsing recognizes an embedded region (script/style block, code fence) as a coherent structural span, regardless of the inner tokenization. Truth is adjudicated by markers, with exclusions recorded (external scripts, empty blocks, template/html/text typed scripts, vbscript).

Engine: inline `RealTimeParserSession` (DevelApp.StepParser), fresh session per file, per-file timeout `1000 ms` (region-level detection is a classification claim that must be visible within a viability budget; probe evidence shows whole-file markdown cannot finish even under 40 s, so the budget mainly bounds the run and records timeouts honestly).

## Outcome taxonomy

The engine cannot always finish an adversarial parse inside one process, so every parse runs in a child worker process (`<this-assembly> --worker <htmlGrammar> <mdGrammar>`; request/ack/result lines over stdin/stdout) and the parent kills the worker tree after the per-file budget. Outcomes: `success` | `parse-fail` (clean engine failure) | `timeout` (killed at budget, worker respawned) | `parse-crash` (worker died instead of answering) | `startup-fail` (no ack within 10 s) | `engine-oom` / `engine-error` (worker caught OutOfMemoryException / other exception during parse) | `no-grammar` (mdx/astro: no shipped grammar, never parsed) | `read-fail` (file unreadable).

## Per-format results

### astro (106 files, 668520 bytes)

- parsed (grammar present): 0 of 106; no-grammar: 106
- outcomes: success 0, parse-fail 0, timeout 0, parse-crash 0, startup-fail 0, engine-oom 0, engine-error 0, read-fail 0
- truth regions: script 0, style 0, fence 104 (untagged 0), excluded 0
- detections: script open tokens 0, style open tokens 0, tagged fences 0, untagged fences 0
- span-matched: script 0, style 0, fence 0; recall: script null, style null, fence 0 (null = no truth)
- fence info tags captured: 0; files with any truth region: 104

### html (19 files, 444518 bytes)

- parsed (grammar present): 19 of 19; no-grammar: 0
- outcomes: success 0, parse-fail 0, timeout 0, parse-crash 19, startup-fail 0, engine-oom 0, engine-error 0, read-fail 0
- truth regions: script 6, style 7, fence 0 (untagged 0), excluded 16
- detections: script open tokens 0, style open tokens 0, tagged fences 0, untagged fences 0
- span-matched: script 0, style 0, fence 0; recall: script 0, style 0, fence null (null = no truth)
- fence info tags captured: 0; files with any truth region: 8

### markdown (987 files, 2819168 bytes)

- parsed (grammar present): 987 of 987; no-grammar: 0
- outcomes: success 1, parse-fail 1, timeout 985, parse-crash 0, startup-fail 0, engine-oom 0, engine-error 0, read-fail 0
- truth regions: script 0, style 0, fence 1507 (untagged 109), excluded 0
- detections: script open tokens 0, style open tokens 0, tagged fences 0, untagged fences 0
- span-matched: script 0, style 0, fence 0; recall: script null, style null, fence 0 (null = no truth)
- fence info tags captured: 0; files with any truth region: 561

### mdx (110 files, 1001940 bytes)

- parsed (grammar present): 0 of 110; no-grammar: 110
- outcomes: success 0, parse-fail 0, timeout 0, parse-crash 0, startup-fail 0, engine-oom 0, engine-error 0, read-fail 0
- truth regions: script 0, style 0, fence 341 (untagged 0), excluded 0
- detections: script open tokens 0, style open tokens 0, tagged fences 0, untagged fences 0
- span-matched: script 0, style 0, fence 0; recall: script null, style null, fence 0 (null = no truth)
- fence info tags captured: 0; files with any truth region: 62

## The skeleton finding

`HTMLEmbedded.grammar` references `<script-content>` and `<style-content>` productions whose bodies are empty at load time. The engine rejects empty/zero-width token rules at grammar load (issue #111 discipline) and prints load notices (`Rejected zero-width token rule: <script-content> ::= "" ;`), i.e. the grammar's script/style delegation is skeleton-only: the container grammar ships without the embedded-language sub-grammars it implies. Structural detection from `HTMLEmbedded.grammar` is therefore limited to container-level structure at best, and no HTML file in this corpus finished a whole-file structural parse (all 19 ended in `parse-crash`: worker process death during parse, consistent with memory exhaustion).

`Markdown.grammar` is not the skeleton case: it loads, but whole-file parses of real README-sized markdown blow up in the GLR engine (probe: 25-line/409-byte file took 41-43 s and then failed with `PS2003: Parsing exception: OutOfMemoryException`), so detection credits are not expected on the markdown family either. The per-file timeout protocol records these blowups honestly (as `timeout` and process-death classes) instead of hanging the run.

## Tagged-fence inventory audit

Every info-tag first-word found in the corpus's markdown-family fence openings, and whether a monorepo grammar is shipped for that language.

| fence tag | shipped grammar? | files |
| --- | --- | --- |
| c | corpusGrammar (programming-languages/c17/C17.grammar) | 535 |
| cmake | corpusGrammar (infrastructure/cmake/CMake.grammar) | 1 |
| cpp | corpusGrammar (programming-languages/cpp20/Cpp20.grammar) | 1 |
| css | corpusGrammar (programming-languages/css/CSS.grammar) | 9 |
| diff | none shipped | 2 |
| http | none shipped | 1 |
| javascript | corpusGrammar (programming-languages/javascript/JavaScript.grammar) | 21 |
| json | corpusGrammar (programming-languages/json/JSON.grammar) | 4 |
| perl | none shipped | 1 |
| plaintext | none shipped | 7 |
| powershell | corpusGrammar (programming-languages/powershell/PowerShell.grammar) | 1 |
| scss | none shipped | 23 |
| shell | corpusGrammar (programming-languages/bash/Bash.grammar) | 18 |
| toml | corpusGrammar (data-config/toml/TOML.grammar) | 1 |
| typescript | corpusGrammar (programming-languages/typescript/TypeScript.grammar) | 104 |

## Registry mapping status

- `.html` / `.htm`: registry-mapped to `HTMLEmbedded.grammar` (grammar-index.json contains the mapping).
- `.xhtml`: no mapping; the harness assigns `HTMLEmbedded.grammar` (recorded as harness-assigned).
- `.md` / `.markdown`: no registry mapping. `Markdown.grammar` exists under `specialized/markdown/` but is not referenced by `grammar-index.json`; the harness loads it by direct path and records the miss. Extension-side, the corpus's `.md` files are simply unclassified by the registry today.
- `.mdx` / `.astro`: no shipped grammar of any kind; these files are recorded `no-grammar` and never parsed.

## Limitations

- Region-level detection only: no claim is made about tokenizing the inner embedded language, only that its region is span-recognized at structural parse time.
- The worker+kill guard makes outcomes robust, but per-format GLR cliffs mean per-file times are highly skewed; the per-file timeout bounds the run rather than defining a claim budget.
- Deterministic reruns: the harness writes an append-mode resume cache (`embedded-partial-cache.jsonl`); resumed runs skip already-processed file keys, and a torn final line after an interrupted run is tolerated (dropped).


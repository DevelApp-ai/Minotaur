# Minotaur.Cli

The Minotaur command line. Currently one command family: `labyrinth` — runs
Labyrinth SAST rule packs (e.g. the [Minotaur-Labyrinth](https://github.com/DevelApp-ai/Minotaur-Labyrinth)
packs) over source files.

```bash
dotnet run --project src/Minotaur.Cli -- labyrinth scan \
  --rules ../Minotaur-Labyrinth/rules \
  --grammar java17 \
  --src path/to/project \
  --sarif labyrinth.sarif
```

- `--rules` — a rule pack directory, or a pack root with per-grammar
  subdirectories (Minotaur-Labyrinth layout: `rules/<grammar-dir>/`), in which
  case only `rules/<grammar>` is loaded.
- `--grammar` — a Labyrinth target grammar directory name from
  `LabyrinthTargetGrammars` (list with `labyrinth grammars`), e.g. `java17`,
  `csharp10`, `python311`, `cpp20`, `json`.
- `--src` — a source file or a directory scanned recursively; `--ext` filters
  extensions (defaults per grammar, e.g. `.java` for `java17`).
- `--sarif` — also write a SARIF 2.1.0 log (GitHub code scanning).
- `--baseline` / `--write-baseline` — suppress accepted findings via the
  baseline file (see the Labyrinth Rule Author Guide).

Exit codes: `0` no reportable findings, `1` findings, `2` configuration error
(invalid rule pack, missing grammar, ...).

`labyrinth verify-corpus --rules <dir> --tests <dir>` runs a rule test corpus
(`tests/<grammar>/<rule-id>/{vulnerable,safe}.* + expected.json`, as in
Minotaur-Labyrinth) through the engine and checks every case — this is what
Minotaur-Labyrinth's `run-corpus` CI workflow executes.

The host adapter (`ScanHost`) parses each file with the target grammar through
the real DevelApp.StepLexer/StepParser pipeline (grammars are downloaded at
build time from Minotaur-Grammars into the `Grammars/` output directory),
materializes match nodes from the token stream (`FunctionCall` / `Assignment`
/ `Identifier` conventions), and builds a document-order data-flow graph for
the taint engine.

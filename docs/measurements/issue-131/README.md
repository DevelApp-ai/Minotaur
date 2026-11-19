# Issue #131 measurement harness — grammar compile time by rule-count bin

Supports `articles/llvm-grammar-compilation/content/main.md` (measurement
backlog: DevelApp-ai/Minotaur#131; synthetic corpus from
DevelApp-ai/Minotaur-Grammars#192).

## What is measured

`Minotaur.Benchmarks.GrammarCompilation --synthetic <outDir>` compiles the
committed synthetic grammars from Minotaur-Grammars
`benchmarks/synthetic/rules-{020,060,180,320}/` into the engine's shipped
`RealTimeParserSession` and reports, per bin: cold ms (fresh child process,
includes JIT), warm median ms of 10 re-compilations, rule/token counts,
smoke-parse result and retained-heap footprint. Bins map to the article's
<50 / 50-200 / 200+ rule classes.

Outputs: `results.json` + `RESULTS.md` in the output dir. The
`article-measurements` job in `.github/workflows/ci-cd-enhanced.yml` runs it
on every PR/push against a Minotaur-Grammars@main checkout
(`MINOTAUR_GRAMMARS_DIR`) and uploads the `article-measurements` artifact.

## Not measurable here (external baselines)

- **IR generation vs LLVM compile split**: the shipped LLVMIRGenerator
  pipeline needs an LLVM toolchain (clang/llc), unavailable on the .NET CI
  runner. The C#-process compile path measured here is the shipped target.
- **Thread scaling (1/2/4/8/16 threads)**: the lock-free parallel parse path
  must be verified/benchmarked on a machine with the target core count;
  record machine core count with the curve.
- **ARM Cortex-A72 claims**: need real ARM hardware or an ARM CI runner, or
  the percentage claims must be dropped article-side.

## How to run

```bash
dotnet run --project src/Minotaur.Benchmarks.GrammarCompilation -- --synthetic docs/measurements/issue-131
# with a local grammars checkout:
MINOTAUR_GRAMMARS_DIR=/path/to/Minotaur-Grammars dotnet run --project src/Minotaur.Benchmarks.GrammarCompilation -- --synthetic out
```

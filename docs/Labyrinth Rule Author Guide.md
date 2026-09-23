# Labyrinth Rule Author Guide

How to write rules for the Labyrinth SAST rule engine (issues [#100](https://github.com/DevelApp-ai/Minotaur/issues/100), [#105](https://github.com/DevelApp-ai/Minotaur/issues/105)).

Labyrinth follows the Semgrep paradigm: you write rules as YAML containing syntax
snippets of the *target grammar*, augmented with metavariables, the ellipsis
operator and an optional Dynamic LINQ escape hatch. The engine compiles the
patterns to native C# delegates and runs a fixed-point taint traversal directly
on the CognitiveGraph — no serialization, no interpretation.

## The rule file

Rules live in YAML files under a rule pack directory (`rules/` by default), as a
`rules:` list:

```yaml
rules:
  - id: custom-grammar-injection
    severity: ERROR
    message: "Untrusted data flows into a critical execution sink."
    type: taint
    sources:
      - pattern: "ReceiveData($DATA)"
    sinks:
      - pattern: "ExecuteAction(..., $DATA, ...)"
    sanitizers:
      - pattern: "VerifyIntegrity($DATA)"
    propagators:
      - pattern: "$TARGET = FormatString($SRC)"
        from: $SRC
        to: $TARGET
```

### Rule keys

| Key | Required | Values | Meaning |
| :--- | :--- | :--- | :--- |
| `id` | yes | kebab-case, unique in the pack | Rule identifier reported with each finding |
| `severity` | yes | `ERROR`, `WARNING`, `INFO` | Finding severity (drives the SARIF level) |
| `message` | no | free text | Finding message; defaults to the rule id |
| `type` | yes | `search`, `taint` | Analysis kind |
| `pattern` | search | target-grammar snippet | The single pattern to match |
| `condition` | no | Dynamic LINQ expression | Extra boolean constraint on the matched node |
| `sources` / `sinks` | taint | pattern entry list | Where untrusted data enters / arrives |
| `sanitizers` | no | pattern entry list | Matches that prune taint state |
| `propagators` | no | pattern + `from`/`to` | Transfers taint between metavariables |

### Patterns

A pattern is a snippet of the target grammar's syntax, parsed by the
StepLexer/StepParser pipeline:

- **Metavariables** — `$NAME` matches any single node and captures it. The
  unification scope is the whole rule: the same metavariable must bind the very
  same node everywhere it occurs, so `$DATA` bound at a source only matches a
  sink consuming that exact node.
- **Ellipsis** — `...` matches any sequence of sibling nodes (zero or more),
  e.g. `ExecuteAction(..., $DATA, ...)`.
- Node type conventions: `FunctionCall` (name = callee, arguments = arguments),
  `Assignment` (argument 0 = left-hand side, argument 1 = assigned expression).

### Dynamic LINQ conditions

Every pattern entry (search `pattern`, taint `sources`/`sinks`/`sanitizers`,
`propagators`) accepts an optional `condition:` — a string C# boolean expression
([System.Linq.Dynamic.Core](https://github.com/zzzprojects/System.Linq.Dynamic.Core)
syntax) evaluated against the matched node. Example:

```yaml
- id: advanced-size-check
  type: search
  pattern: "AllocateMemory($SIZE)"
  condition: "node.Arguments[0].Value > 1024"
```

Malformed conditions fail fast at rule load time with a rule error naming the rule.

## Loading rule packs

A rule pack is a directory of `.yaml`/`.yml` rule files (scanned recursively):

```csharp
var pack = new LabyrinthRulePackLoader().TryLoadDirectory("rules/");
if (!pack.IsValid)
{
    // pack.Errors: one entry per invalid rule, prefixed with file and rule index,
    // e.g. "sql/rules.yaml: rules[2]: unknown severity 'high'"
}

var ruleSet = new LabyrinthRulePackLoader().LoadDirectory("rules/"); // throws on any error
```

## Reporting findings

`LabyrinthTaintFinding` carries everything the reporting formats need: rule id,
severity, message, source, sink and the full source→sink path (with locations
from the CognitiveGraph when the host adapter provides them).

### Console

```csharp
var reporter = new LabyrinthConsoleReporter(Console.Out);
int exitCode = reporter.Report(findings, suppressedCount);
```

Output (SARIF-friendly plain text):

```
ERROR [custom-grammar-injection] @app.cs:12:1
  Untrusted data flows into a critical execution sink.
  Path:
    FunctionCall:ReceiveData @app.cs:10:5
      -> Identifier:temp
        -> FunctionCall:ExecuteAction @app.cs:12:1
1 finding(s).
```

### SARIF (GitHub code scanning)

```csharp
string sarif = LabyrinthSarifWriter.Write(findings, root: "https://example.com/repo/");
```

Produces a SARIF 2.1.0 log: one result per finding, severity mapped to
`error`/`warning`/`note`, sink location as the result location, and the full
data-flow path as a `codeFlow` threadFlow. Upload to GitHub code scanning via
`sarif-upload` (e.g. `github/codeql-action/upload-sarif`).

### Exit codes

| Code | Meaning |
| :--- | :--- |
| `0` (`LabyrinthExitCodes.Success`) | Analysis ran, no reportable findings |
| `1` (`LabyrinthExitCodes.Findings`) | One or more findings (after baseline suppression) |
| `2` (`LabyrinthExitCodes.ConfigurationError`) | Invalid rules, baseline or graph error |

## Baselines for CI

A baseline stores a stable SHA-256 fingerprint per accepted finding (over rule
id, severity and source/sink identity incl. location). Suppressed findings do
not affect the exit code, so a pipeline only breaks on *new* findings.

```csharp
// Review run: write the baseline after triage
LabyrinthBaseline.Save("labyrinth-baseline.json", findings);

// CI run: suppress accepted findings
var baseline = LabyrinthBaseline.Load("labyrinth-baseline.json");
var reportable = baseline.Apply(findings, out int suppressed);
```

Fingerprints are location-sensitive: editing the code around an accepted finding
moves it and makes it reportable again, forcing re-review.

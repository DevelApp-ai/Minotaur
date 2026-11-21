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

## Pattern v2 shapes (receiver binding, operators, index access)

Beyond the v1 shapes (plain calls, assignments with call right-hand sides,
bare metavariables/identifiers), patterns support the following shapes. All v2
matching works on `FunctionCall`, `Assignment` and `IndexAccess` nodes whose
receivers and operator operands are materialized into the data-flow graph.

### Receiver binding for dotted calls

`$X.Method(...)` matches any dotted call whose callee name ends in
`.Method` and binds `$X` to the *receiver node* (the expression before the
dot) captured earlier in the statement:

```yaml
# source: the receiver of a maybe-null-returning call
- pattern: "$RCV.FirstOrDefault(...)"
# sink: a dereference of the same receiver through a captured variable
- pattern: "$X.ToString()"
```

Because a v1 call match is *exact name equality* and a dotted call's node name
is the full dotted name (`items.FirstOrDefault`), sources over dotted calls
should be written with a metavariable receiver ($RCV above) rather than a bare
callee (`FirstOrDefault(...)`), which would only match undotted calls.

Literal receivers work too, e.g. the guard call sanitizer:

```yaml
sanitizers:
  - pattern: "ArgumentNullException.ThrowIfNull($X)"
```

### Conditional access marker

`x?.ToString()` materializes with node name `x?.ToString`: the `?.` survives
inside the name. Consequences:

- A deref sink `$X.ToString()` never matches a conditional access (the name
  contains `?`), so `?.` is inherently safe against receiver-null sinks.
- To match a conditional access explicitly, write the marker into the
  pattern: `$X?.Method(...)`.

### Operator node conventions

Binary operators materialize as two-argument `FunctionCall` nodes with
reserved names, and patterns use the surface syntax:

| Surface pattern | Node | Arguments |
| :--- | :--- | :--- |
| `$A / $B` | `FunctionCall op:divide` | left, right |
| `$A % $B` | `FunctionCall op:modulo` | left, right |
| `$V as $T` | `FunctionCall op:as` | expression, type |
| `$A ?? $B` | `FunctionCall op:coalesce` | left, right |

Operands may be metavariables or plain identifiers.

### Index access

`$ARR[$I]` matches an `IndexAccess` node (name = array expression, index
expression as sole argument, array node also bound as receiver) and binds
both. Literal array names match by name equality: `data[$I]` matches an index
into `data` with any index. Index nodes are also sink-capable without a
metavariable receiver (`data[$I]`).

### Relaxed assignment right-hand sides

Assignment patterns accept more than call right-hand sides. All of these
bind both sides:

```yaml
propagators:
  - pattern: "$X = $Y"              # bare copy
    from: $Y
    to: $X
  - pattern: "$SAFE = $A ?? $B"     # null-coalescing rescue
  - pattern: "$X = $Y.DoWork()"     # call into a fresh variable
  - pattern: "$V = $ARR[$I]"        # index read
```

### The unification rule for taint flows

Metavariables unify across the whole rule, and the taint chain is
document-order linear: a finding requires the sink to *reuse the same bound
node* the propagator transferred taint to. In practice, write the propagator's
`to:` metavariable with the same name the sink binds, e.g. for a null-deref
rule:

```yaml
sources:
  - pattern: "$RCV.FirstOrDefault(...)"
propagators:
  - pattern: "$X = $Y"        # $X is the same metavariable as the sink receiver
    from: $Y
    to: $X
sinks:
  - pattern: "$X.ToString()"
```

Here taint entering at `items.FirstOrDefault()` propagates to `x` via
`$X = $Y` and the sink `$X.ToString()` matches at `x.ToString()` because the
sink's `$X` must bind the very same node. A distinct variable
(`other.Create(); y.ToString()`) fails receiver unification and is not
reported.

### Sanitizer flow semantics

A sanitizer match prunes the taint state *at the matched node*, so a guard
between source and sink removes the finding: `ArgumentNullException.ThrowIfNull(x)`
between `var x = items.FirstOrDefault()` and `x.ToString()` yields no finding.
The `??` operator can be modeled either as a propagator out of a rescue
assignment (`$SAFE = $A ?? $B`) matching only the uncoalesced variable, or as a
sanitizer on the coalesce node itself.

### Known limitations (honest scope)

- The data-flow graph is a **document-order linear chain**: no real control
  flow, loops, or interprocedural edges. Findings require source-before-sink
  in statement order.
- Branch guards (`if (x == null) return;`) are **not materialized**; only call
  guards such as `ThrowIfNull` work.
- A sanitizer prunes the chain at the match point; if the same variable is
  re-tainted only through a node *before* the sanitizer, the finding stays
  suppressed (approximation, may cause false negatives after refactors).
- Pattern shapes not supported in v2: middle-segment call patterns
  (`M($A, ..., $B)` with dotted receivers where the receiver itself is a
  call), ternaries, and compound assignments (`??=`, `+=`).

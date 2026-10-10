# Grammar-Driven Semantic Binding (Minotaur issue #121)

Minotaur's semantic binder resolves declarations and references on the
**cognitive graph** (the `CognitiveGraphNode` tree produced by
`StepParserIntegration.ParseToCognitiveGraphAsync`). There is no separate AST:
StepParser produces a cognitive graph, and the binder walks that graph.

The binder contains **zero per-language logic**. All binding semantics come
from declarative annotations stored in a grammar's `Metadata` under the
`binding` key. Adding binding support for a new grammar means adding
annotations — no engine code changes.

## Annotation format

Grammars annotate binding behavior with a JSON object in
`Grammar.Metadata["binding"]`:

```json
{
  "scopeOpeners": ["method_body", "block"],
  "declarations": {
    "class_declaration": { "name": "identifier" },
    "method_declaration": { "name": "identifier" }
  },
  "containers": ["namespace_declaration"],
  "imports": {
    "using_directive": { "import": "qualified_name" }
  },
  "references": {
    "base_list": { "reference": "identifier" }
  },
  "typeDeclarationTokens": ["class", "interface"]
}
```

| Section | Meaning |
|---|---|
| `scopeOpeners` | Productions that open a new lexical scope (blocks, bodies). |
| `declarations` | Productions that declare a named symbol. The `name` role gives the token type carrying the declared name. |
| `containers` | Productions that contribute a segment to the qualified container path (namespace/package/module). The `container` role names the token type carrying the segment. |
| `imports` | Productions that import names into file scope (using/import statements). The `import` role gives the token type carrying the imported (possibly dotted) name. |
| `references` | Productions that contain a name reference to resolve (base lists, extends clauses). The `reference` role gives the token type carrying the referenced name. |
| `typeDeclarationTokens` | Keyword texts that mark a declaration as a *type* declaration; used for the symbol `Kind`. |

When a role's token type is not found in the production subtree, the binder
falls back to the first `IdentifierNode`.

## Binding model

- **`GrammarBindingProfile`** — parsed annotations for one grammar
  (`GrammarBindingProfile.FromGrammar(grammar)`).
- **`SemanticBinder`** — walks the cognitive graph per file, collecting:
  - declarations into a `ScopeAwareSymbolTable` keyed by container path and
    simple name, with **stable symbol ids** (`language|Container.Name`) that
    hold across files and re-binds;
  - references, resolved after all files are bound
    (`ResolvePendingReferences()`), so cross-file binding works regardless of
    parse order.
- **`BindingSymbol` / `BindingReference`** — resolved symbol identity (or an
  unresolved marker) per reference, ready for downstream consumers such as
  SpecTreeGenerator's relationship edges.

Resolution order for a simple name: current container chain (innermost
first), then imported containers, then a unique global match. Qualified names
(`N1.A`) resolve directly by container. Same-named symbols in different
containers therefore bind correctly per imports/usings.

## Usage

```csharp
var engine = new SymbolicAnalysisEngine();
var result = await engine.ParseAndBindAsync(sourceCode, language, grammar);
// result.Symbols: declared symbols with stable ids
// result.References: references, resolved or unresolved
```

`SymbolicAnalysisEngine.AnalyzeCode` now parses through the real StepParser
pipeline by default; the previous mock graph path is isolated behind
`UseRealParser = false` for deterministic tests.

## Proven acceptance

`SemanticBinderTests` demonstrates, purely via annotations:

- C#-shaped, Java-shaped and Python-shaped grammars binding `A` in two
  containers, referenced per using/import correctly
  (Minotaur issue #121 acceptance criterion).
- A fictional new grammar (entity/domain) binding with **zero engine changes**.
- Qualified cross-container references and unresolved-reference reporting.

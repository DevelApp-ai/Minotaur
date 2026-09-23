# **Strategic Architecture Report: Designing a High-Performance SAST Rule Engine for Custom Abstract Syntax Graphs in .NET**

## **1\. Introduction and Architectural Imperative**

The evolution of Static Application Security Testing (SAST) has shifted the industry away from shallow, regular-expression-based linting toward deep, semantic analysis of source code. Modern SAST requires the construction of sophisticated intermediate representations, typically combining an Abstract Syntax Tree (AST), a Control Flow Graph (CFG), and a Data Flow Graph (DFG) into a unified Code Property Graph (CPG) or a highly enriched syntax graph. Within the .NET ecosystem, the Minotaur project utilizes a proprietary framework, the CognitiveGraph, which serves as a highly advanced AST replacement capable of representing code parsed from arbitrary grammar files via the DevelApp.StepLexer and DevelApp.StepParser pipelines1.  
Because the CognitiveGraph is designed to analyze general code specified by custom grammar files, traditional .NET analysis frameworks such as Roslyn are fundamentally misaligned, as they are tightly coupled to the C\# and Visual Basic specifications3. Consequently, there is an architectural imperative to define and implement a standardized rule creation language specifically tailored for the CognitiveGraph. This language must satisfy three distinct, and often conflicting, engineering constraints:

> 1. It must be extremely intuitive and easy for end-users to learn, minimizing the friction typically associated with writing complex security rules.  
> 2. It must be highly expressive, capable of articulating the complex data flow patterns, taint propagation mechanics, and graph reachability queries inherent to robust SAST engines.  
> 3. It must be highly efficient, executing over massive, highly connected in-memory graphs without inducing unacceptable latency or memory overhead.

This report exhaustively evaluates the architectural viability of Open Policy Agent (OPA) Rego—both via WebAssembly (opa.wasm) and potential native C\# implementations—against alternative paradigms, including Datalog-based relational engines (CodeQL), syntactic pattern-matching frameworks (Semgrep), and graph query languages (Cypher). The analysis culminates in a comprehensive blueprint for a hybrid rule engine architecture that couples a declarative, YAML-based rule specification with highly optimized, dynamically compiled .NET Expression Trees operating directly on the native CognitiveGraph.

## **2\. The Theoretical Mechanics of Semantic SAST and Data Flow Analysis**

To critically evaluate any rule definition language, it is first necessary to deconstruct the exact computational requirements of static application security testing. SAST does not merely search for isolated tokens; it searches for relationships and data flows between those tokens across the entirety of a program's execution space5.

### **2.1. Taint Tracking and the Data Flow Lifecycle**

The foundational algorithm of modern vulnerability detection is taint analysis. Taint analysis tracks the lifecycle of potentially malicious or untrusted data as it moves through a program's execution paths, seeking to identify instances where this data reaches a sensitive operation without undergoing proper sanitization5. The formal model of taint analysis relies on four distinct classifications of AST or graph nodes:

> * **Sources:** These are the entry points where untrusted data crosses the trust boundary and enters the application memory space. Common sources include HTTP request parameters, environment variables, deserialization endpoints, and file system reads5. When data originates from a source, it is marked with a "tainted" state.  
> * **Sinks:** Sinks are the dangerous execution points where tainted data can cause a catastrophic failure or security breach if executed. Examples include database execution commands (leading to SQL Injection), operating system process invocations (Command Injection), or dynamic HTML rendering (Cross-Site Scripting)5.  
> * **Propagators:** These are intermediate operations that transfer the taint state from one variable or memory location to another. Propagators include string concatenations, mathematical operations, variable reassignments, array insertions, and method return values5. For example, if a tainted string is added to a list, the list itself must inherit the tainted state9.  
> * **Sanitizers:** Sanitizers are functions, type casts, or encoding operations that definitively neutralize the threat of the tainted data. If tainted data passes through a recognized sanitizer (e.g., an HTML encoding function before rendering), the taint state is stripped, and the data is considered safe for downstream sinks5.

### **2.2. Graph Reachability and Fixed-Point Iteration**

The detection of a vulnerability is mathematically equivalent to solving a graph reachability problem: a vulnerability exists if and only if there is a valid path in the Data Flow Graph from a Source node to a Sink node that does not intersect a Sanitizer node6.  
However, this is not a simple linear traversal. Codebases feature complex, non-linear control flows, including conditional branches, loops, and recursive function calls. To accurately model how data flows through these structures, the analysis engine must employ a fixed-point iteration algorithm7.  
In fixed-point iteration, the engine continuously propagates taint states along the edges of the DFG. If a variable's state changes (e.g., it transitions from safe to tainted), that variable is added to a worklist. The engine processes the worklist, updating downstream dependencies, which may in turn be added to the worklist. This process is inherently recursive and continues until a "fixed point" is reached—a state of equilibrium where a full evaluation cycle of the graph results in no new state changes or taint propagations7. Any rule language selected for the CognitiveGraph must either natively execute this unbounded recursive algorithm or provide the semantic anchors for an external execution engine to perform the traversal efficiently.

## **3\. Evaluation of OPA Rego for SAST and Graph Analysis**

Open Policy Agent (OPA) is a cloud-native, open-source policy engine designed to decouple policy decision-making from application logic. It utilizes a high-level declarative language called Rego, which is deeply inspired by Datalog and optimized for evaluating complex, hierarchical JSON data structures12. Given its status as an industry standard for authorization and infrastructure-as-code governance, Rego is frequently considered for generalized rule enforcement.  
The proposition involves writing SAST rules in Rego and executing them against the CognitiveGraph using the opa.wasm integration within the .NET environment, or alternatively, building a native C\# Rego implementation if the WASM boundary proves too inefficient. Both approaches present severe architectural and algorithmic limitations when applied to the domain of SAST.

### **3.1. The Algorithmic Mismatch: Recursion and Termination Guarantees**

The most critical failure point of Rego in the context of SAST is its architectural stance on recursion. Rego is explicitly designed to guarantee termination, predictability, and safety in policy evaluations, ensuring that authorization checks do not result in infinite loops or denial-of-service conditions14. To enforce this, OPA's compiler strictly prohibits mutually recursive rule definitions by default14.  
As established in the theoretical mechanics of SAST, data flow analysis requires traversing loops, resolving cyclical dependencies, and computing fixed points over unpredictable graph topologies. While Rego provides specific built-in functions, such as graph.reachable, to handle simple reachability queries (e.g., traversing a hierarchical role-based access control tree), this primitive is fundamentally inadequate for taint analysis15. Taint tracking is not a simple boolean reachability check; it is a stateful traversal. As the engine walks the graph, it must evaluate complex constraints at each node—applying propagators that alter the taint state (such as tracking index sensitivity in arrays) and evaluating sanitizers that conditionally halt the traversal9. Because Rego lacks the ability to define custom, state-mutating, recursive graph traversal algorithms natively, it cannot express the deep interprocedural taint tracking required for a modern SAST engine17.

### **3.2. WebAssembly (opa.wasm) Integration and the Serialization Tax**

Executing OPA policies via WebAssembly within a .NET host is a well-documented pattern, supported by robust NuGet packages such as OpaDotNet.Wasm18. In this architecture, the .NET application compiles Rego policies into WASM byte code and invokes them using a WASM runtime.  
However, crossing the interop boundary between the managed .NET Common Language Runtime (CLR) and the linear memory space of WebAssembly introduces a catastrophic performance bottleneck when dealing with graph structures. Rego is inherently designed to operate on JSON documents13. To evaluate the CognitiveGraph using opa.wasm, the entire in-memory graph—encompassing the AST, CFG, and DFG—would have to be serialized into a JSON payload and marshaled across the WASM boundary for every file or analysis unit.  
This serialization imposes multiple severe penalties:

> 1. **Topological Flattening and Memory Bloat:** An AST is a highly connected graph where nodes frequently share references (e.g., a variable reference in an expression points back to its declaration). JSON is a hierarchical, tree-based format. Serializing a graph into JSON forces the engine to replace direct memory pointers with synthetic string identifiers or to endlessly duplicate shared nodes, resulting in massive memory bloat and exponential payload sizes20.  
> 2. **Serialization Latency:** The CPU cycles expended traversing the .NET object graph, converting it to text-based JSON, copying it to WASM linear memory, and parsing it back into OPA's internal representation will vastly exceed the computational time of the actual rule evaluation4.  
> 3. **WASM Execution Overhead:** While WebAssembly is designed for near-native speed, running a policy interpreter inside a WASM virtual machine, which is itself running inside the managed .NET CLR, introduces compounding layers of virtualization overhead. Benchmarks indicate that WebAssembly execution can be up to 45% slower than highly optimized, native host-language execution, particularly for memory-intensive traversal tasks23.

### **3.3. Feasibility of a Native C\# Rego Implementation**

The prompt notes that if opa.wasm is ineffective, a native implementation of OPA Rego could be developed in C\#. While a native implementation entirely eliminates the WASM serialization tax and allows the engine to query the CognitiveGraph directly via reflection or dynamic dispatch, it does not resolve the fundamental algorithmic and usability issues.  
Even if implemented natively in C\#, the language specification of Rego remains unchanged. The engine would still be forced to restrict recursion, rendering complex taint analysis impossible to express purely in the rule language. Furthermore, a native implementation would require replicating OPA's complex unification and evaluation engine—a massive engineering undertaking that essentially demands rewriting a highly specialized logic programming runtime from scratch in C\#.

### **3.4. The Usability and Learnability Barrier**

A primary requirement for the Minotaur project is that the rule creation language must be "easy for the user to learn." Rego severely violates this constraint. Rego is a declarative, logic-based query language that forces developers to adopt a radically different mental model compared to traditional imperative or object-oriented programming13.  
Writing rules to traverse abstract syntax trees in Rego requires expressing code patterns as deeply nested, indirect JSON queries. Security researchers and developers typically think about code vulnerabilities in terms of syntactic patterns—what the code physically looks like on the screen. Forcing them to translate those intuitive visual patterns into abstract logical predicates over JSON hierarchies introduces a massive learning curve, resulting in brittle, hard-to-maintain rule sets25.

| Evaluation Vector | OPA Rego (WASM or Native) | Suitability for CognitiveGraph SAST |
| :---- | :---- | :---- |
| **Algorithmic Expressiveness** | Prohibits mutual recursion; limited to simple static reachability built-ins. | **Critically Deficient:** Cannot natively express the stateful, fixed-point iteration required for accurate data flow analysis. |
| **Performance via WASM** | Imposes a massive serialization tax to convert C\# object graphs to JSON. | **Critically Deficient:** Graph flattening causes memory bloat and unacceptable latency. |
| **Native Implementation Viability** | Solves the WASM serialization tax but requires rewriting a complex logic engine. | **Poor:** High engineering cost without solving the underlying language limitations regarding recursion. |
| **Learnability and UX** | Steep learning curve based on Datalog logic over hierarchical data. | **Poor:** Highly unintuitive for developers attempting to match syntactic code patterns. |

## **4\. Alternative Paradigms for SAST Rule Languages**

Given the architectural mismatch of OPA Rego, the design of the Minotaur rule engine must look to the three dominant paradigms currently utilized in the static analysis industry: the Relational/Datalog approach (CodeQL), the Graph Query approach (Cypher), and the Syntactic Pattern-Matching approach (Semgrep). By analyzing the strengths and failures of these models, a synthesized, optimal architecture for .NET can be formulated.

### **4.1. The Relational and Logic Paradigm: CodeQL and Datalog**

GitHub's CodeQL represents the most semantically powerful approach to SAST available today. Instead of analyzing text or simple trees, CodeQL operates by utilizing customized extractors that compile the target source code's AST, CFG, and DFG into a highly optimized, queryable relational database6. Users then author security rules using QL, a declarative, object-oriented query language heavily derived from Datalog6.  
Datalog is mathematically flawless for SAST applications. Unlike Rego, standard Datalog natively supports recursive relations and mutual recursion17. Datalog engines operate via bottom-up, semi-naive evaluation, naturally computing the exact transitive closures and fixed-point reachability graphs required to trace complex taint propagations across highly connected codebases31.  
**Applicability to Minotaur:** While CodeQL proves that Datalog is the correct mathematical foundation for graph reachability, it fails the usability constraint. Writing queries in QL requires a massive context switch for developers. They must abandon their understanding of code syntax and adopt a mindset centered on relational algebra and logical predicates33. Furthermore, implementing a highly optimized, concurrent Datalog execution engine—similar to CodeQL's use of the Soufflé engine—natively within C\# is a colossal, multi-year engineering endeavor30. While theoretically powerful, a Datalog approach is too complex for end-users to learn quickly and too difficult to implement efficiently without relying on external, non-.NET binaries.

### **4.2. The Graph Query Paradigm: Cypher**

Because the underlying data structure generated by the Minotaur project is literally a graph (the CognitiveGraph), utilizing a native property graph query language like Cypher appears to be a natural fit. Cypher, originally developed for the Neo4j database, allows users to express complex graph topologies using an ASCII-art inspired syntax35.  
In a SAST context utilizing Cypher, taint analysis could be expressed through variable-length path queries. A rule attempting to find a path from a user input source to a database execution sink might look like this: MATCH path \= (source:MethodCall {name: 'GetUserInput'})-\[\*1..15\]-\>(sink:MethodCall {name: 'ExecuteQuery'})35.  
**Applicability to Minotaur:** Cypher is significantly more readable than Datalog and specifically engineered for graph traversal38. Furthermore, the .NET ecosystem contains existing ANTLR-based openCypher parsers (such as openCypherTranspiler), which can parse Cypher text into executable ASTs directly within C\#39.  
However, Cypher still suffers from usability issues when applied strictly to AST pattern matching. While Cypher is excellent at defining node-to-node relationships, expressing complex structural constraints on the internal properties of an AST node (e.g., verifying that the second argument of a specific function call matches a particular regular expression) requires verbose WHERE clauses37. Furthermore, executing variable-length paths in Cypher while simultaneously tracking state mutations (like sanitization) requires highly complex query formulations that frustrate average users41.

### **4.3. The Syntactic Pattern-Matching Paradigm: Semgrep**

Semgrep (Semantic Grep) abandons the database-query mindset entirely in favor of an approach strictly optimized for developer experience and rapid rule creation28. Instead of forcing users to learn Datalog or Cypher, Semgrep allows users to write rules using the exact syntax of the target programming language, augmented with a few simple operators. Rules are defined in standard YAML files43.  
Semgrep relies on two primary operators to achieve this simplicity:

> * **Metavariables ($X):** These act as capture groups that match any valid AST node (a variable, a function name, an expression) and unify that value across the rule44.  
> * **The Ellipsis Operator (...):** This operator matches zero or more arguments, statements, or characters, abstracting away irrelevant syntactic noise28.

For taint analysis, Semgrep brilliantly decouples the *definition* of the vulnerability from the *execution* of the graph traversal. The user never writes a recursive traversal query. Instead, the user simply defines the distinct endpoints of the data flow using YAML keys: pattern-sources, pattern-sinks, pattern-sanitizers, and pattern-propagators9.

YAML  
rules:  
  \- id: generic-injection-prevention  
    mode: taint  
    pattern-sources:  
      \- pattern: get\_untrusted\_input(...)  
    pattern-sinks:  
      \- pattern: execute\_critical\_command($PAYLOAD)  
    pattern-sanitizers:  
      \- pattern: sanitize\_input(...)

Behind the scenes, Semgrep's execution engine (written in OCaml) takes these user-defined anchor points, maps them to the parsed AST, and utilizes its own highly optimized, internal fixed-point algorithms to traverse the data flow graph, track taint states, and unify metavariables across function calls47.  
**Applicability to Minotaur:** The Semgrep model is the absolute pinnacle of usability for SAST. By writing rules that look identical to the code they are analyzing, the learning curve is effectively reduced to zero. Developers can author highly accurate security policies in minutes rather than weeks28. Adopting the syntactic pattern-matching paradigm is the optimal path for fulfilling the requirement that the rule language be "easy for users to learn."

| SAST Paradigm | Rule Syntax Complexity | Traversal Execution Model | Applicability to CognitiveGraph |
| :---- | :---- | :---- | :---- |
| **CodeQL (Datalog)** | High (Logical Predicates, Relational Algebra) | External Relational Engine | Low. Requires massive ecosystem shift and steep learning curve. |
| **Cypher** | Medium (Graph Topologies, ASCII Art) | In-Memory Graph Query Engine | Medium. Good for structural queries, verbose for syntax matching. |
| **Semgrep (Syntactic)** | **Very Low** (Target Language Syntax \+ YAML) | Internal Fixed-Point Data Flow Engine | **High**. Decouples rule authoring from complex graph traversal logic. |

## **5\. Architectural Blueprint: A Native .NET Rule Engine for CognitiveGraph**

Synthesizing the analysis of existing paradigms, the optimal architecture for the Minotaur project must adopt the developer experience of Semgrep (syntactic pattern matching via YAML) and pair it with a highly optimized, native .NET execution engine utilizing C\# Expression Trees to operate directly on the CognitiveGraph.  
This architecture is founded on the principle of **Separation of Rule Definition from Traversal Execution**. By abstracting the complex recursive graph mathematics away from the user, the system achieves both extreme usability and extreme performance.

### **5.1. Layer 1: The Rule Definition Schema (YAML \+ Custom Grammar)**

The entry point for the rule engine will be a standardized YAML schema. YAML is universally understood, human-readable, and integrates flawlessly into modern CI/CD pipelines and infrastructure-as-code repositories.  
Because the CognitiveGraph operates on code specified by arbitrary, custom grammar files, the rule engine cannot rely on a fixed parser like Roslyn. Instead, the engine must leverage the existing DevelApp.StepLexer and DevelApp.StepParser pipelines to parse the rule definitions1.  
The rule schema will require users to define the anchors for taint analysis—sources, sinks, sanitizers, and propagators—using raw snippets of the target grammar. The StepLexer must be modified slightly to recognize Semgrep-style metavariables (e.g., tokens starting with $) and ellipsis operators (...) as valid token types within any custom grammar.  
A standard rule definition for the Minotaur engine will look like this:

YAML  
rules:  
  \- id: custom-grammar-injection  
    severity: ERROR  
    message: "Untrusted data flows into a critical execution sink."  
    type: taint  
    sources:  
      \- pattern: "ReceiveData($DATA)"  
    sinks:  
      \- pattern: "ExecuteAction(..., $DATA, ...)"  
    sanitizers:  
      \- pattern: "VerifyIntegrity($DATA)"  
    propagators:  
      \- pattern: "$TARGET \= FormatString($SRC)"  
        from: $SRC  
        to: $TARGET

This fulfills the primary user requirement: it is incredibly easy to learn. The user only needs to know the syntax of the language they are securing. They define *what* they are looking for, completely ignoring *how* the engine will traverse the CognitiveGraph to connect those points.

### **5.2. Layer 2: Rule Compilation via C\# Expression Trees**

To fulfill the requirement that the engine must be "very efficient," it cannot rely on interpreting the YAML rules at runtime, nor can it afford the overhead of serializing the CognitiveGraph to an external engine. The rules must be compiled into native .NET executable code.  
When the Minotaur rule engine initializes, it will execute a compilation phase:

> 1. The engine reads the YAML rule files.  
> 2. The engine extracts the code snippet strings defined in the pattern fields.  
> 3. The engine feeds these snippet strings into the DevelApp.StepParser, which parses them into mini-AST structures1.  
> 4. The engine translates these mini-AST structures into **C\# Expression Trees** (System.Linq.Expressions).

Expression Trees represent code as a tree of objects that can be compiled dynamically at runtime3. The engine will construct an Expression Tree that evaluates whether a given node in the full CognitiveGraph matches the structure defined by the mini-AST pattern.  
For example, a rule pattern ReceiveData($DATA) would be translated into an Expression Tree equivalent to this logical lambda:  
node \=\> node.Type \== "FunctionCall" && node.Name \== "ReceiveData"  
Crucially, once the Expression Tree is built, it is compiled into a highly optimized, native C\# delegate (Func\<CognitiveNode, bool\>)50. By compiling the rule matching logic directly into native delegates, the engine bypasses all interpreter overhead. Evaluating whether a specific node in a multi-million-node CognitiveGraph is a "source" or a "sink" becomes a direct memory access and a compiled function call, executing in nanoseconds52. This entirely eliminates the latency penalties associated with OPA Rego and WASM integration4.

### **5.3. Layer 3: Native C\# Data Flow and Fixed-Point Traversal**

With the user-defined rules dynamically compiled into lightning-fast C\# delegates, the engine requires a mechanism to actually traverse the CognitiveGraph and perform the taint analysis.  
Rather than relying on an external Datalog engine, the Minotaur project must implement a native **Fixed-Point Worklist Algorithm** in C\#. This algorithm will operate directly on the memory references of the CognitiveGraph, ensuring maximum CPU cache coherence and zero serialization overhead.  
The execution flow of the native C\# traversal engine will proceed as follows:

> 1. **Source Identification:** The engine iterates over the nodes of the CognitiveGraph, applying the compiled Source delegates. If a delegate returns true, that node is instantiated with a TaintState object and pushed into a highly concurrent Worklist queue48.  
> 2. **Propagation and Traversal:** The engine enters a while loop, continuously dequeuing nodes from the Worklist. For each node, it examines all outgoing edges defined in the Data Flow Graph (DFG).  
> 3. **Metavariable Unification:** As the engine traverses, it must maintain a dictionary mapping metavariables (like $DATA) to their concrete AST node instances. This allows the engine to accurately track specific variables across complex assignments45. To minimize Garbage Collection (GC) pressure, this dictionary should be implemented using C\# structs or object pooling.  
> 4. **Sanitizer Evaluation:** Before propagating the taint state across an edge to a child node, the engine invokes the compiled Sanitizer delegates on the child node. If a sanitizer matches, the traversal along that specific path is immediately halted, pruning the graph5.  
> 5. **Propagator Application:** If the child node matches a compiled Propagator delegate (e.g., a function that modifies a variable), the TaintState is transferred or duplicated to the new target node defined by the propagator9.  
> 6. **Sink Evaluation:** If a tainted node reaches a child node that triggers a compiled Sink delegate, a vulnerability is confirmed. The engine records the full path from the source to the sink for reporting purposes10.  
> 7. **Fixed-Point Re-evaluation:** If a node's TaintState is updated (e.g., it receives a new taint flag it did not previously possess), it is re-added to the Worklist. The loop naturally terminates when the Worklist is empty, guaranteeing that all loops, mutual recursions, and complex control flows have been exhaustively evaluated until a fixed point of equilibrium is reached7.

Because this entire algorithmic pipeline runs within the .NET CLR, operating on native object pointers, it will achieve maximum possible throughput, satisfying the strict efficiency requirements of the project.

### **5.4. Advanced Extensibility via Dynamic LINQ**

While the YAML-based syntactic pattern matching will accommodate the vast majority of SAST use cases, advanced security researchers frequently require the ability to define highly complex, programmatic constraints that cannot be easily expressed via simple syntax snippets.  
To provide an escape hatch for these advanced users without forcing them to write compiled C\# code, the architecture can integrate **Dynamic LINQ** (System.Linq.Dynamic.Core)52. Dynamic LINQ allows developers to embed raw, string-based C\# logical expressions directly into the YAML configuration files.

YAML  
rules:  
  \- id: advanced-size-check  
    type: search  
    pattern: "AllocateMemory($SIZE)"  
    condition: "node.Arguments\[0\].Value \> 1024 && node.Parent.Type \!= 'SafeBlock'"

During the initialization phase, the engine will pass the condition string to the Dynamic LINQ library, which parses the string and seamlessly appends it to the Expression Tree before compilation3. This provides rule authors with the full reflection and logical power of the C\# language directly within the declarative YAML file, ensuring that the rule engine remains infinitely extensible.

## **6\. Strategic Conclusions and Recommendations**

The challenge of engineering a standardized rule creation language for a custom AST replacement like the CognitiveGraph requires carefully aligning the computational model of the rule language with the mathematical realities of static application security testing.  
Based on an exhaustive architectural analysis, the following strategic conclusions must drive the development of the Minotaur rule engine:

> 1. **Discard OPA Rego for SAST:** Open Policy Agent, while excellent for JSON-based authorization, is architecturally unfit for data flow analysis. Rego's inherent restrictions on recursion prevent the expression of the stateful, fixed-point algorithms required for taint tracking. Furthermore, executing Rego via WebAssembly imposes a catastrophic serialization tax that will cripple the performance of the in-memory CognitiveGraph. Building a native C\# Rego implementation mitigates the serialization tax but requires immense engineering effort without resolving the fundamental algorithmic and usability limitations of the language.  
> 2. **Adopt a Semgrep-Inspired Declarative Interface:** To satisfy the requirement for extreme usability, the rule engine must decouple the complexity of graph traversal from the rule definition. The system should utilize a YAML-based schema where users define sources, sinks, and sanitizers using exact syntax snippets of the target grammar, augmented by metavariables and ellipsis operators. This reduces the learning curve to near zero.  
> 3. **Compile Rules via C\# Expression Trees:** To guarantee exceptional performance, the engine must leverage System.Linq.Expressions to dynamically compile the parsed YAML patterns into native C\# delegates at runtime, eliminating all interpretation overhead during the analysis phase.  
> 4. **Execute Traversal Natively:** The core data flow analysis must be executed by a custom, native C\# fixed-point worklist algorithm operating directly on the CognitiveGraph memory references. This ensures flawless interprocedural analysis, minimal garbage collection pressure, and execution speeds capable of supporting real-time or CI/CD-bound security scanning.

By executing this architectural blueprint, the Minotaur project will bypass the limitations of existing enterprise SAST tools, delivering a security engine that offers both the extreme flexibility of custom grammar analysis and the frictionless developer experience required for widespread adoption.

#### **Citerede værker**

> 1. DevelApp.StepParser 1.21.0 \- NuGet Gallery, [https://www.nuget.org/packages/DevelApp.StepParser/1.21.0](https://www.nuget.org/packages/DevelApp.StepParser/1.21.0)  
> 2. DevelApp.StepLexer 1.0.1-ci0072 \- NuGet Gallery, [https://www-1.nuget.org/packages/DevelApp.StepLexer/1.0.1-ci0072](https://www-1.nuget.org/packages/DevelApp.StepLexer/1.0.1-ci0072)  
> 3. When to Use Interpreter Pattern in C\#: Decision Guide with Examples, [https://www.devleader.ca/2026/06/22/when-to-use-interpreter-pattern-in-c-decision-guide-with-examples](https://www.devleader.ca/2026/06/22/when-to-use-interpreter-pattern-in-c-decision-guide-with-examples)  
> 4. C\# 10 in a Nutshell Supplement \- Joseph Albahari, [https://www.albahari.com/nutshell/cs10ian-supplement.pdf](https://www.albahari.com/nutshell/cs10ian-supplement.pdf)  
> 5. Why Data Flow Analysis Is the Gold Standard for Vulnerability, [https://graphnodesoftware.com/blog/why-data-flow-analysis-matters](https://graphnodesoftware.com/blog/why-data-flow-analysis-matters)  
> 6. 12 Questions and Answers About CodeQL (GitHub) \- Security Scientist, [https://www.securityscientist.net/blog/12-questions-and-answers-about-codeql-github/](https://www.securityscientist.net/blog/12-questions-and-answers-about-codeql-github/)  
> 7. Building a Rule-Based SAST Engine \- Grasp, [https://paths.grasp.study/modules/ab91a0a4-e77e-41fe-b218-ea72d1e9640c/lessons/f5062979-6352-4a35-9a05-227d15842078](https://paths.grasp.study/modules/ab91a0a4-e77e-41fe-b218-ea72d1e9640c/lessons/f5062979-6352-4a35-9a05-227d15842078)  
> 8. Static analysis and rule-writing glossary \- Semgrep Docs, [https://docs.semgrep.dev/writing-rules/glossary](https://docs.semgrep.dev/writing-rules/glossary)  
> 9. Demystifying Taint Mode \- Semgrep, [https://semgrep.dev/blog/2022/demystifying-taint-mode/](https://semgrep.dev/blog/2022/demystifying-taint-mode/)  
> 10. Hunting Vulnerabilities with CodeQL: A Hands-On Introduction, [https://medium.com/@waeel.nono3719876/hunting-vulnerabilities-with-codeql-a-hands-on-introduction-17fd686dfb72](https://medium.com/@waeel.nono3719876/hunting-vulnerabilities-with-codeql-a-hands-on-introduction-17fd686dfb72)  
> 11. A Database of Code aka Advanced Metaprogramming Queries, [http://hpts.ws/papers/2022/HPTS22\_AnnaHerlihy\_Pub.pdf](http://hpts.ws/papers/2022/HPTS22_AnnaHerlihy_Pub.pdf)  
> 12. Implementing Policies with OPA — Example Use Cases \- Medium, [https://medium.com/@chathuragunasekera/implementing-policies-with-opa-example-use-cases-6f8f850cdec4](https://medium.com/@chathuragunasekera/implementing-policies-with-opa-example-use-cases-6f8f850cdec4)  
> 13. Policy-Based Access Control (PBAC): Debunking the Myths, [https://www.golodiuk.com/news/pbac-debunking-myths/](https://www.golodiuk.com/news/pbac-debunking-myths/)  
> 14. Arbitrary recursion using dynamic data references \#1565 \- GitHub, [https://github.com/open-policy-agent/opa/issues/1565](https://github.com/open-policy-agent/opa/issues/1565)  
> 15. How to Build Complex OPA Rego Functions \- OneUptime, [https://oneuptime.com/blog/post/2026-02-02-opa-rego-functions/view](https://oneuptime.com/blog/post/2026-02-02-opa-rego-functions/view)  
> 16. Advanced taint analysis techniques \- Semgrep Docs, [https://docs.semgrep.dev/writing-rules/data-flow/taint-mode/advanced](https://docs.semgrep.dev/writing-rules/data-flow/taint-mode/advanced)  
> 17. Policy Compiler for Secure Agentic Systems \- arXiv, [https://arxiv.org/html/2602.16708v1](https://arxiv.org/html/2602.16708v1)  
> 18. Wasm Integrations OPA Ecosystem Projects \- Open Policy Agent, [https://openpolicyagent.org/ecosystem/by-feature/wasm-integration](https://openpolicyagent.org/ecosystem/by-feature/wasm-integration)  
> 19. OpaDotNet.Wasm 3.1.0 \- NuGet, [https://www.nuget.org/packages/OpaDotNet.Wasm/](https://www.nuget.org/packages/OpaDotNet.Wasm/)  
> 20. Design of a Medical IT Automated Auditing System Based on, [https://www.paradigmpress.org/ist/article/download/1836/1675](https://www.paradigmpress.org/ist/article/download/1836/1675)  
> 21. Integrating DCR Graphs with Azure Cosmos DB and Runtime IL, [https://rucforsk.ruc.dk/ws/files/109159944/From\_XML\_to\_Execution\_Integrating\_DCR\_Graphs\_with\_Azure\_Cosmos\_DB\_and\_Runtime\_IL.pdf](https://rucforsk.ruc.dk/ws/files/109159944/From_XML_to_Execution_Integrating_DCR_Graphs_with_Azure_Cosmos_DB_and_Runtime_IL.pdf)  
> 22. Super-Scaling Open Policy Agent with Batch Queries \- KubeFM, [https://kube.fm/scaling-opa-nicholaos](https://kube.fm/scaling-opa-nicholaos)  
> 23. The Benchmark Bake-Off: Which Runtime Actually Wins in 2025?, [https://medium.com/the-rise-of-device-independent-architecture/the-benchmark-bake-off-which-runtime-actually-wins-in-2025-ebf69ec5a080](https://medium.com/the-rise-of-device-independent-architecture/the-benchmark-bake-off-which-runtime-actually-wins-in-2025-ebf69ec5a080)  
> 24. Analyzing the Performance of WebAssembly vs. Native Code \- arXiv, [https://arxiv.org/html/1901.09056v3](https://arxiv.org/html/1901.09056v3)  
> 25. Securing AI-generated code with Cloudsmith, [https://cloudsmith.com/blog/securing-ai-generated-code-with-cloudsmith](https://cloudsmith.com/blog/securing-ai-generated-code-with-cloudsmith)  
> 26. Beyond Static Enforcement: Why Policy-as-Code Needs ... \- TechRxiv, [https://www.techrxiv.org/doi/pdf/10.36227/techrxiv.176945782.27826416/v1](https://www.techrxiv.org/doi/pdf/10.36227/techrxiv.176945782.27826416/v1)  
> 27. InsightQL: Advancing Human-Assisted Fuzzing with a Unified Code, [https://arxiv.org/html/2510.04835v1](https://arxiv.org/html/2510.04835v1)  
> 28. Semgrep vs CodeQL (2026): Fast Scans vs Deeper Analysis \- Konvu, [https://konvu.com/compare/semgrep-vs-codeql](https://konvu.com/compare/semgrep-vs-codeql)  
> 29. Incrementalizing Production CodeQL Analyses \- arXiv, [https://arxiv.org/html/2308.09660v1](https://arxiv.org/html/2308.09660v1)  
> 30. Using Datalog for Fast and Easy Program Analysis, [https://yanniss.github.io/doop-datalog2.0.pdf](https://yanniss.github.io/doop-datalog2.0.pdf)  
> 31. Datalog | Hey There Buddo\! \- Philip Zucker, [https://www.philipzucker.com/notes/Languages/datalog/](https://www.philipzucker.com/notes/Languages/datalog/)  
> 32. Not sure about Prolog itself but Datalog really needs to overtake, [https://news.ycombinator.com/item?id=40994870](https://news.ycombinator.com/item?id=40994870)  
> 33. Rule Writing for CodeQL and Semgrep \- Spaceraccoon's Blog, [https://spaceraccoon.dev/comparing-rule-syntax-codeql-semgrep/](https://spaceraccoon.dev/comparing-rule-syntax-codeql-semgrep/)  
> 34. Design and Implementation of the LogicBlox System, [https://www.cs.ox.ac.uk/dan.olteanu/papers/logicblox-sigmod15.pdf](https://www.cs.ox.ac.uk/dan.olteanu/papers/logicblox-sigmod15.pdf)  
> 35. The Neo4j Graph Platform \- Overview of Neo4j 4.x, [https://neo4j.com/graphacademy/training-overview-40/02-overview40-neo4j-graph-platform/](https://neo4j.com/graphacademy/training-overview-40/02-overview40-neo4j-graph-platform/)  
> 36. Raqlet: Cross-Paradigm Compilation for Recursive Queries \- arXiv, [https://arxiv.org/html/2508.03978](https://arxiv.org/html/2508.03978)  
> 37. What is Datalog? Declarative Graph Query Language Guide, [https://terminusdb.org/docs/what-is-datalog/](https://terminusdb.org/docs/what-is-datalog/)  
> 38. A type-safe, realtime collaborative Graph Database in a CRDT, [https://news.ycombinator.com/item?id=47846946](https://news.ycombinator.com/item?id=47846946)  
> 39. microsoft/openCypherTranspiler: This tool transpiles openCypher, [https://github.com/microsoft/openCypherTranspiler](https://github.com/microsoft/openCypherTranspiler)  
> 40. Using Cypher without Neo4j \- Stack Overflow, [https://stackoverflow.com/questions/45032551/using-cypher-without-neo4j](https://stackoverflow.com/questions/45032551/using-cypher-without-neo4j)  
> 41. Benchmarking Database Systems for Graph Pattern Matching, [https://www.researchgate.net/publication/267450216\_Benchmarking\_Database\_Systems\_for\_Graph\_Pattern\_Matching](https://www.researchgate.net/publication/267450216_Benchmarking_Database_Systems_for_Graph_Pattern_Matching)  
> 42. Semgrep: a static analysis journey, [https://semgrep.dev/blog/2021/semgrep-a-static-analysis-journey/](https://semgrep.dev/blog/2021/semgrep-a-static-analysis-journey/)  
> 43. Semgrep vs CodeQL: Static Analysis for Security Teams \- Safeguard, [https://safeguard.sh/resources/blog/semgrep-codeql-sast-comparison](https://safeguard.sh/resources/blog/semgrep-codeql-sast-comparison)  
> 44. Semgrep 101: Creating SAST Rules for Your Codebase \- Medium, [https://medium.com/devsecops-ai/semgrep-101-creating-sast-rules-for-your-codebase-e3147f339e55](https://medium.com/devsecops-ai/semgrep-101-creating-sast-rules-for-your-codebase-e3147f339e55)  
> 45. Rule pattern syntax \- Semgrep Docs, [https://docs.semgrep.dev/writing-rules/pattern-syntax](https://docs.semgrep.dev/writing-rules/pattern-syntax)  
> 46. Semgrep Writing Rule Tutorial (DOM-Based XSS) \- DEV Community, [https://dev.to/takutoy/semgrep-writing-rule-tutorial-dom-based-xss-2jh0](https://dev.to/takutoy/semgrep-writing-rule-tutorial-dom-based-xss-2jh0)  
> 47. semgrep/AGENTS.md at develop \- GitHub, [https://github.com/semgrep/semgrep/blob/develop/AGENTS.md](https://github.com/semgrep/semgrep/blob/develop/AGENTS.md)  
> 48. How Semgrep Cut Taint Analysis Time by 75%, [https://semgrep.dev/blog/2026/how-we-cut-semgreps-taint-analysis-time-by-75-percent/](https://semgrep.dev/blog/2026/how-we-cut-semgreps-taint-analysis-time-by-75-percent/)  
> 49. Static Code Analysis at Scale: Finding Bugs and Enforcing, [https://www.blog.brightcoding.dev/2025/10/02/static-code-analysis-at-scale-finding-bugs-and-enforcing-standards-in-30+-languages-with-semgrep](https://www.blog.brightcoding.dev/2025/10/02/static-code-analysis-at-scale-finding-bugs-and-enforcing-standards-in-30+-languages-with-semgrep)  
> 50. Are there any programming language interpreters written in C\#? : r, [https://www.reddit.com/r/csharp/comments/1thfx3c/are\_there\_any\_programming\_language\_interpreters/](https://www.reddit.com/r/csharp/comments/1thfx3c/are_there_any_programming_language_interpreters/)  
> 51. AmirSasson/NetRuleEngine: High performance C\# Rule Engine, [https://github.com/AmirSasson/NetRuleEngine](https://github.com/AmirSasson/NetRuleEngine)  
> 52. System.Linq.Dynamic.Core vs DynamicExpresso \- LibHunt, [https://www.libhunt.com/compare-System.Linq.Dynamic.Core-vs-DynamicExpresso](https://www.libhunt.com/compare-System.Linq.Dynamic.Core-vs-DynamicExpresso)  
> 53. Pattern syntax (experimental) \- Semgrep Docs, [https://docs.semgrep.dev/writing-rules/experiments/pattern-syntax](https://docs.semgrep.dev/writing-rules/experiments/pattern-syntax)  
> 54. DynamicExpresso vs Remote.Linq \- compare differences ... \- LibHunt, [https://www.libhunt.com/compare-DynamicExpresso-vs-Remote.Linq](https://www.libhunt.com/compare-DynamicExpresso-vs-Remote.Linq)
# Safe .NET expression engines for time-series formulas

Research date: 2026-09-05

## Decision summary

Use **Parlot as a parser toolkit, but own Chrono's grammar, AST, validator, dependency binder, canonical serializer, and time-series evaluator**.

This is a build-and-adopt choice rather than a from-scratch parser or an off-the-shelf evaluator. Parlot already demonstrates the exact core grammar needed here—numbers, precedence, unary minus, `+ - * /`, and parentheses—and constructs an application-owned AST directly ([Parlot arithmetic example](https://github.com/sebastienros/parlot#fluent-api)). Chrono adds only identifiers and allow-listed function calls. The evaluator should interpret that small AST against windowed time-series values; it should never compile or execute user-supplied C#, reflection, member access, or arbitrary delegates.

NCalc is the runner-up if delivery pressure makes even the small custom grammar unattractive. It has a public logical-expression factory and visitor-shaped AST ([`ILogicalExpressionFactory`](https://ncalc.github.io/ncalc/api/NCalc.Factories.ILogicalExpressionFactory.html), [`EvaluationVisitor`](https://ncalc.github.io/ncalc/api/NCalc.Visitors.EvaluationVisitor.html)). If used, Chrono should consume the AST only, exhaustively reject every node/operator/function outside its DSL, and still use its own time-series evaluator. Do not expose NCalc's complete evaluator surface as the product language.

## What the engine must do

The selected approach must support:

- numeric scalars, named time-series references, parentheses, unary signs, and `+ - * /`;
- a deliberately small, typed function registry for `Sum`, `Average`, `Min`, `Max`, and `Count`;
- dependency discovery from the same AST that will be evaluated;
- stable binding from the display name typed by a user to a time-series identity;
- deterministic, versioned interpretation over explicit missing-value and period policies;
- useful source spans and validation errors for the Angular formula editor; and
- safe processing of untrusted formula text with bounded input, AST depth, work, and cancellation.

An ordinary scalar expression engine does not supply Chrono's important semantics: timestamp alignment, sparse versus stepwise values, period compatibility, missing-value policy, windowed execution, or provenance. Adopting an evaluator therefore does not remove the need for a domain evaluator.

## Options compared

| Option | Fit | Dependency and version story | Safety and maintenance observations | Verdict |
|---|---|---|---|---|
| **Own DSL + Parlot** | Its fluent API constructs an arbitrary result type and its official arithmetic sample covers precedence, grouping, and all four required binary operators ([README](https://github.com/sebastienros/parlot#fluent-api)). Adding identifier and call nodes is small. | Chrono owns the AST, so a visitor can collect references before binding them to stable IDs. The AST schema and interpreter can be language-versioned independently of parser-package upgrades. | Parsing only constructs data; it does not evaluate user code. Parlot explicitly documents input limits, cancellation, recursion depth, and result-growth bounds for hostile input ([security guidance](https://github.com/sebastienros/parlot/blob/main/docs/security.md#processing-untrusted-input)). Its repository is active and current documentation includes .NET 10 benchmarks and source-generation support ([README](https://github.com/sebastienros/parlot)). | **Recommended.** Smallest trustworthy language and cleanest long-term provenance model. |
| **NCalc AST + Chrono validator/evaluator** | NCalc handles operators, parameters, built-in and custom functions ([README](https://github.com/ncalc/ncalc)). Its public AST has `BinaryExpression`, `UnaryExpression`, `Identifier`, `Function`, and value/list nodes exposed through a visitor ([API](https://ncalc.github.io/ncalc/api/NCalc.Visitors.EvaluationVisitor.html)). Bracketed parameters are convenient for names containing spaces ([parameter docs](https://github.com/ncalc/ncalc/wiki/parameters#square-brackets-parameters)). | AST walking can discover identifiers. Chrono would still need its own bound AST, canonical serialization, language version, and typed time-series semantics. | NCalc intentionally supports a much larger language than V1 and has extensible functions. Its recent factorial denial-of-service advisory is a useful reminder that expression evaluators still require version patching and resource limits, even without arbitrary-code features ([GHSA-3w5p-95mh-gq75](https://github.com/ncalc/ncalc/security/advisories/GHSA-3w5p-95mh-gq75)). The active project has modern .NET targets and recent releases ([repository](https://github.com/ncalc/ncalc), [releases](https://github.com/ncalc/ncalc/releases)). | **Acceptable runner-up**, but only as parser/AST. More rejection logic and semantic impedance than owning the grammar. |
| **Own DSL + Superpower or Pidgin** | Both are C# parser-combinator libraries. Superpower emphasizes token-based, user-friendly errors and ships an arithmetic expression example ([Superpower README](https://github.com/datalust/superpower#tokenization)); Pidgin includes operator-precedence parsing tools ([Pidgin README](https://github.com/benjamin-hodgson/Pidgin#parsing-expressions)). | Same strong ownership and versioning story as Parlot. | Neither evaluates arbitrary code; the application defines all semantic actions. They are credible fallbacks if the team prefers their diagnostics or API. | **Viable alternatives.** Prefer Parlot because its current arithmetic example is closest to the V1 grammar and its hostile-input guidance is unusually explicit. |
| **Sonic** | A maintained numeric evaluator with variables, arithmetic, statistics, custom functions, validation, interpreted mode, and IL-emitting dynamic mode ([upstream README](https://github.com/adletec/sonic#quick-start)). | Public usage centers on evaluation and delegates rather than an application-owned, persisted AST. Its optimizer can remove a dependency such as `var2` from `var1 + 0 * var2`, and validation follows that optimized view ([optimizer behavior](https://github.com/adletec/sonic#validation)). That is unsuitable as the authoritative provenance graph. | The language is numeric rather than general C#, and defaults can be narrowed, but its functions are scalar functions; they do not provide Chrono's series semantics. Its upstream describes it as the maintained successor to dormant Jace.NET ([migration note](https://github.com/adletec/sonic#migration-from-jacenet)). | **Do not adopt for V1.** Fast scalar evaluation is not the bottleneck or hard problem, and optimized dependency discovery conflicts with provenance. |
| **Dynamic Expresso** | It discovers identifiers and supports the arithmetic subset ([identifier detection](https://github.com/dynamicexpresso/DynamicExpresso#identifiers-detection)). | C# expression trees/delegates are the durable model it creates, not a deliberately small domain AST. Binding identifiers alone is not enough to version time-series semantics. | Its supported language includes `new`, `typeof`, member and method invocation, indexers, assignment, and optionally lambdas ([syntax](https://github.com/dynamicexpresso/DynamicExpresso#syntax-and-operators)). Its own security section warns that exposed .NET types can be accessed and that assignment and reflection need careful restriction ([security](https://github.com/dynamicexpresso/DynamicExpresso#security)). | **Reject.** The attack surface and C# semantics are unnecessary for a five-function numeric DSL. |
| **Own grammar + ANTLR** | Full parser generation, C# target, parse trees, and excellent room for language growth. The official getting-started grammar demonstrates the same arithmetic precedence/grouping core ([ANTLR guide](https://github.com/antlr/antlr4/blob/dev/doc/getting-started.md#lets-play-with-a-simple-grammar)). | Chrono can own a stable AST after translating the generated parse tree. | The generator is a Java tool and the generated C# parser needs the target runtime ([ANTLR architecture](https://github.com/antlr/antlr4/blob/dev/doc/getting-started.md#installation), [targets](https://github.com/antlr/antlr4/blob/dev/doc/targets.md)). This adds generated code and build tooling that a tiny grammar does not yet justify. | **Defer.** Reconsider only if the language grows into conditionals, named arguments, window clauses, or rich diagnostics that strain a combinator grammar. |

Flee/Jace.NET should not be selected. Sonic's maintainers explicitly describe Jace.NET as no longer actively maintained ([source](https://github.com/adletec/sonic#migration-from-jacenet)). A new Flee.NET port exists, but it compiles to IL, exposes fields/properties/functions, uses LGPL dependencies, and currently has essentially no adoption signal ([upstream repository](https://github.com/lufegit/Flee.NET)); none of those trade-offs helps this constrained DSL.

## Proposed V1 shape

### Grammar

Keep grammar V1 intentionally finite:

```text
expression     := additive
additive       := multiplicative (("+" | "-") multiplicative)*
multiplicative := unary (("*" | "/") unary)*
unary          := ("+" | "-") unary | primary
primary        := number | seriesReference | functionCall | "(" expression ")"
functionCall   := allowedFunction "(" arguments ")"
arguments      := expression ("," expression)*
```

Use an unambiguous quoted or bracketed token for names with whitespace. At save time, resolve the typed name inside the workspace and replace it in the bound AST with the stable time-series identity. Display names remain presentation metadata and can change without altering a dependency edge.

The AST should be a closed hierarchy such as `Number`, `SeriesReference`, `Unary`, `Binary`, and `Call`; operators and functions should be enums, not strings or delegates. Both validation and evaluation use exhaustive pattern matches. Unknown syntax is rejected rather than forwarded to a general runtime.

### Stored formula version

Persist these together for each immutable formula definition version:

- original user-facing source text;
- `formulaLanguageVersion` (start at `1`);
- canonical serialized **bound AST** and a deterministic hash;
- every referenced time-series identity and its display name at save time;
- interpreter/semantic version when behavior changes independently of syntax.

Do not reparse historical source with the newest grammar to reproduce an old result. Evaluate its persisted bound AST with the corresponding language/interpreter version. Each calculated time-series version separately records the exact dependency **version IDs** used, as already decided for recalculation and provenance.

### Evaluation boundary

The parser should not know about PostgreSQL, chunks, or `ITimeSeries<T>`. A domain evaluator receives:

1. the validated bound AST;
2. an evaluation window and output period;
3. readers for the exact dependency versions;
4. explicit missing-value/resampling policies; and
5. a cancellation token and work budget.

It evaluates in bounded windows and stages output points. A successful job atomically publishes a complete new time-series version; a failed or cancelled job publishes none. This preserves the previously successful current version.

### Safety baseline

- Cap formula length, token count, AST depth, function arity, dependency count, requested range, and evaluated point count.
- Parse and evaluate with cancellation/deadlines; never rely on cancellation alone for future multi-tenant isolation. Parlot's own guidance makes the same distinction ([security guidance](https://github.com/sebastienros/parlot/blob/main/docs/security.md#processing-untrusted-input)).
- Use invariant numeric parsing and define overflow, division-by-zero, `NaN`, and infinity behavior explicitly.
- Allow only stable series references and known pure functions. No member access, reflection, file/network access, constructors, dynamic loading, or user-provided delegates.
- Fuzz the parser and property-test canonical serialization, dependency extraction, and evaluator determinism.
- Pin and routinely update the parser dependency. Formula semantics must not silently change merely because Parlot changes.

## One unresolved semantic decision

The words `Sum`, `Average`, `Min`, `Max`, and `Count` are not enough to define a result type. They could mean an element-by-element aggregate across several input series, a scalar reduction across the full time range, or a bucket/window aggregation that yields another series. Since every derived definition here produces a time series, V1 should either:

- define these as element-by-element aggregates across input series; or
- introduce an explicit bucket/window argument for temporal aggregation.

Do not let a third-party evaluator decide this accidentally. Record the function signatures and missing-value behavior in the formula-language decision/ADR before implementation.

## Suggested proof spike

Before committing the architecture, implement a small Parlot spike with source spans and these acceptance checks:

- parses and canonicalizes `Average([Grid import], [Solar]) * 1.2`;
- resolves both names to stable IDs and reports both dependencies from the bound AST;
- rejects an unknown function, member access, comparisons, assignment, malformed numeric input, excessive nesting, and trailing tokens;
- round-trips the bound AST through JSON without changing its deterministic hash;
- evaluates a small pair of sparse series under each chosen missing-value policy; and
- proves that renaming a dependency changes rendering, not the stored dependency identity or historical calculated version.

If this spike reveals unacceptable diagnostic or source-span friction, repeat it with Superpower before falling back to NCalc's AST.

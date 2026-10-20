# Dryv Architecture Review

> Generated: October 2026
> Status: **In Progress** — fix issues one by one, check off when done.

---

## Overview

Dryv's core pipeline is:

**Rule Definition → Rule Detection → Compilation → Translation → Validation**

The issues below are ordered by suggested priority (highest impact / lowest risk first).

---

## Issue 1: Duplicated Async Promise Writing

**Status:** ✅ Resolved
**Risk:** Low (isolated change)
**Files:**
- `Dryv/Translation/JavaScriptTranslator.cs`

**Problem:**
The `.then(function(...){return ...;})` promise-chain pattern was copy-pasted in three
`Visit` methods: `ConditionalExpression`, `MemberExpression`, and `MethodCallExpression`.

**Resolution:**
Extracted a private `WriteAsyncPromiseChain` helper method that encapsulates the
detect → wrap → translate → close cycle. All three call sites now delegate to it
via an `Action` callback for the body translation.

---

## Issue 2: `DryvRuleFinder` Has Too Many Responsibilities

**Status:** ✅ Resolved
**Risk:** Medium
**Files:**
- `Dryv/RuleDetection/DryvRuleFinder.cs`
- `Dryv/RuleDetection/RuleDiscovery.cs` *(new)*
- `Dryv/RuleDetection/RuleTranslationPhase.cs` *(new)*

**Problem:**
This single class (347 lines) handles six concerns:

1. Discovers `DryvRules` instances on types via reflection (`FindRulesOnType`)
2. Walks the model tree to match rules to nodes (`FindRulesInModelTree`)
3. Transposes expressions when rules are inherited (`TransposeExpressions`, `TransposePath`)
4. Compiles rules (calls `DryvCompiler`)
5. Translates rules to JavaScript (the `Translate` method at line 311)
6. Manages two separate caches

Most critically, `ApplyRuleToNode` mixes compilation and translation into a single step —
a rule *finder* should not know about JavaScript translation.

**Resolution:**
Split into three focused classes:

- `RuleDiscovery` — finds `DryvRules` instances on types via reflection (extracted
  `FindRulesOnType`, `GetRulesOfType`, and the per-type discovery cache)
- `RuleTranslationPhase` — owns the `Translate` method and a separate translated-rule
  cache; `DryvTranslator` and `DryvPreloader` now consume this instead of the finder
- `DryvRuleFinder` — reduced to tree walking, node matching, transposition, and
  compilation only; no longer depends on `ITranslator` or `DryvOptions`

---

## Issue 3: `JavaScriptTranslator` Is a God Class (963 lines)

**Status:** 🔴 Open
**Risk:** Medium–High (large surface area)
**Files:**
- `Dryv/Translation/JavaScriptTranslator.cs`

**Problem:**
The class has at least five distinct responsibilities:

- **Expression dispatching** — giant `switch` (lines 150–178)
- **Null comparison logic** — `TryWriteNullComparison`, `TryWriteValueTypeCoalesce`, `WriteValueTypeNullCheck`
- **Async promise generation** — (see Issue 1)
- **Expression injection** — `TryWriteInjectedExpression`, `TryWriteNegation`
- **Member access resolution** — `WriteMember` with its own sub-dispatch

**Fix:**
Break into focused components:

- `ExpressionDispatcher` — routes expressions to handlers (replaces the switch)
- `NullComparisonWriter` — null / value-type semantics
- `AsyncPromiseWriter` — (from Issue 1)
- `MemberAccessWriter` — member access + identifier formatting
- Keep `JavaScriptTranslator` as a thin orchestrator that delegates

---

## Issue 4: `TranslationContext` Is a Mutable Grab-Bag

**Status:** 🔴 Open
**Risk:** Medium (thread safety)
**Files:**
- `Dryv/Translation/TranslationContext.cs`
- `Dryv/Translation/TranslationContextExtensions.cs`

**Problem:**

- `static int ParameterCount` — mutable shared state, thread-safety hazard
- 13+ properties mixed together (model metadata, writer, injection state, async flag, custom data)
- `Clone<T>` creates deep `ParentContext` chains — hard to trace state
- `IsAsync` setter propagates up through `ParentContext` chain (implicit side effects)

**Fix:**

- Split into: `TranslationScope` (model/rule metadata), `CodeWriter` (output), `InjectionContext` (injected expressions/services)
- Replace `static ParameterCount` with an instance counter on the root scope
- Make state immutable where possible; pass new snapshots instead of mutating

---

## Issue 5: `Translator` Base Class Leaks JavaScript Concerns

**Status:** 🔴 Open
**Risk:** Medium
**Files:**
- `Dryv/Translation/Translator.cs` (method `GenerateJavaScriptCode`, struct `GeneratedJavaScriptCode`)
- `Dryv/Translation/TranslationCompiler.cs` (hard-codes `JavaScriptTranslator.TranslateValue`)

**Problem:**
The *abstract* base class contains `GenerateJavaScriptCode` and `GeneratedJavaScriptCode`.
`TranslationCompiler` references `JavaScriptTranslator.TranslateValue` via reflection.
This makes it impossible to add another target language without modifying the base.

**Fix:**

- Move `GenerateJavaScriptCode` out of the abstract base into the JS-specific subclass
  or into a composition-based `ICodeGenerationStrategy`
- `TranslationCompiler` should accept a value-translation delegate instead of
  hard-coding `JavaScriptTranslator`

---

## Issue 6: Deep Inheritance Chain for Rule Types

**Status:** 🔴 Open
**Risk:** Low–Medium
**Files:**
- `Dryv/Rules/DryvRuleDefinition.cs`
- `Dryv/Rules/DryvCompiledRule.cs`
- `Dryv/Rules/DryvTranslatedRule.cs`

**Problem:**

```
DryvRuleDefinition → DryvCompiledRule → DryvTranslatedRule
```

- Copy constructors manually copy every field — **fragile** when new fields are added
- `DryvCompiledRule` doubles as a compiled rule AND a parameter holder (`CreateParameter`),
  violating single responsibility
- A `DryvTranslatedRule` can have a `TranslationError` but no
  `TranslatedValidationExpression` — ambiguous state

**Fix:**
Use composition over inheritance:

```
DryvRule (definition/metadata)
├── CompiledExpressions  (compiled delegates)
└── TranslationOutput?   (JS code + error, nullable/optional)
```

Alternatively, use C# records with `with` expressions for immutable copies.

---

## Issue 7: `ITranslator` Interface Is Too Broad

**Status:** 🔴 Open
**Risk:** Low
**Files:**
- `Dryv/Translation/ITranslator.cs`

**Problem:**
The interface combines:
- High-level API: `Translate(Expression, MemberExpression, DryvRuleDefinition)`
- Low-level visitor: `Translate(Expression, TranslationContext, bool)`
- Formatting utilities: `FormatIdentifier`, `TranslateValue`, `TryWriteTerminal`

Any consumer needing only high-level translation must also see visitor internals.

**Fix:**
Split into:
- `IRuleTranslator` — high-level translation entry point
- `IExpressionWriter` — low-level visitor used internally
- `IValueFormatter` — `FormatIdentifier`, `TranslateValue`

---

## Issue 8: `DryvValidator` and `DryvTranslator` Duplicate Rule Resolution

**Status:** 🔴 Open
**Risk:** Low
**Files:**
- `Dryv/DryvValidator.cs` (lines ~107–121)
- `Dryv/DryvTranslator.cs` (lines ~16–28)

**Problem:**
Both classes independently:
1. Call `ruleFinder.FindValidationRulesInTree(type, RuleType.Validation)` + filter by location
2. Do the same for `RuleType.Disabling`
3. Resolve services via `serviceResolver.GetServices(rule, parameters)`
4. Check enabling expressions
5. Handle `DryvParameters`

**Fix:**
Extract a `RuleResolutionPipeline` that handles: find → filter by location → check
enablement → resolve services. Both `DryvValidator` and `DryvTranslator` consume
the pipeline output.

---

## Issue 9: Expression Injection Uses Hash Codes as Keys

**Status:** 🔴 Open
**Risk:** Low (rare but dangerous)
**Files:**
- `Dryv/Translation/TranslationContextExtensions.cs` (line 43)

**Problem:**

```csharp
var hash = expression.ToString().GetHashCode();
```

`GetHashCode()` is not guaranteed to be unique — hash collisions would silently produce
incorrect JavaScript. The `$$hash$$` placeholder pattern is also fragile.

**Fix:**
Use an incrementing counter per translation session (avoids collisions entirely) and store
expressions in an indexed list rather than a hash-keyed dictionary.

---

## Checklist

- [x] Issue 1 — Extract async promise writing
- [x] Issue 2 — Split `DryvRuleFinder`
- [ ] Issue 3 — Break up `JavaScriptTranslator`
- [ ] Issue 4 — Fix `TranslationContext`
- [ ] Issue 5 — Remove JS leaks from `Translator` base
- [ ] Issue 6 — Flatten rule type hierarchy
- [ ] Issue 7 — Split `ITranslator`
- [ ] Issue 8 — Unify rule resolution
- [ ] Issue 9 — Fix injection key collision risk

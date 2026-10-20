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

**Status:** ✅ Resolved
**Risk:** Medium–High (large surface area)
**Files:**
- `Dryv/Translation/JavaScriptTranslator.cs`
- `Dryv/Translation/NullComparisonWriter.cs` *(new)*
- `Dryv/Translation/AsyncPromiseWriter.cs` *(new)*
- `Dryv/Translation/MemberAccessWriter.cs` *(new)*

**Problem:**
The class has at least five distinct responsibilities:

- **Expression dispatching** — giant `switch` (lines 150–178)
- **Null comparison logic** — `TryWriteNullComparison`, `TryWriteValueTypeCoalesce`, `WriteValueTypeNullCheck`
- **Async promise generation** — (see Issue 1)
- **Expression injection** — `TryWriteInjectedExpression`, `TryWriteNegation`
- **Member access resolution** — `WriteMember` with its own sub-dispatch

**Resolution:**
Extracted three focused writer classes composed by `JavaScriptTranslator`:

- `NullComparisonWriter` — owns `TryWriteNullComparison`, `TryWriteValueTypeCoalesce`,
  `WriteValueTypeNullCheck`, `GetNullComparisonOperand`, `IsNullConstant`, and `WriteOperand`
- `AsyncPromiseWriter` — owns `WriteAsyncPromiseChain`, `TranslateAsyncBooleanChain`,
  and `TranslateAsyncBooleanOperand`
- `MemberAccessWriter` — owns the `WriteMember` method with its sub-dispatch

`JavaScriptTranslator` is now a thinner orchestrator (738 lines, down from 943) that
composes the three writers and retains only expression dispatching, injection, negation,
and method-call translation

---

## Issue 4: `TranslationContext` Is a Mutable Grab-Bag

**Status:** ✅ Resolved
**Risk:** Medium (thread safety)
**Files:**
- `Dryv/Translation/TranslationContext.cs`
- `Dryv/Translation/TranslationContextExtensions.cs`
- `Dryv/Translation/Visitors/AsyncMethodCallFinder.cs`
- `Dryv/Translation/Visitors/AsyncMethodCallModifier.cs`

**Problem:**

- `static int ParameterCount` — mutable shared state, thread-safety hazard
- 13+ properties mixed together (model metadata, writer, injection state, async flag, custom data)
- `Clone<T>` creates deep `ParentContext` chains — hard to trace state
- `IsAsync` setter propagates up through `ParentContext` chain (implicit side effects)

**Resolution:**

- Replaced `static int ParameterCount` with an instance-based `SessionCounter` class
  that lives on the root context and is shared across all clones via `Clone<T>`,
  eliminating the thread-safety hazard
- Replaced the recursive `IsAsync` parent-chain setter with a `SharedAsyncFlag`
  reference object shared by default across clones — setting `IsAsync = true`
  anywhere in the tree is instantly visible to all contexts without recursive mutation
- Added `shareAsyncFlag` parameter to `Clone<T>` so callers that need an independent
  flag (async detection probes in `AsyncMethodCallFinder` and `AsyncMethodCallModifier`)
  can opt out of sharing
- Removed the latent bug where `AsyncMethodCallModifier` set `IsAsync = false` and
  inadvertently reset the parent context's flag via the old recursive setter

---

## Issue 5: `Translator` Base Class Leaks JavaScript Concerns

**Status:** ✅ Resolved
**Risk:** Medium
**Files:**
- `Dryv/Translation/Translator.cs`
- `Dryv/Translation/TranslationCompiler.cs`
- `Dryv/Translation/JavaScriptTranslator.cs`

**Problem:**
The *abstract* base class contains `GenerateJavaScriptCode` and `GeneratedJavaScriptCode`.
`TranslationCompiler` references `JavaScriptTranslator.TranslateValue` via reflection.
This makes it impossible to add another target language without modifying the base.

**Resolution:**

- Moved `GenerateJavaScriptCode`, `GeneratedJavaScriptCode`, and the `TranslationCompiler`
  field out of `Translator` into `JavaScriptTranslator`; the base-class `Translate` overload
  is now `abstract`, keeping the base language-agnostic
- `TranslationCompiler.GenerateTranslationDelegate` now accepts a `MethodInfo
  translateValueMethod` parameter instead of hard-coding `JavaScriptTranslator.TranslateValue`
  via a static field — any future target-language translator can supply its own value formatter

---

## Issue 6: Deep Inheritance Chain for Rule Types

**Status:** ✅ Resolved
**Risk:** Low–Medium
**Files:**
- `Dryv/Rules/DryvRuleDefinition.cs`
- `Dryv/Rules/DryvCompiledRule.cs`
- `Dryv/Rules/DryvTranslatedRule.cs` *(deleted)*
- `Dryv/Rules/TranslationOutput.cs` *(new)*
- `Dryv/Rules/DryvParameterFactory.cs` *(new)*

**Problem:**

```
DryvRuleDefinition → DryvCompiledRule → DryvTranslatedRule
```

- Copy constructors manually copy every field — **fragile** when new fields are added
- `DryvCompiledRule` doubles as a compiled rule AND a parameter holder (`CreateParameter`),
  violating single responsibility
- A `DryvTranslatedRule` can have a `TranslationError` but no
  `TranslatedValidationExpression` — ambiguous state

**Resolution:**

- Eliminated `DryvTranslatedRule` entirely; translation state is now a nullable
  `TranslationOutput` composition property on `DryvCompiledRule` — no more ambiguous
  state (either `Translation` is `null` or it contains a complete result/error)
- Removed all fragile copy constructors from the chain; `DryvCompiledRule.WithTranslation`
  uses `MemberwiseClone()` which automatically picks up new fields, eliminating the
  field-by-field copy maintenance burden
- Extracted `CreateParameter` into a dedicated `DryvParameterFactory` static class,
  so `DryvCompiledRule` is no longer responsible for parameter construction
- Updated `RuleTranslationPhase`, `DryvTranslator`, and all tests to use the new
  composition-based `Translation` property instead of type casts

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
- [x] Issue 3 — Break up `JavaScriptTranslator`
- [x] Issue 4 — Fix `TranslationContext`
- [x] Issue 5 — Remove JS leaks from `Translator` base
- [x] Issue 6 — Flatten rule type hierarchy
- [ ] Issue 7 — Split `ITranslator`
- [ ] Issue 8 — Unify rule resolution
- [ ] Issue 9 — Fix injection key collision risk

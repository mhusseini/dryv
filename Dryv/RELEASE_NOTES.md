# Dryv 10.0.0 — Release Notes

## Highlights

Dryv 10.0 is a major release that overhauls the internal architecture for cleaner separation of concerns, introduces a fluent rule-builder API, replaces hand-maintained PowerShell-generated code with a Roslyn source generator, and broadens platform support to **netstandard2.1**, **net8.0**, **net9.0**, and **net10.0**.

---

## New Features

- **Fluent rule-builder API** — `DryvRuleBuilder<TModel>` enables a chainable configuration style for rules: `.Validate(…).Disable(…).EvaluateOn(…).Group(…).Name(…).RelatedTo(…).Annotate(…)`.
- **Roslyn source generator (`Dryv.Generators`)** — replaces the three PowerShell scripts (`GenerateRuleMethods.ps1`, `GenerateRuleParameters.ps1`, `GenerateRuleServer.ps1`) and their checked-in `.g.cs` files with a compile-time incremental generator that emits `DryvRules`, server rule, parameter, and builder overloads automatically.
- **`IDryvServiceResolver` abstraction** — decouples the core library from `IServiceProvider`, making it usable outside of ASP.NET Core with any DI container.
- **`DryvCoreSetup`** — framework-agnostic service registration API. Configures default translators and wires up singletons without depending on `Microsoft.Extensions.DependencyInjection`.
- **`DryvValidationCache`** — caches grouped validation/disabling rules per model type to avoid repeated rule discovery on every validation call.
- **`RuleResolutionPipeline`** — unified pipeline for resolving parameters and filtering enabled rules, used by both `DryvValidator` and `DryvTranslator`.
- **`RuleDiscovery` & `RuleTranslationPhase`** — extracted from `DryvRuleFinder` to separate rule discovery from translation preparation.
- **`IDryvClientValidationSetWriter` & `IDryvClientValidationFunctionWriter`** — new interfaces for pluggable client-side output formatting.
- **Pluggable translator registration** — translators are registered via `DryvOptions.TranslatorType` or the `options.Translators` collection, enabling custom translator pipelines (e.g. JSON Schema).

## Architecture & Refactoring

- **Issue 1 — Duplicated Async Promise Writing** → extracted `AsyncPromiseWriter` as a dedicated component.
- **Issue 2 — `DryvRuleFinder` had too many responsibilities** → split into `DryvRuleFinder`, `RuleDiscovery`, and `RuleTranslationPhase`.
- **Issue 3 — `JavaScriptTranslator` god class (963 lines)** → decomposed into focused writer components: `MemberAccessWriter`, `NullComparisonWriter`, `IExpressionWriter`, `IValueFormatter`.
- **Issue 4 — `TranslationContext` was a mutable grab-bag** → reduced mutability and clarified ownership.
- **Issue 5 — Translator base class leaked JavaScript concerns** → `Translator` now receives `DryvOptions` via constructor injection; JavaScript-specific logic isolated.
- **Issue 6 — Deep inheritance chain for rule types** → `DryvCompiledRule` split into phase-specific types; new `DryvRuleDefinition` and `TranslationOutput` value objects.
- **Issue 7 — `ITranslator` interface was too broad** → introduced `IRuleTranslator` for rule-level translation, keeping `ITranslator` focused.
- **Issue 8 — `DryvValidator` and `DryvTranslator` duplicated rule resolution** → shared logic extracted into `RuleResolutionPipeline`.
- **Issue 9 — Expression injection used hash codes as keys** → replaced with deterministic key strategy in `ExpressionInjectionHelper`.
- **M1** — replaced dynamic dispatch with pattern-match `switch` expressions.
- **M2** — moved static caches to instance-level on singletons for testability and isolation.
- **M3** — split `DryvCompiledRule` into phase-specific types.
- **M4/M5** — upgraded `LangVersion` to `latest`; resolved all TODOs and removed dead code.
- Converted classes to **primary constructors** where applicable.

## Breaking Changes

- **Target framework changed** — from `net9.0` only to multi-target `netstandard2.1;net8.0;net9.0;net10.0`.
- **Version bump** — `9.7.6` → `10.0.0`.
- **Removed generated files** — `DryvRules`.g.cs`, `DryvRules`.parameters.g.cs`, `DryvRules`.server.g.cs` and their PowerShell generators are gone; the new source generator produces equivalent code at compile time.
- **`ITranslator` interface narrowed** — consumers implementing custom translators may need to adjust.
- **`DryvCompiledRule` restructured** — split into multiple types; code referencing internal rule properties directly may need updating.
- **Service resolution** — `Func<Type, object>` parameters replaced with `IDryvServiceResolver` in core APIs. Existing delegate-based call sites can use `DryvServiceResolverFactory.FromFunc(…)`.
- **`DryvRuleFinder` API changes** — rule discovery methods moved to `RuleDiscovery`; translation preparation moved to `RuleTranslationPhase`.
- **`TranslationContext` property changes** — reduced mutability; some previously public setters are now read-only.

## Dependencies

- `System.Collections.Immutable` upgraded from `9.0.2` → `10.0.13`.
- `LangVersion` upgraded from `8` → `latest`.

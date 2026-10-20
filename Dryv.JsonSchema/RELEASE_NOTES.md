# Dryv.JsonSchema 10.0.0 — Release Notes

## Highlights

**Dryv.JsonSchema is a brand-new package** that translates Dryv validation rules into JSON Schema (draft 2020-12) constraint fragments. It enables using Dryv's C# validation rules to generate standards-compliant JSON Schema documents for use with any JSON Schema-aware tooling, form generators, or API documentation.

---

## Features

- **`JsonSchemaTranslator`** — a Dryv translator that converts C# validation expressions into JSON Schema keywords (`minLength`, `maxLength`, `pattern`, `minimum`, `maximum`, `required`, `const`, `minItems`, `maxItems`, etc.).
- **`ConstraintExtractor`** — walks expression trees and extracts JSON Schema constraints from the standard Dryv ternary pattern (`failureCondition ? "error" : Success` and its inverted form).
- **`SchemaBuilder`** — wraps per-rule constraint fragments into per-property schema objects and merges them into a complete JSON Schema document.
- **`FormatHints`** — detects well-known regex patterns (email, URI, date, IPv4) and adds the corresponding `format` keyword.
- **`JsonSchemaFunctionWriter` & `JsonSchemaSetWriter`** — pluggable writer components for JSON Schema output formatting.
- **`ExpressionHelpers` & `ExpressionVisitors`** — utilities for expression tree analysis specific to schema extraction.

### Supported Constraints

| C# Pattern | JSON Schema Output |
|---|---|
| Null checks | `required` |
| `string.IsNullOrEmpty` / `IsNullOrWhiteSpace` | `minLength: 1` |
| `.Length` comparisons | `minLength` / `maxLength` |
| Numeric comparisons | `minimum` / `maximum` / `exclusiveMinimum` / `exclusiveMaximum` |
| `Regex.IsMatch` | `pattern` / `not: { pattern }` |
| `.Contains()` / `.StartsWith()` / `.EndsWith()` | `pattern` / `not: { pattern }` |
| String/numeric equality | `const` |
| Boolean properties | `const: true` / `const: false` |
| Collection `.Count` / `.Length` | `minItems` / `maxItems` |
| Logical `&&` / `||` combinators | Merged constraints |
| Negation (`!`) | Inverted extraction path |

### Target Frameworks

`net8.0`, `net9.0`, `net10.0`

## Known Limitations

- No `oneOf` / `anyOf` / `allOf` composition — logical OR is merged best-effort.
- Only compile-time constant comparisons are extracted; runtime/DI values are skipped.
- Enum comparisons are not currently mapped to the `enum` keyword.

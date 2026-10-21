<p align="center">
  <img src="../logo_slogan_light.svg" title="Dryv - DRY Validation for ASP.NET" width="300">
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Dryv"><img src="https://img.shields.io/nuget/v/Dryv.svg" alt="NuGet"></a>
  <a href="https://github.com/mhusseini/dryv/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
</p>

<p align="center">
  <strong>Core library for DRY Validation — write model validation rules in C# once, get JavaScript for the client automatically.</strong>
</p>

---

This package provides the framework-agnostic foundation for Dryv. For ASP.NET Core integration (MVC filters, Tag Helpers, dynamic controllers), see [`Dryv.AspNetCore`](https://www.nuget.org/packages/Dryv.AspNetCore).


## Why Dryv?

In a typical web application, validation logic inevitably gets duplicated:
- **Server-side validation (C#)** is absolutely mandatory for security and data integrity.
- **Client-side validation (JavaScript)** is essential for providing instant user feedback and a snappy UI.

For straightforward rules (like `[Required]` or `[StringLength]`), ASP.NET Core's built-in Data Annotations do the job perfectly and translate natively to jQuery Validate. But the moment you need **complex validation**—like cross-property checks, conditional rules, or database lookups—you hit a wall. To solve this, developers usually have to:
1. Write a custom `ValidationAttribute`, use `IValidatableObject`, or integrate a library like `FluentValidation` for the backend.
2. Manually write custom JavaScript or wire up tedious AJAX calls for the frontend.

**Dryv breaks this cycle by making C# the definitive source of truth.** You write your complex business rules exactly once using C# expression trees. Dryv compiles and executes them on the server, and more importantly, it **translates them into pure JavaScript functions** that run instantly on the client.

## Installation

```shell
dotnet add package Dryv
```

## What's Included

- **Rule definition** via the fluent `DryvRules<TModel>` builder (`Rule`, `ServerRule`, `DisableRules`, `Parameter`).
- **Server-side validation** via `DryvValidator`.
- **C#-to-JavaScript translation** via `DryvTranslator` and `JavaScriptTranslator`.
- **Rule discovery** — automatic scanning of model types, interfaces, base classes, and attributed properties for `DryvRules` fields/properties/methods.
- **Built-in translators** for `string` methods, `Regex`, LINQ (`Any`, `All`, `Where`, `Select`, `Count`, …), enums, `DateTime`, `DryvValidationResult`, and more.
- **Dependency injection** support — inject up to 5 services into validation expressions.
- **Async rules** — rules can return `Task<DryvValidationResult>`.

## Quick Example

```csharp
using Dryv;

public class Address
{
    public static readonly DryvRules Rules = DryvRules
        .For<Address>()
        .Rule(
            a => a.City,
            a => string.IsNullOrWhiteSpace(a.City)
                ? "Please enter a city."
                : null)
        .Rule(
            a => a.ZipCode,
            a => string.IsNullOrWhiteSpace(a.ZipCode)
                ? "Please enter a ZIP code."
                : null)
        .Rule(
            a => a.ZipCode,
            a => a.ZipCode.Trim().Length < 5
                ? "ZIP code must have at least 5 characters."
                : null);

    public string City { get; set; }
    public string ZipCode { get; set; }
}
```

### Dependency Injection in Rules

```csharp
.Rule<IOptions<MySettings>>(
    m => m.Name,
    (m, options) => m.Name == options.Value.ForbiddenName
        ? "This name is not allowed"
        : null)
```

### Multi-Property Rules

```csharp
.Rule(
    m => m.Street, m => m.City, m => m.Zip,
    m => string.IsNullOrWhiteSpace(m.Street)
      || string.IsNullOrWhiteSpace(m.City)
      || string.IsNullOrWhiteSpace(m.Zip)
        ? DryvValidationResult.Error("Address is incomplete")
        : DryvValidationResult.Success)
```

### Server-Only Rules

```csharp
.ServerRule(
    m => m.Email,
    m => m.Email.Contains("@")
        ? DryvValidationResult.Success
        : DryvValidationResult.Error("Invalid email"))
```

### Disabling Rules

```csharp
.DisableRules(
    m => m.ShippingAddress,
    m => m.SameAsBillingAddress)
```

## Evaluating Rules Manually

If you want to run the validation rules on the server manually (without ASP.NET Core's automatic MVC filter), you can use the `DryvValidator` class:

```csharp
var model = new Address { City = "", ZipCode = "123" };
var validator = new DryvValidator();

// The second parameter is a service provider function for DI resolution
var errors = await validator.Validate(model, type => serviceProvider.GetService(type));

foreach (var error in errors)
{
    foreach (var message in error.Message)
    {
        Console.WriteLine($"{message.Type} on {error.Path}: {message.Text}");
    }
}
```

## Translating Rules Manually

If you need the JavaScript code without the ASP.NET Core wrappers, use `DryvTranslator`:

```csharp
var translator = new DryvTranslator(
    new DryvRuleFinder(new ModelTreeBuilder(), new DryvCompiler(), /*... dependencies */),
    new JavaScriptTranslator(/*... dependencies */),
    new DryvOptions());

// Translate all rules defined on the model
var translatedRules = await translator.TranslateValidationRules(typeof(Address), type => serviceProvider.GetService(type));

foreach (var rule in translatedRules.ValidationRules)
{
    Console.WriteLine($"Rule for {rule.Property.Name}:");
    Console.WriteLine(rule.TranslatedValidationExpression);
}
```

## Evaluation Locations

By default, rules run on both the server and the client. You can control this via the `DryvRuleSettings` or by using specific rule methods:

```csharp
// Runs on both Client and Server (default)
.Rule(m => m.City, m => string.IsNullOrWhiteSpace(m.City) ? "Error" : null)

// Server only - never translated to JS
.ServerRule(m => m.City, m => ...)

// Explicitly set via settings
.Rule(m => m.City, m => ..., new DryvRuleSettings { EvaluationLocation = DryvEvaluationLocation.Client })
```

## Testing Validation Rules

Because Dryv rules are defined as static properties containing expressions, they are very easy to unit test:

```csharp
[Fact]
public async Task Validates_ZipCode()
{
    // Arrange
    var model = new Address { ZipCode = "123" }; // too short
    var validator = new DryvValidator();

    // Act
    var result = await validator.Validate(model, t => null);

    // Assert
    Assert.Contains(result, r => r.Path == "zipCode" && r.Message.Any(m => m.Type == DryvResultType.Error));
}
```

## Rule Parameters

Parameters are named values resolved via DI and sent to the client alongside validation functions. They are ideal for dynamic values that should not be frozen at translation time:

```csharp
public static readonly DryvRules Rules = DryvRules
    .For<MyModel>()
    .Parameter<IOptions<MySettings>, string>(
        "maxLength",
        options => options.Value.MaxLength.ToString())
    .Rule<DryvParameters>(
        m => m.Name,
        (m, p) => m.Name.Length > p.Get<int>("maxLength")
            ? "Name is too long"
            : null);
```

`DryvParameters.Get<T>("name")` translates to `$ctx.parameter("name")` in JavaScript.

## Validation Results

`DryvValidationResult` supports multiple severity levels:

```csharp
DryvValidationResult.Success                          // validation passed
DryvValidationResult.Error("message")                 // error — blocks submission
DryvValidationResult.Warning("message")               // warning — informational only
DryvValidationResult.Error("message", customData)     // error with extra data
```

**JavaScript translation:**

| C# | JavaScript |
|----|-----------|
| `DryvValidationResult.Success` | `null` |
| `DryvValidationResult.Error("text")` | `{ type: "error", text: "text" }` |
| `DryvValidationResult.Warning("text")` | `{ type: "warning", text: "text" }` |
| `"error message"` (implicit conversion) | `{ type: "error", text: "error message" }` |

## Async Rules

Rules can return `Task<DryvValidationResult>` for async operations:

```csharp
.Rule<IEmailService>(
    m => m.Email,
    (m, emailService) => emailService.IsAvailable(m.Email)
        .ContinueWith(t => t.Result
            ? DryvValidationResult.Success
            : "Email is already taken"))
```

Async rules that inject services which cannot be inlined are routed through dynamically generated server endpoints (when using `Dryv.AspNetCore`). The generated JavaScript calls these endpoints via `$ctx.dryv.callServer(url, method, data)`.

## Custom Translators

### Method Call Translators

Extend `MethodCallTranslator` to add translation support for your own types:

```csharp
using Dryv.Translation;
using Dryv.Translation.Translators;

public class MyHelperTranslator : MethodCallTranslator
{
    public MyHelperTranslator()
    {
        Supports<MyHelper>();
        AddMethodTranslator(nameof(MyHelper.Validate), context =>
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".validate(");
            WriteArguments(context.Translator, context.Expression.Arguments, context);
            context.Writer.Write(")");
        });
    }
}
```

### Expression Translators

For intercepting entire expression nodes, implement `IDryvCustomTranslator`:

```csharp
public class MyCustomTranslator : IDryvCustomTranslator
{
    public int? OrderIndex { get; set; }

    public bool? AllowSurroundingBrackets(Expression expression) => true;

    public bool TryTranslate(CustomTranslationContext context)
    {
        if (context.Expression is not BinaryExpression binary
            || binary.Left.Type != typeof(MySpecialType))
            return false;

        context.Translator.Translate(binary.Left, context);
        context.Writer.Write(" /* custom */ ");
        context.Translator.Translate(binary.Right, context);
        return true;
    }
}
```

### Built-in Translators

Dryv ships with the following translators (all registered by default):

| Translator | Handles |
|-----------|---------|
| `StringTranslator` | `string` methods: `StartsWith`, `EndsWith`, `Contains`, `Trim`, `ToLower`, `ToUpper`, `CompareTo`, `Equals`, `IndexOf`, `IsNullOrWhiteSpace`, `IsNullOrEmpty`, `Substring`, `Compare` |
| `RegexTranslator` | `Regex.IsMatch`, `Regex.Match`, `new Regex(pattern).IsMatch(...)` — translates to JS `/pattern/.test(...)` |
| `EnumerableTranslator` | LINQ: `Any`, `All`, `Where`, `Select`, `First`, `FirstOrDefault`, `Last`, `LastOrDefault`, `Count`, `Contains`, `ElementAt`, `ElementAtOrDefault`, `DefaultIfEmpty`, `Sum`, `Average`, `Min`, `Max` |
| `DryvValidationResultTranslator` | `DryvValidationResult.Success`, `.Error()`, `.Warning()` |
| `DryvParametersTranslator` | `DryvParameters.Get<T>("name")` → `$ctx.parameter("name")` |
| `ObjectTranslator` | Handles general object translation |
| `DateTimeTranslator` | `DateTime` / `DateTimeOffset` comparisons → `$ctx.dryv.parseDate(...)` |
| `CustomCodeTranslator` | `DryvClientCode.Raw(...)` — injects raw JavaScript |
| `AllMethodCallTranslator` | Catch-all: translates any method call 1:1 (camelCased) |
| `ToStringTranslator` | `.ToString()` / `.ToString(format)` → `$ctx.dryv.format(...)` |

## Raw JavaScript Injection

For cases where you need to embed raw JavaScript, use `DryvClientCode.Raw(...)`:

```csharp
using Dryv.Rules;

.Rule(m => m.Field,
    m => DryvClientCode.Raw("window.customValidator($m.field)")
        + DryvValidationResult.Success)
```

`DryvClientCode.Raw` is a marker method that throws at runtime — it only exists for the translator.

## Rule Annotators

Add custom metadata to rules via `IDryvRuleAnnotator`:

```csharp
public class MyAnnotator : IDryvRuleAnnotator
{
    public void Annotate(DryvCompiledRule rule, IDictionary<string, object> annotations)
    {
        annotations["priority"] = rule.Property.Name == "Email" ? "high" : "normal";
    }
}
```

Annotations appear in the generated JavaScript rule objects under the `annotations` property.

## C# to JavaScript — Quick Reference

| C# | JavaScript |
|----|-----------|
| `string.IsNullOrWhiteSpace(s)` | `!/\S/.test(s \|\| "")` |
| `s.StartsWith("x")` | `s.indexOf("x") === 0` |
| `s.Contains("x")` | `s.indexOf("x") !== -1` |
| `s.Equals("x", OrdinalIgnoreCase)` | `s.toLowerCase() === "x".toLowerCase()` |
| `new Regex(p).IsMatch(s)` | `/p/.test(s)` |
| `items.Any(x => ...)` | `items.some(x => ...)` |
| `items.All(x => ...)` | `items.every(x => ...)` |
| `items.Where(x => ...)` | `items.filter(x => ...)` |
| `items.Select(x => ...)` | `items.map(x => ...)` |
| `items.Count()` | `items.length` |
| `items.Contains(x)` | `items.includes(x)` |
| `$"text {v}"` | `"text " + v` |
| `v.ToString("D")` | `$ctx.dryv.format(v, "int32", "D")` |
| Enum comparison | String comparison (by default) |
| `DateTime` comparison | `$ctx.dryv.parseDate(...)` comparison |

See the [full translation reference](https://github.com/mhusseini/dryv#c-to-javascript-translation--reference) in the repository root.

## JavaScriptTranslator — Detailed Capabilities

The `JavaScriptTranslator` converts C# expression trees into equivalent JavaScript code. This section documents every expression kind, operator, method call, and special case it supports.

### Supported Expression Types

The translator dispatches on the .NET `Expression` node type. The following table lists each node and how it is handled:

| Expression Node | JavaScript Output | Notes |
|---|---|---|
| `BinaryExpression` | `left op right` | See *Operators* below. Includes null-coalescing (`??` → `\|\|`) and null-safe comparisons. |
| `BlockExpression` | `var a, b; ...` | Variable declarations are emitted as a single `var` statement; sub-expressions are translated sequentially. |
| `ConditionalExpression` | `test ? ifTrue : ifFalse` | Async-aware: when the test contains async calls, wraps in `.then(...)` promise chains automatically. |
| `ConstantExpression` | Literal value | Strings are escaped for safe HTML embedding. `null` → `null`, booleans → `true`/`false`. |
| `DefaultExpression` | Type default | `default(int)` → `0`, `default(string)` → `null`, etc. |
| `IndexExpression` | `obj[index]` | Single-argument indexers only. |
| `InvocationExpression` | `expr(args)` | Delegate invocations. |
| `LambdaExpression` | `function(m, $ctx) { return ... }` | Model parameter is camelCased when `UseLowercaseMembers` is set. A `$ctx` parameter is always appended. |
| `ListInitExpression` | `= [item, ...]` | Collection initializers become JavaScript arrays. |
| `MemberExpression` | `obj.member` | Property/field access. Nullable members are automatically unwrapped via `NullableMemberModifier`. |
| `MemberInitExpression` | `{}` | Object initialization — emits an empty object literal. |
| `MethodCallExpression` | Varies | Dispatched to registered `IDryvMethodCallTranslator` instances. See *Method Call Translators* below. |
| `NewArrayExpression` | `[elements]` | Array creation expressions. |
| `NewExpression` | `{}` | Constructor calls emit an empty object literal. |
| `ParameterExpression` | Parameter name | Emitted as-is. |
| `SwitchExpression` | `switch(val){ case ...: ... break; }` | Full switch/case translation. |
| `UnaryExpression` | Prefix operator | `Not` → `!`, `Negate` → `-`, `Convert` → type-aware (see below), `ArrayLength` → `.length`. |

**Unsupported** (throw `NotSupportedException`): `DynamicExpression`, `GotoExpression`, `LabelExpression`, `LoopExpression`, `TryExpression`, `RuntimeVariablesExpression`, `TypeBinaryExpression`.

### Operators

All C# binary and unary operators are mapped:

| Category | C# | JavaScript |
|---|---|---|
| **Arithmetic** | `+`, `-`, `*`, `/`, `%`, `**` | `+`, `-`, `*`, `/`, `%`, `**` |
| **Comparison** | `==`, `!=`, `<`, `>`, `<=`, `>=` | `===`, `!=`, `<`, `>`, `<=`, `>=` |
| **Logical** | `&&`, `\|\|`, `!` | `&&`, `\|\|`, `!` |
| **Bitwise** | `&`, `\|`, `^` | `&&`, `\|\|`, `\|\|` (mapped to logical equivalents) |
| **Assignment** | `=`, `+=`, `-=`, `*=`, `/=`, `%=`, `**=`, `<<=`, `>>=`, `&=`, `\|=` | Same tokens |
| **Null coalescing** | `??` | `\|\|` |
| **Shift** | `<<`, `>>` | `<<`, `>>` |
| **Increment/Decrement** | `++`, `--` | `++`, `--` |

### Null Handling

- **Null comparisons** (`x == null`, `x != null`) are translated with special `NullComparisonWriter` logic to emit idiomatic null/undefined checks.
- **Value-type coalescing** (`Nullable<T> ?? default`) is detected and translated using `||`.
- **Nullable member access** is rewritten by `NullableMemberModifier` before translation so `.Value` / `.HasValue` are unwrapped.

### Type Conversions (UnaryExpression.Convert)

| Source | Target | JavaScript |
|---|---|---|
| `string` literal | `DryvValidationResult` | `{ type:"error", text: "...", group: "..." }` |
| Any constant | Enum / `Nullable<Enum>` | Enum string name (via `TranslateValue`) |
| Other casts | — | Ignored (operand is translated directly) |

### Async Support

When a rule expression contains async method calls (services returning `Task`), the translator:

1. Detects async calls via `AsyncMethodCallModifier` / `AsyncBinaryFinder`.
2. Wraps them in `.then(function(result){ return ... })` promise chains.
3. Sets `TranslationResult.IsAsync = true` so the client runtime knows to `await` / `.then()` the result.

### Expression Pre-Processing

Before translation, the expression tree is rewritten by three visitors:

| Visitor | Purpose |
|---|---|
| `NullableMemberModifier` | Unwraps `Nullable<T>.Value` / `.HasValue` access. |
| `EnumComparisionModifier` | Normalizes enum comparisons for consistent JS string output. |
| `ConditionConversionModifier` | Normalizes conditional expression conversions. |

### Injected Expressions

The translator supports runtime expression injection via `ExpressionInjectionHelper`. Injected service parameters and `DryvOptions`-based delegates are extracted and replaced with placeholder tokens at translation time. At runtime, these tokens are filled by the generated `TranslationResult.Factory` delegate.

### Method Call Translators — Detailed Reference

#### `StringTranslator` — `System.String`

| C# Method | JavaScript | Notes |
|---|---|---|
| `string.IsNullOrEmpty(s)` | `!s` | Negation-aware: `!string.IsNullOrEmpty(s)` → `s` |
| `string.IsNullOrWhiteSpace(s)` | `!/\S/.test(s \|\| "")` | Negation-aware |
| `s.Contains("x")` | `s.indexOf("x") >= 0` | Case-insensitive overloads use `.toLowerCase()` on both sides |
| `s.StartsWith("x")` | `s.indexOf("x") === 0` | Case-insensitive via `StringComparison` or `bool ignoreCase` parameter |
| `s.EndsWith("x")` | `(s.indexOf("x") >= 0 && s.indexOf("x") === (s.length - "x".length))` | Case-insensitive supported |
| `s.Equals("x")` | `s === "x"` | Static overload: `string.Equals(a, b)` also supported. Case-insensitive via `.toLowerCase()` |
| `s.IndexOf("x")` | `s.indexOf("x")` | Overloads with `startIndex` (int) parameter are **not** supported |
| `s.ToLower()` | `s.toLowerCase()` | Culture parameter is accepted but ignored |
| `s.ToUpper()` | `s.toUpperCase()` | Culture parameter is accepted but ignored |
| `s.Trim()` | `s.trim()` | Only the parameterless overload |
| `s.TrimStart()` | `s.trimLeft()` | Only the parameterless overload |
| `s.TrimEnd()` | `s.trimRight()` | Only the parameterless overload |
| `s.Normalize()` | `s.normalize()` | `NormalizationForm` parameter accepted but ignored |
| `string.Compare(a, b)` | `a.localeCompare(b)` | Overloads with index parameters are **not** supported. Case-insensitive via `.toLowerCase()` |
| `s.CompareTo(other)` | `s.localeCompare(other)` | |
| `string.Format(fmt, args)` | `"part1" + arg0 + "part2"` | Non-constant format strings are **not** supported. `IFormatProvider` overloads are parsed but locale is ignored. Format specifiers use `$ctx.dryv.format(...)`. |

#### `RegexTranslator` — `System.Text.RegularExpressions.Regex`

| C# Pattern | JavaScript | Notes |
|---|---|---|
| `Regex.IsMatch(input, pattern)` | `/pattern/.test(input)` | Static overload |
| `new Regex(pattern).IsMatch(input)` | `/pattern/.test(input)` | Instance overload; pattern extracted from constructor |
| `regex.IsMatch(input)` (captured variable) | `/pattern/.test(input)` | Pattern extracted from captured `Regex` object |
| `regex.Match(input).Success` | `/pattern/.test(input)` | `Group.Success` property access is translated as a test |

**RegexOptions mapping:**

| C# Option | JS Flag |
|---|---|
| `IgnoreCase` | `i` |
| `Multiline` | `m` |
| `IgnorePatternWhitespace` | ❌ Not supported (throws) |
| `RightToLeft` | ❌ Not supported (throws) |

#### `EnumerableTranslator` — `System.Linq.Enumerable`

| C# Method | JavaScript | Notes |
|---|---|---|
| `items.Any(x => ...)` | `items.some(function(x){ return ... })` | Negation-aware |
| `items.All(x => ...)` | `items.every(function(x){ return ... })` | Negation-aware |
| `items.Where(x => ...)` | `items.filter(function(x){ return ... })` | |
| `items.Select(x => ...)` | `items.map(function(x){ return ... })` | |
| `items.First()` | `(items)[0]` | With predicate: filters first |
| `items.FirstOrDefault()` | `(function(arr){ return arr.length ? arr[0] : null; })(items)` | With predicate: filters first |
| `items.Last()` | `(function(arr){ return arr[arr.length - 1]; })(items)` | |
| `items.LastOrDefault()` | `(function(arr){ return arr.length ? arr[arr.length - 1] : null; })(items)` | |
| `items.Single()` | `(function(arr){ return arr[0]; })(items)` | |
| `items.SingleOrDefault()` | `(function(arr){ return arr.length == 1 ? arr[0] : null; })(items)` | |
| `items.Count()` | `(function(arr){ return arr.length; })(items)` | With predicate: filters first |
| `items.LongCount()` | Same as `Count()` | |
| `items.Contains(x)` | `items.indexOf(x) >= 0` | Negation-aware |
| `items.ElementAt(i)` | `items[i]` | |
| `items.ElementAtOrDefault(i)` | `(function(arr){ return arr.length > i ? arr[i] : null; })(items)` | |
| `items.DefaultIfEmpty()` | `(function(arr){ return arr.length ? arr : [default]; })(items)` | Custom default value supported |
| `items.Sum()` | `items.reduce(function(a,b){ return a+b }, 0)` | With selector: maps first |
| `items.Average()` | `(function(){ var _c = 0; return items.reduce(...) / _c; })()` | With selector: maps first |
| `items.Min(x => ...)` | `Math.min.apply(null, items.map(...))` | |
| `items.Max(x => ...)` | `Math.max.apply(null, items.map(...))` | |

#### `DryvValidationResultTranslator`

| C# | JavaScript |
|---|---|
| `DryvValidationResult.Success` | `null` |
| `DryvValidationResult.Error("text")` | `{ type:"error", text:"text" }` |
| `DryvValidationResult.Warning("text")` | `{ type:"warning", text:"text" }` |
| `DryvValidationResult.Error("text", data)` | `{ type:"error", text:"text", data: ... }` |

When a `group` is set on the rule, it is included as `group: "..."` in the emitted object.

#### `DateTimeTranslator` — `DateTime` / `DateTimeOffset`

Comparisons between `DateTime` or `DateTimeOffset` values (e.g. `m.BirthDate < DateTime.Now`) are translated using the Dryv client-side helper:

```
$ctx.dryv.parseDate(value, cultureName, momentFormat) op $ctx.dryv.parseDate(...)
```

- .NET date format strings are converted to Moment.js tokens via `MomentJsFormatConverter`.
- `CultureInfo.CurrentUICulture.Name` is injected as a runtime expression.
- Null comparisons are excluded (handled by the normal binary path).

#### `ObjectTranslator` — `.ToString()`

Any `.ToString()` call on an object is translated to:

```javascript
$ctx.dryv.format(value, "typeName")
// or with a format string:
$ctx.dryv.format(value, "typeName", "formatString")
```

#### `ToStringTranslator`

A simpler fallback for `.ToString()` without format parameters:

```javascript
value.toString()
```

#### `DryvParametersTranslator`

When `DryvOptions.DisableParameterInjection` is set:

| C# | JavaScript |
|---|---|
| `parameters.Get<int>("name")` | `$ctx.parameter("name")` |

#### `CustomCodeTranslator` — Raw JavaScript

`DryvClientCode.Raw("js code")` injects the literal string as JavaScript. Binary expressions involving `DryvClientCode` concatenation are handled specially (brackets are suppressed).

#### `AllMethodCallTranslator` — Catch-All

Any method call not handled by a more specific translator is translated 1:1 with the method name converted to camelCase:

```
obj.MyMethod(a, b)  →  obj.myMethod(a, b)
```

### Value Serialization

| C# Type | JavaScript Literal |
|---|---|
| `string` | `"text"` (HTML-safe escaped: `\u0022`, `\u0027`, `\u0026`, `\u003c`, `\u003e`, `\u005c`) |
| `bool` | `true` / `false` |
| `null` | `null` |
| `DateTime` / `DateTimeOffset` | `"formatted string"` (using `CultureInfo.CurrentUICulture`) |
| `Guid` | `"guid-string"` |
| `Enum` | Delegated to `DryvOptions.JsonConversion` or `.ToString()` |
| Numeric types | `.ToString()` |
| `DryvValidationResult` | Object literal (see above) |

### Identifier Formatting

When `UseLowercaseMembers` is `true` (the default for ASP.NET Core integration), property and parameter names are emitted in camelCase (`FirstName` → `firstName`).

## Client-Side Libraries

The official companion packages consume the `DryvValidationRuleSet` objects generated by this library:

| Package | Install | Description |
|---------|---------|-------------|
| [`dryvjs`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvjs) | `npm install dryvjs` | Framework-agnostic reactive validation engine |
| [`dryvue`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvue) | `npm install dryvue` | Vue 3 bindings with `useDryv` composable |

## Documentation

See the [full documentation](https://github.com/mhusseini/dryv#readme) in the repository root for:
- Client-side usage with DryvJS and Dryvue
- Build-time code generation patterns
- Standalone / custom integration (without ASP.NET Core)
- Comprehensive C# to JavaScript translation reference
- Rule discovery deep-dive (interfaces, base classes, nested models, collections)
- Troubleshooting and FAQ

## License

[MIT](../LICENSE)

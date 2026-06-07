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
    async (m, emailService) => await emailService.IsAvailable(m.Email)
        ? DryvValidationResult.Success
        : "Email is already taken")
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

See the [full translation reference](https://github.com/mhusseini/dryv#c-to-javascript-translation--in-depth) in the repository root.

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

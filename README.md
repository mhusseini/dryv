<p align="center">
  <img src="logo_slogan_light.svg" title="Dryv - DRY Validation for ASP.NET" width="300">
</p>

<p align="center">
  <a href="https://www.nuget.org/packages/Dryv"><img src="https://img.shields.io/nuget/v/Dryv.svg" alt="NuGet"></a>
  <a href="https://www.nuget.org/packages/Dryv.AspNetCore"><img src="https://img.shields.io/nuget/v/Dryv.AspNetCore.svg?label=Dryv.AspNetCore" alt="NuGet Dryv.AspNetCore"></a>
  <a href="https://github.com/mhusseini/dryv/blob/main/LICENSE"><img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License"></a>
</p>

<p align="center">
  <strong>Write complex model validation rules in C# once — Dryv translates them to JavaScript automatically.</strong>
</p>

---

Dryv (short for **DRY Validation**) eliminates the headache of duplicating validation logic across your server and client. Simply define your rules once using familiar C# expressions, and Dryv will:

1. **Execute them securely on the server** during standard model validation.
2. **Translate them into JavaScript** to provide instant, seamless feedback in the browser.

By making your C# backend the single source of truth, Dryv ensures your application remains secure and responsive—without the burden of maintaining two separate codebases.

## Why Dryv?

In a typical web application, validation logic inevitably gets duplicated:
- **Server-side validation (C#)** is absolutely mandatory for security and data integrity.
- **Client-side validation (JavaScript)** is essential for providing instant user feedback and a snappy UI.

For straightforward rules (like `[Required]` or `[StringLength]`), ASP.NET Core's built-in Data Annotations do the job perfectly and translate natively to jQuery Validate. But the moment you need **complex validation**—like cross-property checks, conditional rules, or database lookups—you hit a wall. To solve this, developers usually have to:
1. Write a custom `ValidationAttribute`, use `IValidatableObject`, or integrate a library like `FluentValidation` for the backend.
2. Manually write custom JavaScript or wire up tedious AJAX calls for the frontend.

**Dryv breaks this cycle by making C# the definitive source of truth.** You write your complex business rules exactly once using C# expression trees. Dryv compiles and executes them on the server, and more importantly, it **translates them into pure JavaScript functions** that run instantly on the client.

**How Dryv stands out:**
- **vs. DataAnnotations:** Effortlessly handles complex conditional logic, cross-property validation, and dependency injection—areas where basic attributes fall short.
- **vs. FluentValidation:** While FluentValidation is fantastic for server-side checks, it leaves you hanging on the frontend. Dryv bridges this gap by translating complex logic into JavaScript.
- **vs. AJAX/Remote Validation:** Dryv translates rules to run *natively* in the browser whenever possible, entirely eliminating network latency. It only falls back to dynamic API endpoints when a rule explicitly requires server-side resources (like a database check).
- **vs. Frontend Validation Libraries (Vuelidate, Yup, Zod, etc.):** While these libraries are excellent for client-side validation in Vue, React, or Svelte, using them means you still have to manually duplicate and maintain your C# backend logic in JavaScript. Dryv eliminates this duplication by automatically generating the exact validation rules your frontend needs, directly from your C# code.

## Table of Contents

- [Why Dryv?](#why-dryv)
- [Features](#features)
- [Packages](#packages)
- [Quick Start](#quick-start)
  - [1. Define Rules](#1-define-rules)
  - [2. Register Dryv (ASP.NET Core)](#2-register-dryv-aspnet-core)
  - [3. Render Client Validation](#3-render-client-validation)
- [Writing Validation Rules](#writing-validation-rules)
  - [Basic Rules](#basic-rules)
  - [Multi-Property Rules](#multi-property-rules)
  - [Async Rules](#async-rules)
  - [Server-Only Rules](#server-only-rules)
  - [Disabling Rules](#disabling-rules)
  - [Rule Groups](#rule-groups)
  - [Validation Results (Errors and Warnings)](#validation-results-errors-and-warnings)
  - [Dependency Injection in Rules](#dependency-injection-in-rules)
  - [Rule Parameters](#rule-parameters)
  - [Rule Annotators](#rule-annotators)
  - [Testing Rules](#testing-rules)
- [Rule Discovery](#rule-discovery)
- [C# to JavaScript Translation](#c-to-javascript-translation)
- [ASP.NET Core Integration](#aspnet-core-integration)
  - [Automatic Server-Side Validation](#automatic-server-side-validation)
  - [Dynamic Controllers for Async Rules](#dynamic-controllers-for-async-rules)
  - [Tag Helper](#tag-helper)
  - [HTML Helper](#html-helper)
  - [Preloading](#preloading)
  - [Disabling Dryv per Action](#disabling-dryv-per-action)
  - [Validation Sets](#validation-sets)
- [Client-Side Usage](#client-side-usage)
  - [Understanding the Output Format](#understanding-the-output-format)
  - [How the Script Is Rendered on the Page](#how-the-script-is-rendered-on-the-page)
  - [Automatic Discovery by DryvJS / Dryvue](#automatic-discovery-by-dryvjs--dryvue)
  - [The Validation Function Signature](#the-validation-function-signature)
  - [Running Validators in Vanilla JavaScript](#running-validators-in-vanilla-javascript)
  - [Handling Async Rules on the Client](#handling-async-rules-on-the-client)
  - [Using Parameters on the Client](#using-parameters-on-the-client)
  - [Working with Groups and Related Properties](#working-with-groups-and-related-properties)
  - [Integration with Frontend Frameworks](#integration-with-frontend-frameworks)
- [Standalone / Custom Integration](#standalone--custom-integration)
  - [Extracting Translated Rules Programmatically](#extracting-translated-rules-programmatically)
  - [Extracting Rule Parameters](#extracting-rule-parameters)
  - [Build-Time Code Generation](#build-time-code-generation)
- [C# to JavaScript Translation — Reference](#c-to-javascript-translation--reference)
- [Tips & Tricks](#tips--tricks)
- [Troubleshooting & FAQ](#troubleshooting--faq)
- [Configuration](#configuration)
- [Project Structure](#project-structure)
- [License](#license)

## Features

- **C# → JavaScript translation** — validation expressions are automatically converted to equivalent JavaScript functions.
- **Async validation** — supports `Task<DryvValidationResult>` rules; async rules that cannot run in the browser are routed through dynamically generated server endpoints.
- **Dependency injection** — inject services (e.g. `IOptions<T>`, custom validators) directly into rule expressions.
- **Multi-property rules** — a single rule can apply to multiple properties, with related-property metadata for cross-field validation UIs.
- **Rule parameters** — define named parameters resolved via DI and passed to client-side validation.
- **Disabling rules** — conditionally disable entire subtrees of validation.
- **Automatic ASP.NET Core integration** — plugs into the MVC validation pipeline, auto-generates API endpoints for async rules, and provides Tag Helpers and HTML Helpers for rendering client code.
- **Extensible translation** — plug in custom translators for your own types and methods.
- **Preloading** — optionally discover and compile all rules at startup for faster first requests.

## Packages

**Server (C# / .NET):**

| Package | Description |
|---------|-------------|
| [`Dryv`](https://www.nuget.org/packages/Dryv) | Core library — rule definition, validation, and C#-to-JavaScript translation. Framework-agnostic (targets .NET 9). |
| [`Dryv.AspNetCore`](https://www.nuget.org/packages/Dryv.AspNetCore) | ASP.NET Core integration — MVC filters, Tag Helpers, HTML Helpers, dynamic controller generation, and preloading. |

```shell
dotnet add package Dryv
dotnet add package Dryv.AspNetCore   # if using ASP.NET Core
```

**Client (JavaScript / TypeScript):**

| Package | Description |
|---------|-------------|
| [`dryvjs`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvjs) | Core validation engine — framework-agnostic, reactive, supports async rules, nested objects, arrays, dirty tracking. |
| [`dryvue`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvue) | Vue 3 integration — reactive composables (`useDryv`), `v-model` bindings, validation group slots. |

```shell
npm install dryvjs              # framework-agnostic client validation
npm install dryvue              # Vue 3 integration (includes dryvjs)
```

## Quick Start

### 1. Define Rules

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

By defining rules as simple C# lambda expressions, Dryv can translate them into JavaScript for the client while simultaneously compiling them for efficient server-side execution.

### 2. Register Dryv (ASP.NET Core)

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddDryv()
    .AddDryvDynamicControllers()
    .AddDryvPreloading();

var app = builder.Build();

app.UseDryv();

app.MapDefaultControllerRoute();
app.Run();
```

### 3. Render Client Validation

**Using the Tag Helper:**
```html
@addTagHelper *, Dryv.AspNetCore

<dryv-client-rules for="typeof(Address)" name="address" />
```

This renders a `<script>` tag containing the translated JavaScript validation functions.

**Using the HTML Helper:**
```html
@await Html.DryvValidation<Address>("address")
```

## Writing Validation Rules

### Basic Rules

A standard rule targets a specific property. It returns an error message (or a `DryvValidationResult`) if validation fails, and `null` (or `DryvValidationResult.Success`) if it succeeds:

```csharp
public static readonly DryvRules Rules = DryvRules
    .For<MyModel>()
    .Rule(
        m => m.Email,
        m => string.IsNullOrWhiteSpace(m.Email)
            ? DryvValidationResult.Error("Email is required")
            : DryvValidationResult.Success);
```

Strings are implicitly converted to `DryvValidationResult`, so you can also write:

```csharp
.Rule(
    m => m.Email,
    m => string.IsNullOrWhiteSpace(m.Email)
        ? "Email is required"
        : null)
```

### Multi-Property Rules

A single rule can govern multiple properties at once. If any of the specified properties change, the rule is automatically re-evaluated for all of them:

```csharp
.Rule(
    m => m.Street, m => m.City, m => m.Zip,
    m => string.IsNullOrWhiteSpace(m.Street)
      || string.IsNullOrWhiteSpace(m.City)
      || string.IsNullOrWhiteSpace(m.Zip)
        ? DryvValidationResult.Error("Address is incomplete")
        : DryvValidationResult.Success)
```

Multi-property rules are smart. They automatically:
- Assign a **group name** (defaulting to the first property's path) so your UI can easily deduplicate error messages.
- Track **related properties**, giving the client the context it needs to re-validate interdependent fields together.

### Async Rules

Rules can handle asynchronous operations by returning a `Task<DryvValidationResult>`:

```csharp
.Rule(
    m => m.Email,
    async m => await emailService.IsAvailable(m.Email)
        ? DryvValidationResult.Success
        : "Email is already taken")
```

On the client side, Dryv automatically routes any async rules requiring server interaction through seamlessly generated [dynamic controllers](#dynamic-controllers-for-async-rules).

### Server-Only Rules

Use `ServerRule` for rules that should only run on the server and never be translated to JavaScript:

```csharp
.ServerRule(
    m => m.Email,
    m => m.Email.Contains("@")
        ? DryvValidationResult.Success
        : DryvValidationResult.Error("Invalid email"))
```

Server-only rules can use `Func<>` delegates (not just `Expression<>`) since they don't need translation.

### Disabling Rules

You can conditionally disable all validation rules for a property (or subtree):

```csharp
.DisableRules(
    m => m.ShippingAddress,
    m => m.SameAsBillingAddress)   // returns bool
```

When `SameAsBillingAddress` is `true`, all rules under `ShippingAddress` are skipped — on both server and client.

### Rule Groups

Multi-property rules automatically receive a group. You can also set an explicit group:

```csharp
.Rule(
    m => m.Field1, m => m.Field2,
    m => string.IsNullOrWhiteSpace(m.Field1)
        ? DryvValidationResult.Error("error")
        : DryvValidationResult.Success,
    new DryvRuleSettings("my-custom-group"))
```

Groups are included in the generated JavaScript validation result, allowing client-side UI frameworks to show a single error for a group of related fields.

### Validation Results (Errors and Warnings)

`DryvValidationResult` supports two severity levels:

```csharp
DryvValidationResult.Error("This is an error")       // blocks form submission
DryvValidationResult.Warning("This is a warning")    // informational, does not block

DryvValidationResult.Error("error", customData)       // with additional data
DryvValidationResult.Warning("warning", customData)
```

### Dependency Injection in Rules

Rules can accept injected services as additional parameters. Service types are resolved from the DI container at validation time:

```csharp
.Rule<IOptions<MyAppSettings>>(
    m => m.Name,
    (m, options) => m.Name == options.Value.ForbiddenName
        ? "This name is not allowed"
        : null)
```

You can inject up to 5 services:

```csharp
.Rule<IValidator, IOptions<Settings>>(
    m => m.Email,
    (m, validator, settings) => validator.Check(m.Email, settings.Value))
```

Injected services whose values are known at translation time (e.g. `IOptions<T>`) are **inlined** into the generated JavaScript. Services that cannot be inlined cause the rule to be routed through a dynamic controller endpoint instead.

### Rule Parameters

Parameters are named values resolved via DI and sent to the client alongside validation functions:

```csharp
.Parameter<IOptions<MySettings>, string>(
    "maxLength",
    options => options.Value.MaxLength.ToString())
```

In rules, parameters are accessed via `DryvParameters`:

```csharp
.Rule(
    m => m.Name,
    (m, p) => m.Name.Length > p.Get<int>("maxLength")
        ? "Name is too long"
        : null)
```

On the client, parameters are available through the `$ctx.parameter("maxLength")` function.

### Rule Annotators

You can add custom metadata to your rules that will be passed down to the client. This is useful for building advanced UI features, such as defining which UI component should render the error, or tracking analytics.

1. Create a class implementing `IDryvRuleAnnotator`.
2. Register it in `DryvOptions`.

```csharp
public class CustomDataAnnotator : IDryvRuleAnnotator
{
    public void Annotate(DryvCompiledRule rule, IDictionary<string, object> annotations)
    {
        if (rule.Property.Name == "CreditCard")
        {
            annotations["ui-component"] = "SecureInput";
            annotations["analytics-event"] = "credit_card_validation_failed";
        }
    }
}

// In Startup.cs
builder.Services.AddDryv(options => 
{
    options.Annotators.Add<CustomDataAnnotator>();
});
```

The annotations are serialized into the generated JavaScript and are available to your client-side validation handler.

### Testing Rules

Because Dryv rules are pure C# expressions stored on your models, unit testing them is straightforward. You don't need a running ASP.NET Core server to test your validation logic.

```csharp
[Fact]
public async Task Address_WithMissingCity_FailsValidation()
{
    // Arrange
    var model = new Address { City = "", ZipCode = "12345" };
    var validator = new DryvValidator();
    
    // Act
    // Pass a dummy service provider if your rules don't use DI
    var errors = await validator.Validate(model, type => null);
    
    // Assert
    var cityError = errors.FirstOrDefault(e => e.Path == "city");
    Assert.NotNull(cityError);
    Assert.Contains(cityError.Message, m => m.Type == DryvResultType.Error);
}
```

## Rule Discovery

Dryv automatically discovers validation rules by scanning for `static` fields, properties, or methods of type `DryvRules` on:

- **The model class itself** — rules defined directly on the model.
- **Implemented interfaces** — rules defined for an interface apply to all implementing types.
- **Base classes** — rules for a base type apply to derived types.
- **Child model properties** — rules on nested types are discovered recursively via the model tree.
- **Attributed properties** — use `[DryvValidation(typeof(MyRuleContainer))]` on a property or field to point to an external rule container class.
- **Static methods** — `static DryvRules Rules() => ...` methods are also discovered.

```csharp
// Rules defined on a separate class, referenced via attribute
public class ParentModel
{
    [DryvValidation(typeof(ChildRules))]
    public ChildModel Child { get; set; }

    [DryvValidation(typeof(OtherChildRules))]
    public ChildModel OtherChild { get; set; }
}

public class ChildRules
{
    public static DryvRules Rules = DryvRules
        .For<ChildModel>()
        .Rule(m => m.Name, m => string.IsNullOrWhiteSpace(m.Name) ? "Required" : null);
}
```

This allows different validation rule sets for the same model type depending on context.

Dryv uses a `DryvRuleFinder` to scan model types and their dependencies for validation rules. Here are the discovery mechanisms in detail:

### Rules on Interfaces

Rules defined for an interface apply to all implementing classes:

```csharp
public interface IHasEmail
{
    string Email { get; set; }
}

// Rules defined for the interface
public static class EmailRules
{
    public static readonly DryvRules Rules = DryvRules
        .For<IHasEmail>()
        .Rule(m => m.Email,
            m => string.IsNullOrWhiteSpace(m.Email)
                ? DryvValidationResult.Error("Email is required")
                : DryvValidationResult.Success);
}

// Any class implementing IHasEmail gets these rules automatically
public class Customer : IHasEmail
{
    public static DryvRules Rules = EmailRules.Rules; // reference the rules
    public string Email { get; set; }
}
```

### Rules on Base Classes

Rules defined for a base class apply to all derived classes:

```csharp
public abstract class PersonBase
{
    public virtual string Name { get; set; }
}

public class Employee : PersonBase
{
    public static DryvRules Rules = DryvRules
        .For<PersonBase>()
        .Rule(m => m.Name,
            m => m.Name != null
                ? DryvValidationResult.Success
                : DryvValidationResult.Error("Name is required"));

    public override string Name { get; set; }
}
```

### Rules in Static Methods

Rules can be returned by static methods (not just fields/properties):

```csharp
public class Product
{
    // Discovered as a static method returning DryvRules
    public static DryvRules Rules() => DryvRules
        .For<Product>()
        .Rule(m => m.Name,
            m => m.Name != null
                ? DryvValidationResult.Success
                : DryvValidationResult.Error("error"));

    public string Name { get; set; }
}
```

### Rules via External Container Classes

Use `[DryvValidation]` to point to an external class that contains rules:

```csharp
// On the model class:
[DryvValidation(typeof(AddressValidation))]
public class Address
{
    public string City { get; set; }
    public string ZipCode { get; set; }
}

// Separate validation class:
public class AddressValidation
{
    public static DryvRules Rules = DryvRules
        .For<Address>()
        .Rule(m => m.City, m => string.IsNullOrWhiteSpace(m.City) ? "Required" : null)
        .Rule(m => m.ZipCode, m => string.IsNullOrWhiteSpace(m.ZipCode) ? "Required" : null);
}
```

You can also put `[DryvValidation]` on individual **properties or fields** to apply different rule sets for the same child type in different contexts:

```csharp
public class Order
{
    [DryvValidation(typeof(BillingAddressRules))]
    public Address BillingAddress { get; set; }

    [DryvValidation(typeof(ShippingAddressRules))]
    public Address ShippingAddress { get; set; }
}
```

### Nested / Hierarchical Models

Dryv recursively traverses the model tree to find rules on nested types:

```csharp
public class OrderForm
{
    public CustomerInfo Customer { get; set; }
}

public class CustomerInfo
{
    public ContactDetails Contact { get; set; }
}

public class ContactDetails
{
    public static DryvRules Rules = DryvRules
        .For<ContactDetails>()
        .Rule(m => m.Email,
            m => string.IsNullOrWhiteSpace(m.Email)
                ? "Email required"
                : null);

    public string Email { get; set; }
}
```

When validating `OrderForm`, Dryv discovers the rule on `ContactDetails.Email` via the path `customer.contact.email`. The generated JavaScript uses the full camelCase path to reference the property.

Rules can also target nested properties directly from a parent:

```csharp
public class Parent
{
    public static DryvRules Rules = DryvRules
        .For<Parent>()
        .Rule(m => m.Child.Child.Text,
            m => m.Child.Child.Text != null
                ? DryvValidationResult.Success
                : DryvValidationResult.Error("error"));

    public Child Child { get; set; }
}
```

### Collection Elements

Rules on types that appear as elements of collections (`T[]`, `IEnumerable<T>`, `List<T>`) are also discovered:

```csharp
public class ShoppingCart
{
    public CartItem[] Items { get; set; }
}

public class CartItem
{
    public static DryvRules Rules = DryvRules
        .For<CartItem>()
        .Rule(m => m.Name,
            m => string.IsNullOrWhiteSpace(m.Name)
                ? "Item name is required"
                : null);

    public string Name { get; set; }
}
```

Dryv discovers the `CartItem` rules when scanning `ShoppingCart` because `Items` is a `CartItem[]`.

### Different Rule Sets for the Same Type

By using `[DryvValidation(typeof(...))]` on different properties, you can apply entirely different rule sets to the same model type based on where it appears:

```csharp
public class ParentModel
{
    [DryvValidation(typeof(NameRules))]
    public ChildModel Primary { get; set; }

    [DryvValidation(typeof(AddressRules))]
    public ChildModel Secondary { get; set; }
}

public class ChildModel
{
    public string Name { get; set; }
    public string Address { get; set; }
}

public class NameRules
{
    public static DryvRules Rules = DryvRules
        .For<ChildModel>()
        .Rule(m => m.Name, m => string.IsNullOrWhiteSpace(m.Name) ? "Name is required" : null);
}

public class AddressRules
{
    public static DryvRules Rules = DryvRules
        .For<ChildModel>()
        .Rule(m => m.Address, m => string.IsNullOrWhiteSpace(m.Address) ? "Address is required" : null);
}
```

Result: `primary.name` gets the `NameRules`, `secondary.address` gets the `AddressRules`. The `primary` property does **not** get address validation and vice versa.


## C# to JavaScript Translation

Dryv translates C# expression trees into equivalent JavaScript. The translation is performed by `JavaScriptTranslator` with a set of built-in translators for common .NET types. For a complete reference of every supported construct with detailed examples, see [C# to JavaScript Translation — Reference](#c-to-javascript-translation--reference) below.

**Quick Reference:**

| C# | JavaScript |
|----|-----------|
| `string.IsNullOrWhiteSpace(s)` | `!/\S/.test(s \|\| "")` |
| `s.StartsWith("x")` | `s.indexOf("x") === 0` |
| `s.EndsWith("x")` | `s.indexOf("x", s.length - "x".length) !== -1` |
| `s.Contains("x")` | `s.indexOf("x") !== -1` |
| `s.Trim()`, `ToLower()`, `ToUpper()` | `.trim()`, `.toLowerCase()`, `.toUpperCase()` |
| `$"Hello {name}"` | `"Hello " + name` |
| `new Regex(p).IsMatch(s)` | `/p/.test(s)` |
| `items.Any(x => ...)` / `.All(...)` | `items.some(...)` / `.every(...)` |
| `items.Where(...)` / `.Select(...)` | `items.filter(...)` / `.map(...)` |
| `items.Count()` / `.Contains(x)` | `items.length` / `.includes(x)` |
| `DryvValidationResult.Error("text")` | `{ type: "error", text: "text" }` |
| `DryvValidationResult.Success` | `null` |

## ASP.NET Core Integration

The `Dryv.AspNetCore` package provides deep integration with ASP.NET Core MVC.

### Automatic Server-Side Validation

By default, Dryv adds an action filter (`DryvValidationFilterAttribute`) that validates the model on every action and adds errors to `ModelState`. This works alongside standard `[Required]`, `[StringLength]`, etc. attributes.

### Dynamic Controllers for Async Rules

When you call `AddDryvDynamicControllers()`, Dryv automatically generates ASP.NET Core controller endpoints at runtime for async validation rules that cannot run purely in the browser. The generated JavaScript calls these endpoints transparently.

```csharp
builder.Services
    .AddControllersWithViews()
    .AddDryv()
    .AddDryvDynamicControllers(options =>
    {
        // Customize endpoint routing, HTTP method, filters, etc.
        options.WithEndpoint((ctx, builder) =>
            builder.MapControllerRoute(ctx.ControllerFullName, $"api/validate/{ctx.Action}"));
    });
```

### Tag Helper

The `<dryv-client-rules>` tag helper renders a `<script>` block with all translated validation functions:

```html
@addTagHelper *, Dryv.AspNetCore

<!-- Single model type -->
<dryv-client-rules for="typeof(MyModel)" name="myModel" />

<!-- Infer from page model -->
<dryv-client-rules />

<!-- Multiple validation sets -->
<dryv-client-rules for="@(new Dictionary<string, Type> {
    { "billing", typeof(Address) },
    { "shipping", typeof(Address) }
})" />
```

### HTML Helper

```csharp
// In a Razor view:
@await Html.DryvValidation<MyModel>("myModel")

// Or let the name default to the model type name:
@await Html.DryvValidation<MyModel>()
```

### Preloading

Call `AddDryvPreloading()` to discover and compile all rules at application startup instead of on first request:

```csharp
builder.Services
    .AddControllersWithViews()
    .AddDryv()
    .AddDryvPreloading();
```

### Disabling Dryv per Action

Use `[DryvDisable]` on a controller or action to skip Dryv validation:

```csharp
[DryvDisable]
public IActionResult Import(MyModel model)
{
    // Dryv validation is skipped for this action
}
```

### Validation Sets

Use the `[DryvSet("name")]` attribute on model classes to organize validation into named sets. This is useful when rendering validation for multiple model types on a single page.

## Standalone / Custom Integration

Dryv isn't tied exclusively to server-rendered Razor views. Using the `DryvTranslator` and `DryvClientWriter` APIs, you can programmatically extract the translated JavaScript and serve it through any API—whether it's REST, GraphQL, gRPC, or something else. This approach shines when:

- You're building a **decoupled SPA** (like React, Vue, Angular, or Svelte) that doesn't rely on Razor.
- You want to **generate validation code at build time**, meaning the client never has to ping the server for rule definitions during runtime.
- You need to **embed validation rules inside a mobile app** or any other non-browser environment.

### Extracting Translated Rules Programmatically

Inject `DryvTranslator`, `IDryvClientValidationFunctionWriter`, and `DryvOptions`, then use `DryvClientWriter` to produce the JavaScript output:

```csharp
using Dryv;
using Dryv.AspNetCore;
using Dryv.Configuration;
using Dryv.Validation;

public class ValidationRulesEndpoint(
    DryvTranslator translator,
    IDryvClientValidationFunctionWriter functionWriter,
    DryvOptions options,
    IHttpContextAccessor httpContextAccessor)
{
    public async Task<string> GetRulesAsJson()
    {
        // Discover all model types annotated with [DryvValidation]
        var types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass
                && t.GetCustomAttributes(typeof(DryvValidationAttribute), true).Length > 0)
            .ToDictionary(t => t.Name);

        // Create a client writer with a custom set writer that outputs JSON
        var clientWriter = new DryvClientWriter(
            translator,
            functionWriter,
            new DryvClientValidationSetWriter(options));

        // Translate all rules to JavaScript
        var write = await clientWriter.WriteDryvValidation(
            types,
            httpContextAccessor.HttpContext!.RequestServices.GetService,
            new Dictionary<string, object>());

        // Render to string
        var sb = new StringBuilder();
        await using (var sw = new StringWriter(sb))
        {
            write(sw);
        }

        return sb.ToString();
    }
}
```

This gives you the complete set of translated validation functions, disablers, and parameter placeholders as a string. You can serve it from a REST controller, a GraphQL query, a middleware, or write it to a file.

### Extracting Rule Parameters

Rule parameters (values defined via `.Parameter(...)`) are intentionally **not** baked into the translated rules — they change at runtime (e.g. `DateTime.Today`). Expose a separate endpoint that evaluates parameter factories on demand:

```csharp
public async Task<IDictionary<string, object>> GetParameters(string modelTypeName)
{
    var type = /* resolve your model type by name */;

    var result = await translator.TranslateValidationRules(
        type,
        httpContextAccessor.HttpContext!.RequestServices.GetService);

    return result.Parameters;
}
```

The client fetches fresh parameter values at runtime and injects them into the validation context.

### Build-Time Code Generation

One of Dryv's most powerful patterns is **generating typed validation files during development**. This ensures your client ships with pre-compiled rules, completely bypassing the need to fetch them at runtime:

1. **Expose a dev-only API** (like REST or GraphQL) that serves the translated rules as JSON.
2. **Write a quick code-generation script** (such as a Node.js or TypeScript script) that:
   - Queries the API while your backend is running locally.
   - Parses the resulting JSON.
   - Generates a dedicated, strongly-typed file per model (e.g., in TypeScript or JavaScript) containing the validation rule set.
3. **Import the generated files** straight into your frontend and hand them off to your validation runtime.

**Example generated TypeScript file:**

```typescript
// generated/validation/Address.ts  (auto-generated — do not edit)
import type { DryvValidationRuleSet } from 'dryvjs'
import type { AddressInput } from '../types'

export const AddressValidationSet = {
    name: "Address",
    validators: {
        city: [
            {
                validate: function ($m, $ctx) {
                    return !/\S/.test($m.city || "")
                        ? { type: "error", text: "Please enter a city." }
                        : null
                }
            }
        ],
        zipCode: [
            {
                validate: function ($m, $ctx) {
                    return !/\S/.test($m.zipCode || "")
                        ? { type: "error", text: "Please enter a ZIP code." }
                        : null
                }
            },
            {
                async: true,
                validate: function ($m, $ctx) {
                    return $ctx.dryv.callServer("/_v/cab12ef34", "POST", {
                        zipCode: $m.zipCode
                    }).then(function ($r) {
                        return $ctx.dryv.handleResult($ctx, $m, "zipCode", null, $r)
                    })
                }
            }
        ]
    },
    disablers: {},
    parameters: {}
} as DryvValidationRuleSet<AddressInput>
```

**Important considerations:**

- **Regenerate after every C# rule change.** Dynamic controller URLs contain an MD5 hash of the expression — changing a rule changes the URL, and stale client code will get 404s.
- **Exclude parameter values from generated files.** Parameters like `DateTime.Today` must be fetched at runtime; baking them in at build time freezes them.
- **Only expose the generation endpoint in development.** Production should use the pre-generated files.

## Client-Side Usage

While Dryv is incredibly adept at generating JavaScript validation functions, it doesn't force a specific client-side framework onto your stack. You retain full control over how and when these functions execute in the browser. This section demystifies exactly what Dryv outputs and shows you how to seamlessly wire it up to your frontend.

### Understanding the Output Format

Regardless of whether you use the Tag Helper, the HTML Helper, or programmatically extract your rules, Dryv consistently outputs a **validation set**—a structured JavaScript object that looks like this:

```javascript
{
    name: "MyModel",
    validators: {
        "propertyName": [
            {
                name: "optionalRuleName",      // from DryvRuleSettings
                group: "optionalGroupName",    // for multi-property deduplication
                async: true,                   // only present when true
                related: ["otherProp"],        // related property paths
                annotations: { ... },          // custom metadata from annotators
                validate: function ($m, $ctx) {
                    // returns null (success) or { type: "error"|"warning", text: "..." }
                }
            }
        ]
    },
    disablers: {
        "propertyName": [
            function ($m, $ctx) {
                // returns true if validation for this property should be skipped
            }
        ]
    },
    parameters: {
        "paramName": value   // resolved at translation time or at runtime
    }
}
```

**Key points:**

- **`validators`** is a dictionary keyed by the camelCase property path (e.g. `"city"`, `"address.zipCode"`). Each key maps to an array of rule objects.
- **`disablers`** has the same structure — if any disabler returns `true`, all validators for that property are skipped.
- **`parameters`** contains named values defined via `.Parameter(...)`.
- Property names are **always camelCase** regardless of the C# naming convention.

### How the Script Is Rendered on the Page

When you use the Tag Helper (`<dryv-client-rules>`) or HTML Helper (`@await Html.DryvValidation<T>()`), Dryv renders a `<script>` tag that registers the rule set on the global `window.dryv.v` object. Here's the actual pattern used in production (example from [yello.de](https://www.yello.de/)):

```html
<script>
(function(dryv) {
  if (!dryv.v) { dryv.v = {}; }
  dryv.v["tariff-builder-query"] = {
    name: "tariff-builder-query",
    validators: {
      "zipCode": [
        {
          validate: function($m, $ctx) {
            return /\d{5,5}/.test($m.zipCode)
              ? null
              : { type: "error", text: "Deine Postleitzahl muss aus 5 Ziffern bestehen.", group: null }
          }
        },
        {
          async: true,
          validate: function($m, $ctx) {
            return $ctx.dryv.callServer('/_v/cpk97zzro', 'POST', { "zipCode": $m.zipCode })
              .then(function($r) { return $ctx.dryv.handleResult($ctx, $m, "zipCode", null, $r); })
          }
        }
      ],
      "consumption": [
        {
          annotations: { "required": true },
          validate: function($m, $ctx) {
            return !/\S/.test($m.consumption || "")
              ? { type: "error", text: "Gib bitte deinen Jahresverbrauch an.", group: null }
              : null
          }
        },
        {
          async: true,
          validate: function($m, $ctx) {
            return $ctx.dryv.callServer('/_v/ch0smecjn', 'POST', { "consumption": $m.consumption, "sector": $m.sector })
              .then(function($r) { return $ctx.dryv.handleResult($ctx, $m, "consumption", null, $r); })
          }
        }
      ]
    },
    disablers: {
      "consumption": [
        { validate: function($m, $ctx) { return $m.consumption === "frei" } }
      ]
    },
    parameters: {}
  }
})(window.dryv || (window.dryv = {}));
</script>
```

**Key observations:**

- The IIFE pattern `(function(dryv) { ... })(window.dryv || (window.dryv = {}))` ensures the global `window.dryv` namespace exists.
- All rule sets are stored under `window.dryv.v["name"]`, making them globally discoverable.
- Multiple `<script>` tags can coexist on the same page — each one registers an additional rule set.
- The rules contain real JavaScript functions (not JSON), so they can reference `$m` and `$ctx` directly.

### Automatic Discovery by DryvJS / Dryvue

The `window.dryv.v` convention is exactly what DryvJS and Dryvue expect. You can pass the entire `window.dryv.v` object to the `DryvStaticRuleSets` plugin, and all rule sets become automatically available by name:

```typescript
import { createApp } from 'vue'
import { Dryv, DryvStaticRuleSets } from 'dryvue'
import App from './App.vue'

const app = createApp(App)
app.use(Dryv)

// Pass all server-rendered rule sets — DryvJS discovers them automatically
app.use(DryvStaticRuleSets, window.dryv.v)

app.mount('#app')
```

Then in any component, reference a rule set by its name:

```vue
<script setup lang="ts">
import { reactive } from 'vue'
import { useDryv } from 'dryvue'

const data = reactive({ zipCode: '', consumption: '' })

// Automatically resolves "tariff-builder-query" from window.dryv.v
const { validatable, validate, valid } = useDryv(data, 'tariff-builder-query')
</script>

<template>
  <form @submit.prevent="validate">
    <input v-model="validatable.zipCode.value" placeholder="PLZ" />
    <span v-if="validatable.zipCode.hasErrors">{{ validatable.zipCode.text }}</span>

    <input v-model="validatable.consumption.value" placeholder="Verbrauch" />
    <span v-if="validatable.consumption.hasErrors">{{ validatable.consumption.text }}</span>

    <button type="submit" :disabled="!valid">Weiter</button>
  </form>
</template>
```

For vanilla JS/TS (without Vue), access rule sets directly from `window.dryv.v`:

```typescript
import { DryvValidationSession, DryvObjectValidator, defaultDryvOptions } from 'dryvjs'

// Grab the server-rendered rule set by name
const ruleSet = window.dryv.v['tariff-builder-query']

const model = { zipCode: '12345', consumption: '2500', sector: 'strom' }
const options = { ...defaultDryvOptions }
const session = new DryvValidationSession(options, ruleSet)
const validator = new DryvObjectValidator(model, session, undefined, options)

const result = await validator.validate()
```

### The Validation Function Signature

Every translated validation function has the same signature:

```javascript
function ($m, $ctx) { ... }
```

- **`$m`** — the model object being validated. Properties are camelCase mirrors of the C# model.
- **`$ctx`** — the validation context object. This is where Dryv puts runtime helpers:
  - `$ctx.parameter("name")` — retrieves a named parameter value.
  - `$ctx.dryv.callServer(url, method, data)` — calls a dynamic controller endpoint (for async rules).
  - `$ctx.dryv.handleResult(ctx, model, prop, group, result)` — processes the server response.
  - `$ctx.dryv.parseDate(value, culture, format)` — parses date strings for comparison.
  - `$ctx.dryv.format(value, type, format)` — formats values (used for `.ToString(format)` translations).

**Return values:**

| Return | Meaning |
|--------|---------|
| `null` | Validation passed (success) |
| `{ type: "error", text: "..." }` | Validation failed with an error |
| `{ type: "warning", text: "..." }` | Validation produced a warning |
| `{ type: "error", text: "...", group: "..." }` | Error with a group for deduplication |
| A `Promise` resolving to any of the above | Async validation (server round-trip) |

### Running Validators in Vanilla JavaScript

Here is a minimal example of consuming Dryv output without any framework:

```html
<script>
// This is what Dryv generates (via Tag Helper or API):
var validationSet = {
    name: "Address",
    validators: {
        "city": [{
            validate: function ($m, $ctx) {
                return !/\S/.test($m.city || "")
                    ? { type: "error", text: "Please enter a city." }
                    : null;
            }
        }],
        "zipCode": [{
            validate: function ($m, $ctx) {
                return !/\S/.test($m.zipCode || "")
                    ? { type: "error", text: "Please enter a ZIP code." }
                    : null;
            }
        }]
    },
    disablers: {},
    parameters: {}
};

// Your validation runner:
function validateModel(model, validationSet) {
    var ctx = {
        parameter: function (name) {
            return validationSet.parameters[name];
        },
        dryv: {
            // Provide helpers if your rules use them:
            format: function (v) { return String(v); },
            parseDate: function (v) { return new Date(v); }
        }
    };

    var errors = {};

    for (var prop in validationSet.validators) {
        // Check disablers first
        var disablers = validationSet.disablers[prop] || [];
        var disabled = disablers.some(function (d) { return d(model, ctx); });
        if (disabled) continue;

        // Run all validators for this property
        var rules = validationSet.validators[prop];
        for (var i = 0; i < rules.length; i++) {
            var result = rules[i].validate(model, ctx);
            if (result && result.type === "error") {
                errors[prop] = result;
                break; // stop at first error for this property
            }
        }
    }

    return errors;
}

// Usage:
var model = { city: "", zipCode: "1234" };
var errors = validateModel(model, validationSet);
console.log(errors);
// { city: { type: "error", text: "Please enter a city." } }
</script>
```

### Handling Async Rules on the Client

Async rules (those routed through dynamic controllers) return a `Promise` instead of a plain result. You need to handle both sync and async returns:

```javascript
async function validateProperty(model, validationSet, propertyName) {
    var ctx = { /* ... build context as above ... */ };
    var rules = validationSet.validators[propertyName] || [];

    for (var i = 0; i < rules.length; i++) {
        var result = rules[i].validate(model, ctx);

        // Async rules return a Promise
        if (result && typeof result.then === "function") {
            result = await result;
        }

        if (result && (result.type === "error" || result.type === "warning")) {
            return result;
        }
    }

    return null; // all rules passed
}
```

For async rules to work, you must provide the `$ctx.dryv.callServer` function:

```javascript
var ctx = {
    dryv: {
        callServer: function (url, method, data) {
            return fetch(url, {
                method: method,
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(data)
            }).then(function (response) {
                return response.json();
            });
        },
        handleResult: function (ctx, model, prop, group, result) {
            return result; // pass through the server response
        }
    }
};
```

### Using Parameters on the Client

Parameters are dynamic values that should be fetched from the server at runtime (not baked in at build time). Load them via the parameters API endpoint and inject them into the context:

```javascript
// Fetch parameters from your API
var params = await fetch("/api/validation/parameters/Address").then(r => r.json());

// Build the context with parameter support
var ctx = {
    parameter: function (name) {
        return params[name];
    }
};

// Now rules like `$ctx.parameter("maxAge")` resolve correctly
```

If you use the Tag Helper with server-rendered pages, parameters are already baked into the `parameters` property of the validation set object and you can read them directly:

```javascript
ctx.parameter = function (name) {
    return validationSet.parameters[name];
};
```

### Working with Groups and Related Properties

Multi-property rules produce a **group** name and a **related properties** list. This metadata lets you build smart UIs:

```javascript
// Example: a multi-property rule on Street, City, Zip
// All three get the same rule with group: "street" and related: ["city", "zip"]

function validateWithGroups(model, validationSet) {
    var errors = {};
    var shownGroups = new Set();

    for (var prop in validationSet.validators) {
        var rules = validationSet.validators[prop];
        for (var i = 0; i < rules.length; i++) {
            var rule = rules[i];
            var result = rule.validate(model, { /* ctx */ });

            if (result && result.type === "error") {
                // Deduplicate by group — show the error only once
                var group = result.group || rule.group;
                if (group && shownGroups.has(group)) continue;
                if (group) shownGroups.add(group);

                errors[prop] = result;
                break;
            }
        }
    }

    return errors;
}
```

The **`related`** array tells you which other properties to re-validate when one changes. For example, if the user changes `city`, you should also re-run validation for `street` and `zip`:

```javascript
function onFieldChange(fieldName, model, validationSet) {
    // Validate the changed field
    validateProperty(model, validationSet, fieldName);

    // Also re-validate related fields
    var rules = validationSet.validators[fieldName] || [];
    rules.forEach(function (rule) {
        if (rule.related) {
            rule.related.forEach(function (relatedProp) {
                validateProperty(model, validationSet, relatedProp);
            });
        }
    });
}
```

### Integration with Frontend Frameworks

#### Official Client Libraries: DryvJS and Dryvue

The recommended way to consume Dryv's generated validation rules on the client is through the official companion packages:

| Package | Description | Install |
|---------|-------------|---------|
| [`dryvjs`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvjs) | Framework-agnostic validation engine | `npm install dryvjs` |
| [`dryvue`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvue) | Vue 3 reactive bindings for DryvJS | `npm install dryvue` |

These packages consume the exact `DryvValidationRuleSet` structure that Dryv (C#) generates, providing:

- **Reactive validation** — field changes automatically trigger validation
- **Nested object & array validation** — recursive validator tree matching your model
- **Async & server-side rules** — built-in `callServer` / `handleResult` for dynamic controller endpoints
- **Dirty tracking** — `commit()` / `revert()` semantics
- **Grouped validation messages** — consolidated error display for multi-property rules
- **Related fields** — automatic cross-field re-validation
- **Disablers** — conditional field disabling
- **Parameters** — runtime parameter injection via `session.parameter(name)`
- **Warnings vs. errors** — distinguishes severity levels
- **Validation triggers** — `immediate`, `auto`, `manual`, or `autoAfterManual`

---

#### Using DryvJS (Vanilla TypeScript)

DryvJS is the core, framework-agnostic engine. It directly accepts the rule set objects that Dryv generates:

```typescript
import {
  DryvValidationSession,
  DryvObjectValidator,
  defaultDryvOptions,
  type DryvValidationRuleSet,
  type DryvOptions
} from 'dryvjs'

// This is the rule set generated by Dryv (C#) — via Tag Helper, API, or build-time codegen:
interface Address {
  city: string
  zipCode: string
}

const ruleSet: DryvValidationRuleSet<Address> = {
  name: 'Address',
  validators: {
    city: [{
      annotations: { required: true },
      validate: ($m) => !/\S/.test($m.city || '')
        ? { type: 'error', text: 'Please enter a city.' }
        : null
    }],
    zipCode: [{
      annotations: { required: true },
      validate: ($m) => ($m.zipCode || '').trim().length < 5
        ? { type: 'error', text: 'ZIP code must have at least 5 characters.' }
        : null
    }]
  },
  parameters: {}
}

// Create a session and validator
const options: DryvOptions = { ...defaultDryvOptions }
const session = new DryvValidationSession<Address>(options, ruleSet)
const model: Address = { city: '', zipCode: '123' }
const validator = new DryvObjectValidator<Address>(model, session, undefined, options)

// Validate the entire form
const result = await validator.validate()
console.log(result.success)    // false
console.log(result.hasErrors)  // true
console.log(result.results)    // Array of DryvFieldValidationResult
```

**Customizing DryvJS options (server calls, date parsing, etc.):**

```typescript
const options: DryvOptions = {
  ...defaultDryvOptions,

  // Custom server call for async rules (dynamic controllers)
  callServer: async (url, method, data) => {
    const response = await fetch(url, {
      method,
      headers: { 'Content-Type': 'application/json' },
      body: data ? JSON.stringify(data) : undefined
    })
    return response.json()
  },

  // Custom date parsing (for DateTime comparison rules)
  parseDate: (date, locale, format) => new Date(date).valueOf(),

  // Custom formatting (for ToString rules)
  format: (data, type, pattern) => data?.toString() ?? '',

  // Base URL for dynamic controller endpoints
  baseUrl: 'https://myapp.example.com',

  // Validation trigger strategy
  validationTrigger: 'autoAfterManual'
}
```

**Async rules with server-side validation:**

```typescript
// Rules generated by Dryv for async validation look like this:
const ruleSet: DryvValidationRuleSet<MyForm> = {
  name: 'MyForm',
  validators: {
    email: [{
      async: true,
      validate: ($m, session) => {
        return session.dryv
          .callServer('/_v/c8a3f2...', 'POST', { email: $m.email })
          .then(($r) => session.dryv.handleResult(session, $m, 'email', null, $r))
      }
    }]
  }
}
// DryvJS handles the async call and result processing automatically.
```

**Nested objects and dot-notation paths:**

```typescript
interface OrderForm {
  customer: { name: string }
  address: { city: string; zip: string }
}

const ruleSet: DryvValidationRuleSet<OrderForm> = {
  name: 'OrderForm',
  validators: {
    'customer.name': [{
      annotations: { required: true },
      validate: ($m) => !$m.name ? 'Customer name is required' : null
    }],
    'address.city': [{
      annotations: { required: true },
      validate: ($m) => !$m.city ? 'City is required' : null
    }]
  }
}
// DryvJS builds a recursive validator tree matching the nested structure.
```

**Parameters (runtime values from the server):**

```typescript
const ruleSet: DryvValidationRuleSet<MyForm> = {
  name: 'MyForm',
  validators: {
    birthDate: [{
      validate: ($m, session) => {
        const maxDate = session.parameter('maxBirthDate')
        return session.dryv.parseDate($m.birthDate, 'en-US', 'YYYY-MM-DD') >
          session.dryv.parseDate(maxDate, 'en-US', 'YYYY-MM-DD')
          ? { type: 'error', text: 'You must be at least 18 years old.' }
          : null
      }
    }]
  },
  parameters: {
    maxBirthDate: '2007-06-07'  // Provided by the server at runtime
  }
}
```

**Validator hierarchy:**

```
DryvValidator (abstract base)
├── DryvFieldValidator       — Leaf validator for scalar fields
└── DryvCompositeValidator   — Base for composite validators
    ├── DryvObjectValidator  — Validates objects with named fields
    └── DryvArrayValidator   — Validates arrays of items
```

---

#### Using Dryvue (Vue 3)

Dryvue wraps DryvJS with Vue 3 reactivity, giving you computed validation state, automatic re-validation on model changes, and seamless `v-model` integration.

**1. Register the plugin:**

```typescript
import { createApp } from 'vue'
import { Dryv, DryvStaticRuleSets } from 'dryvue'
import { addressRules } from './rules/address'  // generated or imported rule sets
import App from './App.vue'

const app = createApp(App)
app.use(Dryv)  // Sets Vue.reactive() as the reactivity wrapper
app.use(DryvStaticRuleSets, {
  Address: addressRules  // Register rule sets by name (case-insensitive)
})
app.mount('#app')
```

**2. Use the `useDryv` composable:**

```vue
<template>
  <form @submit.prevent="onSubmit">
    <div>
      <label>City<span v-if="validatable.city.required">*</span>:</label>
      <input v-model="validatable.city.value" />
      <span v-if="validatable.city.hasErrors" class="error">
        {{ validatable.city.text }}
      </span>
    </div>

    <div>
      <label>ZIP Code<span v-if="validatable.zipCode.required">*</span>:</label>
      <input v-model="validatable.zipCode.value" />
      <span v-if="validatable.zipCode.hasErrors" class="error">
        {{ validatable.zipCode.text }}
      </span>
    </div>

    <button type="submit" :disabled="!valid">Save</button>
    <button type="button" @click="revert" :disabled="!dirty">Reset</button>
  </form>
</template>

<script setup lang="ts">
import { reactive } from 'vue'
import { useDryv } from 'dryvue'

interface Address {
  city: string
  zipCode: string
}

const data = reactive<Address>({ city: '', zipCode: '' })

// Use a registered rule set name or pass the rule set object directly:
const { validatable, validate, valid, dirty, commit, revert, setValidationResult } =
  useDryv(data, 'Address')

async function onSubmit() {
  const result = await validate()
  if (!result.success) return

  try {
    const response = await fetch('/api/address', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(data)
    })
    const serverResult = await response.json()

    if (!serverResult.success) {
      // Apply server-side validation errors to the form
      setValidationResult(serverResult)
    }
  } catch {
    // handle network error
  }
}
</script>
```

**Per-field reactive state available via `validatable`:**

| Property | Type | Description |
|----------|------|-------------|
| `validatable.field.value` | `T` | Current field value (two-way bindable with `v-model`) |
| `validatable.field.text` | `string \| null` | Validation message text |
| `validatable.field.type` | `'error' \| 'warning' \| 'success' \| null` | Result type |
| `validatable.field.hasErrors` | `boolean` | Whether the field has an error |
| `validatable.field.hasWarnings` | `boolean` | Whether the field has a warning |
| `validatable.field.required` | `boolean` | From `annotations.required` |
| `validatable.field.isDirty` | `boolean` | Whether the value has changed |
| `validatable.field.group` | `string \| null` | Group name |
| `validatable.field.groupShown` | `boolean` | Whether the group error is shown elsewhere |

**3. Async parameter loading:**

```typescript
const { validatable, validate, parameters } = await useDryv(data, 'Address', {
  loadParameters: async (ruleSetName) => {
    const response = await fetch(`/api/validation-params/${ruleSetName}`)
    return response.json()
  }
})

// Parameters are a writable ref — can be updated at runtime:
parameters.value = { maxBirthDate: '2007-06-07' }
```

**4. Reusable validatable input components:**

```vue
<!-- ValidatingInput.vue -->
<template>
  <div>
    <label>{{ label }}<span v-if="field.required">*</span>:</label>
    <input v-model="field.value" />
    <div class="error" v-if="field.hasErrors && !field.groupShown">
      {{ field.text }}
    </div>
  </div>
</template>

<script setup lang="ts">
import { type DryvValidatable, useDryvValueProp } from 'dryvue'

const props = defineProps<{
  modelValue: string | DryvValidatable<any> | undefined
  label: string
}>()

const emit = defineEmits(['update:modelValue'])
const field = useDryvValueProp(emit, () => props.modelValue)
</script>
```

Usage:
```vue
<validating-input v-model="validatable.city" label="City" />
<validating-input v-model="validatable.zipCode" label="ZIP Code" />
```

**5. Validation group slots (consolidated group errors):**

```vue
<template>
  <div v-if="group?.hasErrors" class="group-error">
    {{ group.text }}
  </div>
</template>

<script setup lang="ts">
import { useDryvGroupSlot } from 'dryvue'
const group = useDryvGroupSlot(['contact'])
</script>
```

**6. Lifecycle methods:**

```typescript
const { validate, clear, commit, revert, reset, setValidationResult } = useDryv(data, ruleSet)

await validate()              // Trigger full form validation
clear()                       // Clear all messages (keep dirty state)
commit()                      // Set current values as new baseline
revert()                      // Restore to last committed state
reset()                       // Commit + reset session (clears triggered state)
setValidationResult(response) // Apply server-returned errors
```

---

#### End-to-End Flow: Dryv (C#) + DryvJS/Dryvue

Here's how the full stack works together:

```
┌──────────────────────────────────────────────────────────────────────────┐
│  C# (Server)                                                              │
│                                                                            │
│  1. Define rules on models using DryvRules                                 │
│  2. Dryv translates rules to JavaScript functions                          │
│  3. Output via Tag Helper, API endpoint, or build-time codegen             │
│     → produces a DryvValidationRuleSet JSON/JS object                      │
│  4. Dynamic controllers handle async rules (server-side validation)        │
└────────────────────────────────────┬─────────────────────────────────────┘
                                     │  DryvValidationRuleSet
                                     ▼
┌──────────────────────────────────────────────────────────────────────────┐
│  JavaScript (Client)                                                      │
│                                                                            │
│  5. DryvJS / Dryvue consumes the rule set                                  │
│  6. Builds a validator tree mirroring the model                            │
│  7. Runs validate functions on field change or form submit                 │
│  8. For async rules: calls dynamic controller endpoints via callServer     │
│  9. Displays errors/warnings reactively                                    │
│ 10. On submit: sends model to server → applies server validation results   │
└──────────────────────────────────────────────────────────────────────────┘
```

---

#### Manual Integration (Without DryvJS)

If you prefer not to use DryvJS (e.g., for React or Angular where no official Dryv binding exists yet), you can write your own validation runner. The generated rule set is plain JavaScript — see [Running Validators in Vanilla JavaScript](#running-validators-in-vanilla-javascript) above for a minimal implementation.

#### React

```jsx
import { useState, useCallback } from 'react';

function useValidation(validationSet) {
    const [errors, setErrors] = useState({});

    const ctx = {
        parameter: (name) => validationSet.parameters[name],
        dryv: {
            callServer: (url, method, data) =>
                fetch(url, {
                    method,
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data),
                }).then(r => r.json()),
            handleResult: (_ctx, _model, _prop, _group, result) => result,
            format: (v) => String(v),
            parseDate: (v) => new Date(v),
        },
    };

    const validate = useCallback(async (model) => {
        const newErrors = {};
        for (const [prop, rules] of Object.entries(validationSet.validators)) {
            const disablers = validationSet.disablers?.[prop] || [];
            if (disablers.some(d => d(model, ctx))) continue;

            for (const rule of rules) {
                let result = rule.validate(model, ctx);
                if (result?.then) result = await result;
                if (result?.type === 'error') {
                    newErrors[prop] = result;
                    break;
                }
            }
        }
        setErrors(newErrors);
        return Object.keys(newErrors).length === 0;
    }, [validationSet]);

    return { errors, validate };
}

// Usage in a component:
function AddressForm({ validationSet }) {
    const [model, setModel] = useState({ city: '', zipCode: '' });
    const { errors, validate } = useValidation(validationSet);

    const handleSubmit = async (e) => {
        e.preventDefault();
        if (await validate(model)) {
            // submit to server
        }
    };

    return (
        <form onSubmit={handleSubmit}>
            <input
                value={model.city}
                onChange={e => setModel({ ...model, city: e.target.value })}
            />
            {errors.city && <span className="error">{errors.city.text}</span>}

            <input
                value={model.zipCode}
                onChange={e => setModel({ ...model, zipCode: e.target.value })}
            />
            {errors.zipCode && <span className="error">{errors.zipCode.text}</span>}

            <button type="submit">Save</button>
        </form>
    );
}
```

#### Angular

```typescript
import { Injectable } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class DryvValidationService {
    async validate(model: any, validationSet: any): Promise<Record<string, any>> {
        const ctx = {
            parameter: (name: string) => validationSet.parameters[name],
            dryv: {
                callServer: (url: string, method: string, data: any) =>
                    fetch(url, {
                        method,
                        headers: { 'Content-Type': 'application/json' },
                        body: JSON.stringify(data),
                    }).then(r => r.json()),
                handleResult: (_ctx: any, _m: any, _p: any, _g: any, result: any) => result,
                format: (v: any) => String(v),
                parseDate: (v: any) => new Date(v).valueOf(),
            },
        };

        const errors: Record<string, any> = {};
        for (const [prop, rules] of Object.entries<any[]>(validationSet.validators)) {
            const disablers = validationSet.disablers?.[prop] || [];
            if (disablers.some((d: any) => d(model, ctx))) continue;

            for (const rule of rules) {
                let result = rule.validate(model, ctx);
                if (result?.then) result = await result;
                if (result?.type === 'error') {
                    errors[prop] = result;
                    break;
                }
            }
        }
        return errors;
    }
}
```

```typescript
// Usage in an Angular component:
import { Component } from '@angular/core';
import { DryvValidationService } from './dryv-validation.service';

@Component({
    selector: 'app-address-form',
    template: `
        <form (ngSubmit)="onSubmit()">
            <input [(ngModel)]="model.city" name="city" (blur)="validateField()" />
            <span class="error" *ngIf="errors['city']">{{ errors['city'].text }}</span>

            <input [(ngModel)]="model.zipCode" name="zipCode" (blur)="validateField()" />
            <span class="error" *ngIf="errors['zipCode']">{{ errors['zipCode'].text }}</span>

            <button type="submit">Save</button>
        </form>
    `
})
export class AddressFormComponent {
    model = { city: '', zipCode: '' };
    errors: Record<string, any> = {};

    constructor(private dryvService: DryvValidationService) {}

    async validateField() {
        this.errors = await this.dryvService.validate(this.model, this.validationSet);
    }

    async onSubmit() {
        this.errors = await this.dryvService.validate(this.model, this.validationSet);
        if (Object.keys(this.errors).length === 0) {
            // submit to server
        }
    }

    // Injected from the server (e.g. via a script tag or API call)
    validationSet: any;
}
```




## C# to JavaScript Translation — Reference

This section provides a detailed reference for every C# construct that Dryv can translate to JavaScript, with realistic examples derived from the project's unit test suite.

### String Methods Reference

Dryv's `StringTranslator` handles most `System.String` methods. All examples assume the model property is `m.Text`.

| C# | JavaScript | Notes |
|----|-----------|-------|
| `string.IsNullOrWhiteSpace(m.Text)` | `!/\S/.test(m.text \|\| "")` | Uses regex; `\|\|` provides empty string fallback for `null` |
| `string.IsNullOrEmpty(m.Text)` | `!(m.text \|\| "").length` | |
| `m.Text.StartsWith("xy")` | `m.text.indexOf("xy") === 0` | |
| `m.Text.StartsWith("xy", OrdinalIgnoreCase)` | `m.text.toLowerCase().indexOf("xy".toLowerCase()) === 0` | Case-insensitive variant |
| `m.Text.EndsWith("xy")` | `m.text.indexOf("xy", m.text.length - "xy".length) !== -1` | |
| `m.Text.EndsWith("xy", OrdinalIgnoreCase)` | Case-insensitive variant using `.toLowerCase()` | |
| `m.Text.Contains("xy")` | `m.text.indexOf("xy") !== -1` | |
| `m.Text.Equals("x", OrdinalIgnoreCase)` | `m.text.toLowerCase() === "x".toLowerCase()` | Both sides lowercased |
| `m.Text.CompareTo("x")` | `m.text.localeCompare("x")` | |
| `string.Compare(m.Text, "x", CurrentCulture)` | `m.text.localeCompare("x")` | Static variant |
| `string.Compare(m.Text, "x", OrdinalIgnoreCase)` | `m.text.toLowerCase().localeCompare("x".toLowerCase())` | |
| `m.Text.IndexOf("x")` | `m.text.indexOf("x")` | |
| `m.Text.IndexOf("x", OrdinalIgnoreCase)` | `m.text.toLowerCase().indexOf("x".toLowerCase())` | |
| `m.Text.Trim()` | `m.text.trim()` | |
| `m.Text.ToLower()` / `ToUpper()` | `m.text.toLowerCase()` / `.toUpperCase()` | |
| `m.Text.Length` | `m.text.length` | |
| `m.Text.Substring(2)` | `m.text.substring(2)` | |
| `m.Text.Substring(2, 5)` | `m.text.substring(2, 5)` | |

**Complete rule example (from unit tests):**

```csharp
// C# rule:
.Rule(m => m.Text,
    m => string.IsNullOrWhiteSpace(m.Text)
        ? "Text is required"
        : DryvValidationResult.Success)

// Translated JavaScript:
function ($m, $ctx) {
    return !/\S/.test($m.text || "")
        ? { type: "error", text: "Text is required" }
        : null;
}
```

```csharp
// Case-insensitive equality check:
.Rule(m => m.Text,
    m => m.Text.Equals("Oscorp", StringComparison.OrdinalIgnoreCase)
        ? "fail"
        : DryvValidationResult.Success)

// JavaScript:
function ($m, $ctx) {
    return $m.text.toLowerCase() === "oscorp".toLowerCase()
        ? { type: "error", text: "fail" }
        : null;
}
```

### String Interpolation and Formatting

C# string interpolation (`$"..."`) is translated to JavaScript string concatenation.

```csharp
// C# interpolation:
m => $"123{m.Text}abc"

// JavaScript:
"123" + $m.text + "abc"
```

Format specifiers are translated to calls to `$ctx.dryv.format(value, type, formatString)`:

```csharp
// C# with format specifier:
m => $"{m.IntItem:D}"

// JavaScript:
$ctx.dryv.format($m.intItem, "int32", "D")
```

`ToString()` is similarly translated:

```csharp
// C# ToString():
m => m.Text.ToString()
// JS: $ctx.dryv.format($m.text, "string")

// C# ToString with format:
m => m.IntItem.ToString("D")
// JS: $ctx.dryv.format($m.intItem, "int32", "D")
```

> **Note:** You must provide a `$ctx.dryv.format` implementation on the client that handles the format specifiers your rules use.

### Regular Expressions Reference

Dryv's `RegexTranslator` converts C# `System.Text.RegularExpressions.Regex` to JavaScript RegExp.

| C# | JavaScript |
|----|-----------|
| `new Regex(@"^\d+$").IsMatch(m.Text)` | `/^\d+$/.test($m.text)` |
| `Regex.IsMatch(pattern, m.Text)` | `/pattern/.test($m.text)` |
| `new Regex(pattern).Match(m.Text).Success` | `/pattern/.test($m.text)` |
| `Regex.Match(pattern, m.Text).Success` | `/pattern/.test($m.text)` |
| `!new Regex(p).IsMatch(m.Text)` | `!/p/.test($m.text)` |

**`RegexOptions` mapping:**

| C# Option | JavaScript Flag |
|-----------|----------------|
| `RegexOptions.IgnoreCase` | `i` |
| `RegexOptions.Singleline` | (ignored — JS has no exact equivalent) |
| `RegexOptions.Multiline` | `m` |

```csharp
// Instance with options:
.Rule(m => m.Text,
    m => !new Regex(@"^\d+$", RegexOptions.IgnoreCase | RegexOptions.Singleline).IsMatch(m.Text)
        ? "Must be numeric"
        : DryvValidationResult.Success)

// JavaScript:
function ($m, $ctx) {
    return !/^\d+$/i.test($m.text)
        ? { type: "error", text: "Must be numeric" }
        : null;
}
```

**Regex from variables and fields:**

Dryv captures `Regex` patterns from fields, properties, and local variables at translation time:

```csharp
private static readonly string Pattern = @"^\d+$";

.Rule(m => m.Text,
    m => new Regex(Pattern).IsMatch(m.Text)
        ? DryvValidationResult.Success
        : "Invalid format")

// The pattern value "^\d+$" is extracted and inlined into the JavaScript regex literal.
```

### LINQ / Collections Reference

Dryv's `EnumerableTranslator` translates LINQ extension methods on `IEnumerable<T>` to their JavaScript array equivalents.

| C# | JavaScript | Notes |
|----|-----------|-------|
| `items.Any(x => x.Length > 1)` | `items.some(function(x) { return x.length > 1; })` | |
| `items.All(x => x.StartsWith("x"))` | `items.every(function(x) { return x.indexOf("x") === 0; })` | |
| `items.Where(x => x.Length > 1)` | `items.filter(function(x) { return x.length > 1; })` | |
| `items.Select(x => x.Length)` | `items.map(function(x) { return x.length; })` | |
| `items.First(x => x.Length > 1)` | `items.find(function(x) { return x.length > 1; })` | |
| `items.FirstOrDefault(x => ...)` | `items.find(function(x) { ... }) \|\| null` | Adds `null` fallback |
| `items.Last(x => ...)` | Reversed `find` | |
| `items.LastOrDefault(x => ...)` | Reversed `find` with `null` fallback | |
| `items.Count()` | `items.length` | |
| `items.Count(x => x.Length > 1)` | `items.filter(function(x) { return x.length > 1; }).length` | |
| `items.Contains("x")` | `items.includes("x")` | |
| `items.ElementAt(2)` | `items[2]` | |
| `items.ElementAtOrDefault(3)` | `items[3] \|\| null` | |
| `items.DefaultIfEmpty()` | Array with default value if empty | |
| `items.Sum(x => x * 2)` | `items.reduce(function(a, x) { return a + x * 2; }, 0)` | |
| `items.Average(x => x.Length)` | `items.reduce(...) / items.length` | |
| `items.Min(x => x.Length)` | `items.reduce(function(a, x) { return Math.min(a, x.length); }, ...)` | |
| `items.Max(x => x.Length)` | `items.reduce(function(a, x) { return Math.max(a, x.length); }, ...)` | |

**Complete example:**

```csharp
// C# — all items must start with "x":
.Rule(m => m.Items,
    m => m.Items.All(s => s.StartsWith("x"))
        ? null
        : "All items must start with x")

// JavaScript:
function ($m, $ctx) {
    return $m.items.every(function(s) { return s.indexOf("x") === 0; })
        ? null
        : { type: "error", text: "All items must start with x" };
}
```

```csharp
// C# — count items with filter:
m => m.Items.Count(i => i.Length > 1)

// JavaScript:
$m.items.filter(function(i) { return i.length > 1; }).length
```

### Array Support

Array properties are translated directly. The `Length` property maps to JavaScript's `length`:

```csharp
// C#:
m => m.ItemArray.Length

// JavaScript:
$m.itemArray.length
```

### DateTime Comparisons

Dryv's `DateTimeTranslator` translates `DateTime` and `DateTimeOffset` comparisons by wrapping both sides in `$ctx.dryv.parseDate(value, culture, format)`:

```csharp
// C# — comparing two DateTime values:
.Rule(m => m.StartDate,
    m => m.StartDate > m.EndDate
        ? "Start date must be before end date"
        : null)

// JavaScript (conceptual):
function ($m, $ctx) {
    return $ctx.dryv.parseDate($m.startDate, "en-US", "M/d/yyyy h:mm:ss tt")
         > $ctx.dryv.parseDate($m.endDate, "en-US", "M/d/yyyy h:mm:ss tt")
        ? { type: "error", text: "Start date must be before end date" }
        : null;
}
```

The culture name and format strings are evaluated at translation time from `CultureInfo.CurrentUICulture`. For `DateTimeOffset`, the format includes the timezone offset (`zzz`).

> **Important:** You must provide a `$ctx.dryv.parseDate` implementation on the client. Use a library like Moment.js, Day.js, or Luxon. Dryv uses `MomentJsFormatConverter` internally to convert .NET format strings to Moment.js format tokens.

### Enum Handling

Enum values are serialized using the configured JSON serialization strategy (typically as strings):

```csharp
// C#:
public enum Status { Draft, Published, Archived }

.Rule(m => m.Status,
    m => m.Status != Status.Published
        ? null
        : "Cannot modify published items")

// JavaScript (with string enum serialization):
function ($m, $ctx) {
    return $m.status !== "Published"
        ? null
        : { type: "error", text: "Cannot modify published items" };
}
```

The exact serialization depends on your `DryvOptions.JsonConversion` setting. By default, enums are serialized as their name strings.

### DryvValidationResult Translation

`DryvValidationResult` factory methods are translated to JavaScript object literals:

| C# | JavaScript |
|----|-----------|
| `DryvValidationResult.Success` | `null` |
| `DryvValidationResult.Error("text")` | `{ type: "error", text: "text" }` |
| `DryvValidationResult.Warning("text")` | `{ type: "warning", text: "text" }` |
| `"error message"` (implicit conversion) | `{ type: "error", text: "error message" }` |
| `null` (return null) | `null` |

```csharp
// C# using explicit factory:
m => m.Text == "test"
    ? DryvValidationResult.Error("fail")
    : null

// JavaScript:
$m.text === "test"
    ? { type: "error", text: "fail" }
    : null
```

### DryvParameters Translation

`DryvParameters.Get<T>("name")` is translated to `$ctx.parameter("name")`:

```csharp
// C#:
.Rule<DryvParameters>(
    m => m.Text,
    (m, p) => m.Text == p.Get<string>("testParameter")
        ? DryvValidationResult.Success
        : DryvValidationResult.Error("Parameter value doesn't match"))

// JavaScript:
function ($m, $ctx) {
    return $m.text === $ctx.parameter("testParameter")
        ? null
        : { type: "error", text: "Parameter value doesn't match" };
}
```

### IOptions\<T\> Inlining

When a rule injects `IOptions<T>`, the `Value` properties are **evaluated at translation time** and inlined as constants:

```csharp
// C# rule with IOptions:
.Rule<IOptions<TestOptions>>(
    m => m.Text,
    (m, o) => m.Text == o.Value.Text
        ? DryvValidationResult.Success
        : "fail")

// If options.Value.Text is "hello" at translation time, JavaScript becomes:
function ($m, $ctx) {
    return $m.text === "hello"
        ? null
        : { type: "error", text: "fail" };
}
```

This works because `IOptions<T>` values are typically static configuration. If the value changes at runtime, use [Rule Parameters](#rule-parameters) instead.

### Resource / Localization Strings

Static string properties (e.g. from `.resx` resource files) are evaluated at translation time and inlined:

```csharp
// C# using a localized string:
m => m.Text == null
    ? LocalizedStrings.String1
    : null

// If LocalizedStrings.String1 is "Please enter a value" at translation time:
function ($m, $ctx) {
    return $m.text === null
        ? { type: "error", text: "Please enter a value" }
        : null;
}
```

This means the generated JavaScript contains the string value from the server's current culture at translation time. If you need different languages, translate the rules per-culture or use parameters.

### Static Method Pre-evaluation

When a rule expression contains calls to static methods or accesses static fields/properties that don't depend on the model, Dryv **pre-evaluates** them at translation time and inlines the result:

```csharp
// C# — the Regex pattern is in a static field:
private static readonly Regex EmailRegex = new Regex(@"^[^@]+@[^@]+$");

.Rule(m => m.Email,
    m => EmailRegex.IsMatch(m.Email) ? null : "Invalid email")

// The Regex object is captured at translation time. JavaScript:
function ($m, $ctx) {
    return /^[^@]+@[^@]+$/.test($m.email) ? null : { type: "error", text: "Invalid email" };
}
```

This applies to:
- Static fields and properties on the enclosing class
- Captured local variables
- Instance fields/properties on captured closure objects
- Constants and `readonly` values

### Raw JavaScript Injection

For cases where you need to embed raw JavaScript that has no C# equivalent, use `DryvClientCode.Raw(...)`:

```csharp
using Dryv.Rules;

// Inject raw JavaScript into the validation output:
.Rule(m => m.Field,
    m => DryvClientCode.Raw("window.customValidator($m.field)")
        + DryvValidationResult.Success)
```

`DryvClientCode.Raw(string)` is a **marker method** that is never called at runtime — it throws `InvalidOperationException` if called. It only exists for the translator. The string argument is injected verbatim into the generated JavaScript.

> **Warning:** Raw JavaScript is not validated by Dryv. Use it sparingly and ensure the script is correct.

### Writing Custom Method Call Translators

To add translation support for your own types and methods, extend `MethodCallTranslator`:

```csharp
using Dryv.Translation;
using Dryv.Translation.Translators;

public class MyHelperTranslator : MethodCallTranslator
{
    public MyHelperTranslator()
    {
        // Register which C# type this translator handles
        Supports<MyHelper>();

        // Register a translator for a specific method name
        AddMethodTranslator(nameof(MyHelper.IsValid), context =>
        {
            // Write the JavaScript equivalent
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".isValid(");
            WriteArguments(context.Translator, context.Expression.Arguments, context);
            context.Writer.Write(")");
        });

        // You can register multiple methods
        AddMethodTranslator(nameof(MyHelper.Format), context =>
        {
            context.Writer.Write("formatHelper(");
            WriteArguments(context.Translator, context.Expression.Arguments, context);
            context.Writer.Write(")");
        });
    }
}
```

Register in DI:

```csharp
builder.Services.AddDryv(options =>
{
    options.Translators.Add<MyHelperTranslator>();
});
```

**Catch-all translator:** If you want to translate every method on a type 1:1 (same name, camelCased), use `AllMethodCallTranslator` as a reference:

```csharp
public class MyFallbackTranslator : MethodCallTranslator
{
    public MyFallbackTranslator()
    {
        // Match any method name with a regex
        AddMethodTranslator(new Regex(".*"), context =>
        {
            context.Translator.Translate(context.Expression.Object, context);
            context.Writer.Write(".");
            context.Writer.Write(context.Expression.Method.Name.ToCamelCase());
            context.Writer.Write("(");
            WriteArguments(context.Translator, context.Expression.Arguments, context);
            context.Writer.Write(")");
        });
    }

    public override bool SupportsType(Type type) => type == typeof(MyType);
}
```

### Writing Custom Expression Translators

For more complex translations that need to intercept entire expression nodes (not just method calls), implement `IDryvCustomTranslator`:

```csharp
using System.Linq.Expressions;
using Dryv.Translation;

public class MyCustomTranslator : IDryvCustomTranslator
{
    public int? OrderIndex { get; set; }

    public bool? AllowSurroundingBrackets(Expression expression)
    {
        // Return false to prevent the engine from wrapping your output in parentheses
        return true;
    }

    public bool TryTranslate(CustomTranslationContext context)
    {
        // Check if this expression is something you handle
        if (context.Expression is not BinaryExpression binary
            || binary.Left.Type != typeof(MySpecialType))
        {
            return false; // Let other translators handle it
        }

        // Write custom JavaScript
        context.Translator.Translate(binary.Left, context);
        context.Writer.Write(" /* custom op */ ");
        context.Translator.Translate(binary.Right, context);

        return true; // Handled
    }
}
```

Register:

```csharp
builder.Services.AddDryv(options =>
{
    options.Translators.Add<MyCustomTranslator>();
});
```

## Tips & Tricks

### 1. Keep Expressions Simple and Translatable
Dryv's `JavaScriptTranslator` is powerful, but it doesn't support every C# language feature.
- **Use ternary operators (`? :`)** instead of `if/else` statements.
- **Avoid local variables** inside the validation expressions.
- **Extract complex logic:** If a rule requires complex imperative logic that cannot be translated, either write it as a `.ServerRule(...)` (if it only needs to run on the server) or put the logic in an injected service. Un-translatable logic in a standard `.Rule()` will trigger the generation of a dynamic controller, resulting in an HTTP round-trip on the client.

### 2. Never Hard-Code Dynamic Values (Like Dates)
If you hard-code `DateTime.Today` in your rule, it will be translated to a static JavaScript date representing the exact moment the rule was translated (typically server startup).

**❌ Bad:**
```csharp
.Rule(m => m.BirthDate, m => m.BirthDate > DateTime.Today ? "Invalid" : null)
```

**✅ Good:**
Use `.Parameter()` to ensure the value is evaluated at runtime:
```csharp
.Parameter("Today", () => DateTime.Today)
.Rule<DryvParameters>(m => m.BirthDate, (m, p) => m.BirthDate > p.Get<DateTime>("Today") ? "Invalid" : null)
```

### 3. Use Constants for Parameter Names
To avoid typos when retrieving parameters in your rules, use `nameof()` or string constants:
```csharp
private const string MaxAgeParam = nameof(MaxAgeParam);

.Parameter(MaxAgeParam, () => 18)
.Rule<DryvParameters>(m => m.Age, (m, p) => m.Age > p.Get<int>(MaxAgeParam) ? "Too old" : null)
```

### 4. Create Reusable Regex Patterns
Instead of instantiating `new Regex(...)` inline, use a static property or a custom injected service to keep your rules clean. Dryv natively supports translating `Regex.IsMatch` to JavaScript `/pattern/.test(string)`.

```csharp
public static class Patterns 
{
    public const string ZipCode = @"^\d{5}$";
}

// In your rule:
.Rule(m => m.Zip, m => Regex.IsMatch(m.Zip, Patterns.ZipCode) ? null : "Invalid ZIP")
```

### 5. Managing Large Rule Sets
If your model has dozens of properties, the static `Rules` definition can get massive. You can split rules using the `[DryvValidation]` attribute to reference an external rule container class:

```csharp
public class OrderModel
{
    [DryvValidation(typeof(OrderValidationRules))]
    public string OrderNumber { get; set; }
}

public class OrderValidationRules 
{
    public static DryvRules Rules = DryvRules.For<OrderModel>()
        .Rule(m => m.OrderNumber, m => string.IsNullOrWhiteSpace(m.OrderNumber) ? "Error" : null);
}
```

### 6. Combine Multiple Checks with Logical Operators
Chain conditions with `&&` and `||` instead of nesting ternaries:

```csharp
.Rule(m => m.Email,
    m => !string.IsNullOrWhiteSpace(m.Email)
      && new Regex(@"^[^@]+@[^@]+\.[^@]+$").IsMatch(m.Email)
        ? null
        : "Enter a valid email address")
```

### 7. Use `DryvValidationResult.Warning` for Soft Validation
Not every issue should block form submission. Use warnings for advisory messages:

```csharp
.Rule(m => m.Phone,
    m => string.IsNullOrWhiteSpace(m.Phone)
        ? DryvValidationResult.Warning("A phone number is recommended but not required")
        : DryvValidationResult.Success)
```

On the client, check `result.type === "warning"` to display the message without preventing submission.

### 8. Share Rules Across Models via Interfaces
Define rules on interfaces to avoid duplicating validation logic:

```csharp
public interface IHasName
{
    string FirstName { get; set; }
    string LastName { get; set; }
}

public static class NameRules
{
    public static readonly DryvRules Rules = DryvRules
        .For<IHasName>()
        .Rule(m => m.FirstName, m => string.IsNullOrWhiteSpace(m.FirstName) ? "Required" : null)
        .Rule(m => m.LastName, m => string.IsNullOrWhiteSpace(m.LastName) ? "Required" : null);
}

public class Customer : IHasName
{
    public static DryvRules Rules = NameRules.Rules;
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

public class Employee : IHasName
{
    public static DryvRules Rules = NameRules.Rules;
    public string FirstName { get; set; }
    public string LastName { get; set; }
}
```

### 9. Test Both Validation and Translation
Don't just test that rules validate correctly — also verify that they translate without errors:

```csharp
[Fact]
public void Rules_ShouldTranslateToJavaScript()
{
    var translator = new DryvTranslator(/* ... */);
    var result = await translator.TranslateValidationRules(
        typeof(MyModel), type => null);

    // Ensure no rules failed translation
    Assert.All(result.ValidationRules,
        r => Assert.NotNull(r.TranslatedValidationExpression));
}
```

### 10. Use `TranslationErrorBehavior.Throw` During Development
Set `TranslationErrorBehavior` to `Throw` in development so you catch untranslatable rules immediately instead of silently falling back to server-side validation:

```csharp
builder.Services.AddDryv(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.TranslationErrorBehavior = TranslationErrorBehavior.Throw;
    }
});
```

### 11. Null-Safe Property Access
Always guard against `null` when accessing properties on potentially null objects:

```csharp
// ✅ Safe:
.Rule(m => m.Name,
    m => m.Name != null && m.Name.Length > 100 ? "Too long" : null)

// ❌ Will throw NullReferenceException at runtime if Name is null:
.Rule(m => m.Name,
    m => m.Name.Length > 100 ? "Too long" : null)
```

### 12. Preloading for Production
Always enable preloading in production to avoid cold-start latency on the first validation request:

```csharp
builder.Services
    .AddControllersWithViews()
    .AddDryv()
    .AddDryvPreloading();
```

## Troubleshooting & FAQ

### "Rule failed to translate" / TranslationException

**Symptoms:** A rule works on the server but produces a translation error when generating client-side JavaScript.

**Common causes:**
1. **Unsupported C# construct** — e.g. using `switch`, `if/else`, `foreach`, or LINQ methods that don't have a translator.
2. **Calling a method without a translator** — Dryv only knows how to translate methods registered via `IDryvMethodCallTranslator`. If you call `myService.Validate(...)` without a translator, it fails.
3. **Using `var` or local variables** — Dryv translates expression trees, which don't support statement-level constructs.

**Solutions:**
- Move un-translatable logic into a `.ServerRule(...)`.
- Write a custom translator for your method/type.
- Simplify the expression to use only supported constructs (ternary, method calls, property access, operators).
- Set `TranslationErrorBehavior = TranslationErrorBehavior.Ignore` to silently skip untranslatable rules (they'll run server-side only via dynamic controllers).

### Client-side validation not running

**Symptoms:** The `<script>` tag is rendered, but validation doesn't fire.

**Checklist:**
1. Ensure your client-side code actually calls the `validate` function on the rule objects.
2. Verify the model property names are **camelCase** — Dryv always uses camelCase in generated JavaScript regardless of your C# naming convention.
3. If using async rules, ensure you've provided `$ctx.dryv.callServer` and `$ctx.dryv.handleResult` implementations.
4. Check the browser console for JavaScript errors in the generated validation code.

### Dynamic controller returns 404

**Symptoms:** Async rules fail on the client with a 404 error.

**Causes:**
1. **Stale client code** — Dynamic controller URLs contain an MD5 hash of the expression. If you changed the C# rule but didn't regenerate the client code, the old URL no longer exists.
2. **Missing `app.UseDryv()`** — The dynamic controllers are initialized during `UseDryv()`.
3. **Missing `AddDryvDynamicControllers()`** — The dynamic controller services aren't registered.

**Fix:** Regenerate client code, ensure both `AddDryvDynamicControllers()` and `app.UseDryv()` are called.

### Parameters are stale / frozen

**Symptoms:** A parameter like "today's date" shows the same value days later.

**Cause:** The parameter was baked into the generated JavaScript at translation time instead of being fetched at runtime.

**Fix:** Use `.Parameter(...)` on the `DryvRules` and serve parameters through a separate runtime endpoint. On the client, always fetch fresh parameter values before validation. See [Rule Parameters](#rule-parameters) and [Using Parameters on the Client](#using-parameters-on-the-client).

### Rules not discovered for nested models

**Symptoms:** Rules defined on a child model type are not included in the validation output.

**Causes:**
1. The child model doesn't have a navigation property on the parent.
2. The `DryvRules` field/property is not `public static`.
3. The child model's rules target a different type than what the parent references.

**Fix:** Ensure a property of the child type exists on the parent model, and that the `DryvRules` is a `public static` field or property (or a `public static` method returning `DryvRules`).

### Enums compare incorrectly on the client

**Symptoms:** An enum comparison rule passes on the server but fails on the client (or vice versa).

**Cause:** The enum serialization strategy differs between server and client. The server might use integer values while the client uses string names.

**Fix:** Ensure your `DryvOptions.JsonConversion` matches your API's JSON serialization settings. If your API uses `JsonStringEnumConverter`, Dryv should too.

### FAQ

#### Can I use Dryv without ASP.NET Core?

Yes. The core `Dryv` NuGet package is framework-agnostic and targets .NET 9. You can define rules, validate on the server, and translate to JavaScript without any web framework. See [Standalone / Custom Integration](#standalone--custom-integration).

#### What happens when a rule can't be translated to JavaScript?

It depends on your `TranslationErrorBehavior` setting:
- **`Throw`** (recommended for development) — throws an exception immediately, so you catch the problem early.
- **`Ignore`** — the rule is silently excluded from client-side output. If dynamic controllers are enabled, the rule will be routed through a server endpoint instead (the client makes an HTTP call to validate).

#### Can I use `if/else` in rules?

No. Dryv works with C# **expression trees** (`Expression<Func<...>>`), which do not support statements like `if/else`, `switch`, `for`, or `while`. Use the **ternary operator** (`? :`) and logical operators (`&&`, `||`) instead.

#### How are property names cased in JavaScript?

Always **camelCase**. C# `ZipCode` becomes `zipCode`, `FirstName` becomes `firstName`. This applies to all property paths in the generated output (validators, disablers, related properties, model paths).

### Can I validate collections / arrays of items?

Dryv discovers rules on collection element types (see [Collection Elements](#collection-elements)). The rules are available in the validation set and the LINQ methods on collections are translated (see [LINQ / Collections Reference](#linq--collections-reference)). However, the exact client-side behavior for validating individual collection items depends on your client-side validation runner implementation.

#### How do I handle multiple validation sets on one page?

Use the `[DryvSet("name")]` attribute on your model classes to assign set names, or pass a dictionary of types to the Tag Helper:

```html
<dryv-client-rules for="@(new Dictionary<string, Type> {
    { "billing", typeof(Address) },
    { "shipping", typeof(Address) }
})" />
```

Each set gets its own `name`, `validators`, `disablers`, and `parameters`.

#### Is there a ready-made client-side library?

Yes! The official companion packages [`dryvjs`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvjs) and [`dryvue`](https://github.com/mhusseini/dryvjs/tree/develop/packages/dryvue) provide a full-featured reactive validation engine that directly consumes the `DryvValidationRuleSet` objects generated by Dryv. `dryvjs` is framework-agnostic (works with vanilla JS/TS, React, Angular, etc.), while `dryvue` adds Vue 3 reactive bindings. See [Integration with Frontend Frameworks](#integration-with-frontend-frameworks) for comprehensive usage examples.

#### Can I use Dryv with Blazor?

Dryv is designed for C#-to-JavaScript translation. In Blazor (where your client-side code is also C#), you can use the server-side `DryvValidator` directly without translation. The JavaScript translation features are not applicable in Blazor's execution model.

#### How do I debug the generated JavaScript?

1. Render the validation script to a string (via Tag Helper, HTML Helper, or the programmatic API).
2. Pretty-print it in the browser dev tools or save it to a file.
3. Set breakpoints in the generated `validate` functions.
4. For build-time generation, inspect the generated `.ts` or `.js` files directly.

## Configuration

`DryvOptions` provides the following settings:

| Option | Description |
|--------|-------------|
| `Translators` | Collection of custom translator types to register. |
| `Annotators` | Collection of rule annotator types (for adding metadata to rules). |
| `DisableAutomaticValidation` | Set to `true` to prevent the automatic MVC validation filter from being added. |
| `TranslationErrorBehavior` | Controls behavior when a rule cannot be translated (`Throw` or `Ignore`). |
| `JsonConversion` | Custom `Func<object, string>` for JSON serialization of values in generated JS. |
| `IncludeModelDataInExceptions` | Include model data in validation exception messages (for debugging). |
| `DisableParameterInjection` | Disable automatic parameter injection into client-side validation. |
| `ClientFunctionWriterType` | Override the writer for client validation function output. |
| `ClientValidationSetWriterType` | Override the writer for client validation set output. |

```csharp
builder.Services
    .AddControllersWithViews()
    .AddDryv(options =>
    {
        options.DisableAutomaticValidation = false;
        options.TranslationErrorBehavior = TranslationErrorBehavior.Throw;
        options.Translators.Add<MyCustomTranslator>();
    });
```

## Project Structure

```
Dryv/
├── Dryv/                          # Core library (NuGet: Dryv)
│   ├── Configuration/             # DryvOptions, service collection
│   ├── Extensions/                # Extension methods
│   ├── Reflection/                # Reflection utilities
│   ├── RuleDetection/             # Rule discovery and model tree building
│   ├── Rules/                     # DryvCompiledRule, DryvParameters, rule types
│   ├── Translation/               # C#-to-JS translator engine
│   │   ├── Translators/           # Built-in translators (String, Regex, Enumerable, etc.)
│   │   └── Visitors/              # Expression tree visitors
│   └── Validation/                # Validation result writers
│
├── Dryv.AspNetCore/               # ASP.NET Core integration (NuGet: Dryv.AspNetCore)
│   ├── Configuration/             # MVC builder interfaces
│   ├── DynamicControllers/        # Runtime controller generation for async rules
│   ├── Filters/                   # MVC filters
│   ├── Json/                      # JSON serialization support
│   ├── Middlewares/               # ASP.NET middleware
│   ├── PreLoading/                # Startup preloading
│   ├── Razor/                     # Razor HTML content helpers
│   ├── TagHelpers/                # <dryv-client-rules> tag helper
│   └── Translators/               # ASP.NET-specific translators
│
├── Dryv.Tests/                    # Unit tests for the core library
└── Dryv.AspNetCore.Tests/         # Tests for the ASP.NET Core integration
```

## License

[MIT](LICENSE) — Copyright 2020 Munir Husseini
# Dryv.AspNetCore 10.0.0 — Release Notes

## Highlights

Dryv.AspNetCore 10.0 aligns with the Dryv core 10.0 overhaul, adds a new lightweight endpoint-routing model for dynamic server-side validation endpoints, expands target frameworks to **net8.0**, **net9.0**, and **net10.0**, and modernises the project to use `Microsoft.AspNetCore.App` framework reference instead of individual NuGet packages.

---

## New Features

- **Dynamic endpoint routing** — new `MapDryvEndpoints()` extension on `IEndpointRouteBuilder` registers dynamically generated server-side validation endpoints using ASP.NET Core's endpoint routing infrastructure, replacing the older MVC dynamic controller approach.
- **`AddDryvDynamicEndpoints()`** — opt-in builder method on `IDryvMvcBuilder` to register the `DryvDynamicEndpointDataSource`, `DryvDynamicEndpointRegistration`, and related services.
- **`DryvServiceProviderExtensions`** — new helper for resolving Dryv services directly from `IServiceProvider`.
- **`ServiceProviderResolver`** — internal `IDryvServiceResolver` implementation backed by `IServiceProvider`, bridging ASP.NET Core DI to the new core abstraction.
- **Preloader enabled by default** — the Dryv rule preloader now runs on application startup by default for faster first-request validation.

## Architecture & Refactoring

- **Project SDK changed** from `Microsoft.NET.Sdk.Web` to `Microsoft.NET.Sdk` — the package is now a pure library, no longer pulling in web host dependencies unnecessarily.
- **Framework reference** — replaced `System.Reflection.Emit` NuGet package with `FrameworkReference Include="Microsoft.AspNetCore.App"`.
- **Primary constructors** adopted across service classes.
- Accessibility cleanup — internal types and members tightened.
- Redundant `Dryv` namespace references removed; unused dependencies cleaned up.

## Breaking Changes

- **Target framework changed** — from single `net9.0` to multi-target `net8.0;net9.0;net10.0`.
- **Version bump** — `9.4.2` → `10.0.0`.
- **Dynamic controller registration** — the new endpoint-based model (`MapDryvEndpoints`) replaces the previous MVC dynamic controller approach. Existing `AddDryvDynamicControllers()` usage should migrate to `AddDryvDynamicEndpoints()` + `MapDryvEndpoints()`.
- **`IDryvServiceResolver`** — core interfaces now use `IDryvServiceResolver` instead of `Func<Type, object>`; the ASP.NET Core layer provides the bridge automatically via DI.
- **Preloader default** — the preloader is now enabled by default. If you relied on lazy rule discovery, configure `DryvOptions` accordingly.

## Dependencies

- `LangVersion` upgraded from `8` → `latest`.
- Removed direct dependency on `System.Reflection.Emit 4.7.0`.

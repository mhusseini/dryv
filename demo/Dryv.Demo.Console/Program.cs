using Dryv;
using Dryv.Configuration;
using Dryv.Demo.Console.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

// ──────────────────────────────────────────────────────────────────
// 1. Set up DI container
// ──────────────────────────────────────────────────────────────────
var services = new ServiceCollection();

// ──────────────────────────────────────────────────────────────────
// 2. Configure Dryv core (no ASP.NET dependency required)
// ──────────────────────────────────────────────────────────────────
var options = new DryvOptions();

DryvCoreSetup.ConfigureDefaults(options);

DryvCoreSetup.RegisterServices(
    options,
    registerSingleton: (svc, impl) => services.TryAddSingleton(svc, impl),
    registerScoped: (svc, impl) => services.TryAddSingleton(svc, impl),
    registerInstance: (svc, inst) => services.AddSingleton(svc, inst),
    registerFactory: (svc, factory) => services.AddSingleton(svc, sp => factory(sp.GetService)));

// In a console app there are no scopes, so register the resolver as a singleton
services.TryAddSingleton<IDryvServiceResolver>(sp => DryvServiceResolverFactory.FromFunc(sp.GetService!));

var provider = services.BuildServiceProvider();

// ──────────────────────────────────────────────────────────────────
// 3. Resolve the validator
// ──────────────────────────────────────────────────────────────────
var validator = provider.GetRequiredService<DryvValidator>();

// ──────────────────────────────────────────────────────────────────
// 4. Validate an invalid model
// ──────────────────────────────────────────────────────────────────
Console.WriteLine("=== Validating invalid model ===");

var invalid = new RegistrationModel
{
    FirstName = "",
    LastName = "",
    Email = "not-an-email",
    Password = "short"
};

var errors = await validator.Validate(invalid);

foreach (var (path, result) in errors)
{
    Console.WriteLine($"  {path}: {result.Text}");
}

// ──────────────────────────────────────────────────────────────────
// 5. Validate a valid model
// ──────────────────────────────────────────────────────────────────
Console.WriteLine();
Console.WriteLine("=== Validating valid model ===");

var valid = new RegistrationModel
{
    FirstName = "Jane",
    LastName = "Doe",
    Email = "jane@example.com",
    Password = "securepassword"
};

errors = await validator.Validate(valid);

if (errors.Count == 0)
{
    Console.WriteLine("  No validation errors — model is valid!");
}

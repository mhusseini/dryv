using System.Text;
using Dryv;
using Dryv.AspNetCore;
using Dryv.Demo.Api.JsonSchema.Models;
using Dryv.JsonSchema;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddControllers()
    .AddDryv(options => options.UseJsonSchemaTranslator());

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();

// ──────────────────────────────────────────────────────────────────
// GET /api/json-schema
//
// Uses DryvClientWriter (the same abstraction the JS demos use)
// to translate validation rules and produce JSON Schema output.
// ──────────────────────────────────────────────────────────────────
app.MapGet("/api/json-schema", async (
    DryvClientWriter clientWriter,
    IHttpContextAccessor httpContextAccessor) =>
{
    var write = await clientWriter.WriteDryvValidation<RegistrationModel>(
        "RegistrationModel",
        httpContextAccessor.HttpContext!.RequestServices.AsDryvServiceResolver(),
        new Dictionary<string, object>());

    var sb = new StringBuilder();
    await using (var sw = new StringWriter(sb))
    {
        write(sw);
    }

    return Results.Content(sb.ToString(), "application/json");
});

// ──────────────────────────────────────────────────────────────────
// POST /api/register
//
// Server-side validation endpoint (works identically regardless of
// the configured translator — the rules are the same C# code).
// ──────────────────────────────────────────────────────────────────
app.MapPost("/api/register", async ([FromBody] RegistrationModel model, HttpContext ctx) =>
{
    var validator = ctx.RequestServices.GetRequiredService<DryvValidator>();
    var results = await validator.Validate(model, ctx.RequestServices.AsDryvServiceResolver());

    if (results.Any())
    {
        return Results.BadRequest(new
        {
            errors = results.Select(r => new { path = r.Key, text = r.Value.Text, type = r.Value.Type.ToString().ToLowerInvariant() })
        });
    }

    return Results.Ok(new { message = $"Welcome, {model.FirstName} {model.LastName}!" });
});

app.Run();

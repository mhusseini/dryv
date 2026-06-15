using System.Text;
using Dryv;
using Dryv.AspNetCore;
using Dryv.Demo.Api.DryvJS.Models;
using Dryv.Demo.Api.DryvJS.Services;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EmailValidator>();

builder.Services
    .AddControllers()
    .AddDryv()
    .AddDryvDynamicControllers()
    .AddDryvPreloading();

builder.Services.AddHttpContextAccessor();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRouting();
app.UseDryv();
app.MapControllers();

app.MapGet("/api/validation-rules", async (
    DryvClientWriter clientWriter,
    IHttpContextAccessor httpContextAccessor) =>
{
    var types = new Dictionary<string, Type>
    {
        { "registration", typeof(RegistrationModel) }
    };

    var write = await clientWriter.WriteDryvValidation(
        types,
        httpContextAccessor.HttpContext!.RequestServices.GetService,
        new Dictionary<string, object>());

    var sb = new StringBuilder();
    await using (var sw = new StringWriter(sb))
    {
        write(sw);
    }

    return Results.Content(sb.ToString(), "application/javascript");
});

app.MapPost("/api/register", async ([FromBody] RegistrationModel model, HttpContext ctx) =>
{
    var validator = ctx.RequestServices.GetRequiredService<DryvValidator>();
    var results = await validator.Validate(model, ctx.RequestServices.GetService);

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

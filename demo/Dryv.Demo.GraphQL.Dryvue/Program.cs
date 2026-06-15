using System.Text;
using Dryv;
using Dryv.AspNetCore;
using Dryv.Demo.GraphQL.Dryvue;
using Dryv.Demo.GraphQL.Dryvue.Models;
using Dryv.Demo.GraphQL.Dryvue.Services;
using Dryv.HotChocolate;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EmailValidator>();

builder.Services
    .AddControllers()
    .AddDryv()
    .AddDryvPreloading();

builder.Services.AddHttpContextAccessor();

builder.Services
    .AddGraphQLServer()
    .AddQueryType<Query>()
    .AddMutationType<Mutation>()
    .AddDryvValidation();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:5174")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

app.UseCors();
app.UseRouting();

app.MapGraphQL();

app.MapGet("/api/validation-rules", async (
    DryvClientWriter clientWriter,
    IHttpContextAccessor httpContextAccessor) =>
{
    var types = new Dictionary<string, Type>
    {
        { "registration", typeof(RegistrationInput) }
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

app.Run();

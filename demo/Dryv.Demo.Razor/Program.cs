using Dryv.AspNetCore;
using Dryv.Demo.Razor.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<EmailValidator>();

builder.Services
    .AddControllersWithViews()
    .AddDryv()
    .AddDryvDynamicControllers()
    .AddDryvPreloading();

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseDryv();
app.MapDefaultControllerRoute();

app.Run();

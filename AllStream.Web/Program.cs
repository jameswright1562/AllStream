using AllStream.Shared.Models;
using AllStream.Shared.Services;
using AllStream.Web.Components;
using AllStream.Web.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
var settings = builder.Configuration.Get<Settings>() ?? new Settings();
builder.Services.AddSingleton(settings);

// Add services to the container.
builder.Services.AddRazorComponents().AddInteractiveServerComponents();

// Add device-specific services used by the AllStream.Shared project
ServiceBuilder.AddSharedServices(builder.Services, sp => new FormFactor(), settings);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

app.MapStaticAssets();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddAdditionalAssemblies(typeof(AllStream.Shared._Imports).Assembly);

var manifest = builder.Configuration.GetSection("Manifest").Get<UpdateManifest>() ?? new UpdateManifest();
app.MapGet("/manifest.json", () => Results.Json(manifest));

app.Run();

public partial class Program { }

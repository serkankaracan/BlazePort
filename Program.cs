using BlazePort.Components;
using BlazePort.Runtime;
using BlazePort.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Parse the command-line arguments to determine the application mode and any warnings
var appArgs = ArgsParser.Parse(args);

// Log any warnings from argument parsing
if (appArgs.Warning != null)
    Console.WriteLine($"[BlazePort Startup] {appArgs.Warning}");

// Add di for AppArgs so it can be injected into components
builder.Services.AddSingleton(appArgs);

builder.Services.AddSingleton<PortScanner>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

using BlazePort.Components;
using BlazePort.Data;
using BlazePort.Runtime;
using BlazePort.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var appArgs = ArgsParser.Parse(args);

if (appArgs.Warning is not null)
    Console.WriteLine($"[BlazePort] {appArgs.Warning}");

builder.Services.AddSingleton(appArgs);
builder.Services.AddSingleton<IPortProvider, DefaultPortProvider>();
builder.Services.AddSingleton<PortScanner>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

await app.RunAsync();

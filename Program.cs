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

try
{
    await app.RunAsync();
}
catch (IOException ex) when (ex.InnerException is Microsoft.AspNetCore.Connections.AddressInUseException)
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[BlazePort] Port is already in use. The application may already be running.");
    Console.ResetColor();
    Environment.Exit(1);
}

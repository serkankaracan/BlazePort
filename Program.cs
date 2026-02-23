using System.Security.Cryptography;
using System.Text;
using BlazePort.Components;
using BlazePort.Data;
using BlazePort.Runtime;
using BlazePort.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var appArgs = ArgsParser.Parse(args);

if (appArgs.Warning is not null)
    Console.WriteLine($"[BlazePort] {appArgs.Warning}");

if (appArgs.Mode == AppMode.Admin)
{
    // SHA256("admin") — to change, update this hash.
    const string adminPasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918";

    Console.Write("[BlazePort] Enter admin password: ");
    var input = ReadMaskedInput();
    Console.WriteLine();

    if (HashSha256(input) != adminPasswordHash)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("[BlazePort] Invalid password. Access denied.");
        Console.ResetColor();
        Environment.Exit(1);
    }

    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("[BlazePort] Access granted.");
    Console.ResetColor();
}

var dbPath = Path.Combine(AppContext.BaseDirectory, "blazeport.db");

builder.Services.AddSingleton(appArgs);
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton<IPortRepository, PortRepository>();
builder.Services.AddSingleton<IPortProvider, SqlitePortProvider>();
builder.Services.AddSingleton<PortScanner>();

var app = builder.Build();

// Create database schema and seed default data via EF Core
app.Services.GetRequiredService<IPortRepository>().EnsureSchema();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

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

static string ReadMaskedInput()
{
    var password = new StringBuilder();
    while (true)
    {
        var key = Console.ReadKey(intercept: true);
        if (key.Key == ConsoleKey.Enter)
            break;
        if (key.Key == ConsoleKey.Backspace)
        {
            if (password.Length > 0)
            {
                password.Length--;
                Console.Write("\b \b");
            }
        }
        else if (!char.IsControl(key.KeyChar))
        {
            password.Append(key.KeyChar);
            Console.Write('*');
        }
    }
    return password.ToString();
}

static string HashSha256(string input)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes).ToLowerInvariant();
}

using System.Security.Cryptography;
using System.Text;
using BlazePort.Components;
using BlazePort.Data;
using BlazePort.Runtime;
using BlazePort.Services;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var appArgs = ArgsParser.Parse(args);

if (!string.IsNullOrWhiteSpace(appArgs.Warning))
    Console.WriteLine($"[BlazePort] {appArgs.Warning}");

if (appArgs.Mode == AppMode.Admin)
{
    // SHA256("admin") — to change, update this hash.
    // powershell -NoProfile -Command "[BitConverter]::ToString([System.Security.Cryptography.SHA256]::Create().ComputeHash([System.Text.Encoding]::UTF8.GetBytes('admin'))).Replace('-','').ToLower()"
    const string adminPasswordHash = "8c6976e5b5410415bde908bd4dee15dfb167a9c873fc4bb8a81f6f2ab448a918";

    const int maxAttempts = 3; // Maximum number of attempts
    var attempts = 0;

    while (attempts < maxAttempts)
    {
        Console.Write("[BlazePort] Enter admin password: ");
        var input = ReadMaskedInput(); // Read masked password from console
        Console.WriteLine();

        if (HashSha256(input) == adminPasswordHash)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("[BlazePort] Access granted.");
            Console.ResetColor();
            break;
        }

        attempts++;

        Console.ForegroundColor = ConsoleColor.Red;
        if (attempts >= maxAttempts)
        {
            Console.WriteLine("[BlazePort] Invalid password. Access denied.");
            Console.ResetColor();
            Environment.Exit(1);
        }
        else
        {
            // wrong password but still have attempts left
            var remaining = maxAttempts - attempts;
            Console.WriteLine($"[BlazePort] Invalid password. Attempts left: {remaining}.");
            Console.ResetColor();
        }
    }
}

var dbPath = Path.Combine(AppContext.BaseDirectory, "blazeport.db"); // Path to the database file

// QuestPDF license compliance for your organization
// The library is free for individuals, non-profits, all FOSS projects, and organizations under $1M in annual revenue.
QuestPDF.Settings.License = LicenseType.Community;

builder.Services.AddSingleton(appArgs);
builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));
builder.Services.AddSingleton<PortRepository>();
builder.Services.AddSingleton<SqlitePortProvider>();
builder.Services.AddSingleton<PdfExportService>();
builder.Services.AddSingleton<PortScanner>();

var app = builder.Build();

// Create database schema and seed default data via EF Core
app.Services.GetRequiredService<PortRepository>().EnsureSchema();

if (!app.Environment.IsDevelopment()) // If not in development environment
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true); // Handle exceptions
    app.UseHsts(); // Strict transport security
}

app.UseStaticFiles(); // Serve static files from the wwwroot folder
app.UseAntiforgery(); // Prevent CSRF attacks

app.MapRazorComponents<App>() // Map Razor components to the app
    .AddInteractiveServerRenderMode(); // Add interactive server render mode

try
{
    await app.RunAsync(); // Run kestrel web server.
}
catch (IOException ex) when (ex.InnerException is Microsoft.AspNetCore.Connections.AddressInUseException) // Catch address in use exception
{
    Console.ForegroundColor = ConsoleColor.Red;
    Console.WriteLine("[BlazePort] Port is already in use. The application may already be running.");
    Console.ResetColor();
    Environment.Exit(1);
}

static string ReadMaskedInput() // Read masked input from console
{
    var password = new StringBuilder(); // StringBuilder to store the password
    while (true)
    {
        var key = Console.ReadKey(intercept: true); // Read key from console. Intercept: true means the key will not be echoed to the console.
        if (key.Key == ConsoleKey.Enter) // If the key is enter, break the loop
            break;
        if (key.Key == ConsoleKey.Backspace) // If the key is backspace, remove the last character from the password
        {
            if (password.Length > 0)
            {
                password.Length--;
                Console.Write("\b \b"); // Erase the last character
            }
        }
        else if (!char.IsControl(key.KeyChar)) // If the key is not a control character, add it to the password
        {
            password.Append(key.KeyChar);
            Console.Write('*'); // Print a * for each character in the password
        }
    }
    return password.ToString();
}

static string HashSha256(string input)
{
    var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
    return Convert.ToHexString(bytes).ToLowerInvariant(); // Convert the bytes to a hexadecimal string and convert it to lowercase
}

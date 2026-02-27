# BlazePort

A Blazor Server application for scanning network ports and checking service availability. Port definitions are stored in a local SQLite database managed by Entity Framework Core.

## Features

- **TCP port scanning** with per-port connect timeout
- **ICMP ping** check for each target host
- **Three-attempt scan logic** per port (ping + connect tried 3 times and aggregated)
- **SQLite storage** via EF Core — port definitions persist across restarts
- **Admin mode** with SHA256 password protection (masked console input, 3 attempts)
- **Port management UI** — add, edit, and delete port entries per mode
- **Mode-based port sets** — Client, Server, and Admin modes load different ports
- **Admin aggregated view** — shows all ports with mode badges; duplicates merged
- **Custom port addition** — add temporary ports to the scan list at runtime
- **Real-time progress** — results update live as each port is checked
- **PDF export** of scan results via server-side QuestPDF
- **Single-file publish** — ships as a self-contained `.exe`

## Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Run

```bash
dotnet run
```

### Run with a specific mode

```bash
dotnet run -- --mode Server
dotnet run -- --mode Admin
```

Available modes: `Client` (default), `Server`, `Admin`.

Admin mode requires a console password before the application starts. The password is validated with up to **3 attempts**; after 3 failures the app exits with “Access denied”.

### Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin/Release/net8.0/win-x64/publish/BlazePort.exe`

## Project Structure

```text
BlazePort/
├── Components/
│   ├── Layout/                 # MainLayout, NavMenu
│   └── Pages/
│       ├── Home.razor          # Port scan UI (markup)
│       ├── Home.razor.cs       # Home code-behind (scan logic, PDF export)
│       ├── Admin.razor         # Port CRUD management
│       └── Error.razor
├── Data/
│   ├── AppDbContext.cs         # EF Core DbContext (Ports table, composite unique)
│   ├── PortEntity.cs           # Entity: Id, Mode, Port, Name
│   ├── PortRepository.cs       # EF Core repository for Ports
│   └── SqlitePortProvider.cs   # Loads ports from SQLite for scanning
├── Models/
│   ├── ServiceEndpoint.cs      # Port + Name + Modes
│   ├── PortResult.cs           # Scan result (Open / Closed / Timeout / Error)
│   ├── PingResult.cs           # Ping result (Success / Fail)
│   └── PortStatus.cs           # Status enum
├── Runtime/
│   ├── AppArgs.cs              # Parsed CLI arguments
│   ├── AppMode.cs              # Mode enum (Client / Server / Admin)
│   └── ArgsParser.cs           # CLI argument parser (--mode)
├── Services/
│   ├── PortScanner.cs          # TCP connect + ICMP ping (single attempt)
│   └── PdfExportService.cs     # Server-side PDF generation (QuestPDF)
├── wwwroot/
│   └── js/
│       └── export.js           # Helper for downloading PDF from base64
└── Program.cs                  # Entry point, DI, admin password gate, QuestPDF license
```

## Architecture

### Database

SQLite database (`blazeport.db`) is created automatically on first run via `EnsureCreated()`. The `Ports` table has a composite unique constraint on `(Mode, Port)` to prevent duplicate entries within the same mode. Default ports are seeded on first run via `PortRepository.SeedDefaults`.

### Scan Flow

```text
User clicks Run
  │
  ├── For each port in the list:
  │     ├── 3 × PingAsync(host, timeoutMs: 1000)
  │     │       → aggregated PingResult:
  │     │           - success if any attempt succeeded
  │     │           - RoundtripTime = minimum successful ping
  │     │           - Error message aggregated across failures
  │     └── 3 × CheckAsync(host, port, timeoutMs: 1000)
  │             → aggregated PortResult:
  │                 - Open if any attempt is Open (min connect time)
  │                 - Timeout if any attempt timed out (with count)
  │                 - otherwise Closed/Error with grouped error messages
  │
  └── UI updates after each port (real-time)
```

Aggregated error messages in the **Details** column are grouped, e.g.:

- `Connection refused (3)`
- `Timeout (2); Connection refused (1)`

### PDF Export

When all ports have been scanned, the **Export PDF** button becomes active on the `Home` page. Clicking it:

1. Uses `PdfExportService` (QuestPDF) on the server to build a PDF summary of the scan results.
2. Encodes the PDF as base64 and calls a small JS helper (`blazePortExport.downloadPdf`) to:
   - Trigger a download (e.g. `blazeport_localhost.pdf`).
   - Let the browser handle opening the file (according to user settings).

The export button is disabled while scanning or exporting and shows `Exporting...` during PDF generation.

### Admin Password

Admin mode is protected by a SHA256 hash embedded in `Program.cs`. The password is entered via masked console input (`*` characters). You have **3 attempts** to enter the correct password:

- On each failure: `[BlazePort] Invalid password. Attempts left: N.`
- After 3 failures: `[BlazePort] Invalid password. Access denied.` and the app exits.

To change the password, update the hash constant in `Program.cs` and adjust the comment accordingly.

## License

MIT

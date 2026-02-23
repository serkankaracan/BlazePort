# BlazePort

A Blazor Server application for scanning network ports and checking service availability. Port definitions are stored in a local SQLite database managed by Entity Framework Core.

## Features

- **TCP port scanning** with configurable timeout
- **ICMP ping** check for each target host
- **SQLite storage** via EF Core — port definitions persist across restarts
- **Admin mode** with SHA256 password protection (masked console input)
- **Port management UI** — add, edit, and delete port entries per mode
- **Mode-based port sets** — Client, Server, and Admin modes load different ports
- **Admin aggregated view** — shows all ports with mode badges; duplicates merged
- **Custom port addition** — add temporary ports to the scan list at runtime
- **Real-time progress** — results update live as each port is checked
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

Admin mode requires a console password before the application starts.

### Publish

```bash
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

Output: `bin/Release/net8.0/win-x64/publish/BlazePort.exe`

## Project Structure

```
BlazePort/
├── Components/
│   ├── Layout/              # MainLayout, NavMenu
│   └── Pages/
│       ├── Home.razor       # Port scan UI
│       ├── Admin.razor      # Port CRUD management
│       └── Error.razor
├── Data/
│   ├── AppDbContext.cs       # EF Core DbContext (Ports table, composite unique)
│   ├── PortEntity.cs         # Entity: Id, Mode, Port, Name
│   ├── IPortRepository.cs    # Repository interface
│   ├── PortRepository.cs     # EF Core repository implementation
│   ├── IPortProvider.cs      # Port provider interface
│   └── SqlitePortProvider.cs # Loads ports from SQLite for scanning
├── Models/
│   ├── ServiceEndpoint.cs    # Port + Name + Modes
│   ├── PortResult.cs         # Scan result (Open / Closed / Timeout)
│   ├── PingResult.cs         # Ping result (Success / Fail)
│   └── PortStatus.cs         # Status enum
├── Runtime/
│   ├── AppArgs.cs            # Parsed CLI arguments
│   ├── AppMode.cs            # Mode enum (Client / Server / Admin)
│   └── ArgsParser.cs         # CLI argument parser
├── Services/
│   └── PortScanner.cs        # TCP connect + ICMP ping
└── Program.cs                # Entry point, DI, admin password gate
```

## Architecture

### Database

SQLite database (`blazeport.db`) is created automatically on first run via `EnsureCreated()`. The `Ports` table has a composite unique constraint on `(Mode, Port)` to prevent duplicate entries within the same mode. Default ports are seeded on first run.

### Scan Flow

```
User clicks Run
  │
  ├── For each port in the list:
  │     ├── PingAsync(host)        → PingResult (OK / Fail)
  │     └── CheckAsync(host, port) → PortResult (Open / Closed / Timeout)
  │           └── TcpClient.ConnectAsync (with timeout)
  │
  └── UI updates after each port (real-time)
```

### Admin Password

Admin mode is protected by a SHA256 hash embedded in the binary. The password is entered via masked console input (`*` characters). To change the password, update the hash constant in `Program.cs`.

## License

MIT

# BlazePort

A Blazor Server application for scanning network ports and checking service availability.

## Features

- **TCP port scanning** with configurable timeout
- **ICMP ping** check for each target
- **Banner reading** (Telnet-like) to identify running services
- **Mode-based port sets** — Client, Server, and Admin modes load different default ports
- **Custom port addition** — add any port/protocol to the scan list at runtime
- **Real-time progress** — results update live as each port is checked

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
```

Available modes: `Client` (default), `Server`, `Admin`.

## Project Structure

```
BlazePort/
├── Components/
│   ├── Layout/          # MainLayout, NavMenu
│   └── Pages/           # Home (main scan UI), Error
├── Data/                # IPortProvider + DefaultPortProvider
├── Models/              # ServiceEndpoint, PingResult, PortResult, enums
├── Runtime/             # AppArgs, AppMode, ArgsParser
├── Services/            # PortScanner
└── Program.cs           # Entry point and DI configuration
```

## Architecture

Port data is loaded through the `IPortProvider` interface, which makes it easy to swap the data source (e.g. SQLite) without changing the rest of the application.

## License

MIT

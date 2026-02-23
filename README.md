# BlazePort

A Blazor Server application that scans network ports using the **Telnet protocol** to determine service availability.

## How It Works

BlazePort connects to each target port using a raw TCP socket — the same mechanism the `telnet` command uses. For each port:

1. **TCP Connect** — Opens a socket to `host:port` with a configurable timeout
2. **IAC Negotiation** — If the remote service sends Telnet protocol commands (IAC sequences), BlazePort responds with proper WILL/WONT/DO/DONT replies
3. **Banner Read** — After negotiation completes, the server sends its actual response (login prompt, service version, etc.)
4. **Status Determination** — Based on the connection result:
   - Connection succeeded → **Open**
   - Connection refused → **Closed**
   - No response within timeout → **Timeout** (likely filtered by firewall)
   - DNS resolution failed → **DNS Fail**
   - Network/host unreachable → **Unreachable**

### Why Telnet Protocol Matters

A simple TCP connect only checks if the port is open. BlazePort goes further by handling the **Telnet negotiation layer**:

```
Simple TCP check:
  Connect → Port is open → Done (no banner)

BlazePort Telnet check:
  Connect → Server sends IAC DO ECHO → BlazePort replies IAC WONT ECHO
          → Server sends IAC WILL SGA → BlazePort replies IAC DO SGA
          → Server sends "Login: "   → BlazePort captures this as banner
```

Without responding to IAC commands, many services (especially on port 23) will wait indefinitely for negotiation and never send useful data. BlazePort's `TelnetProtocol` module handles this automatically.

### Supported IAC Options

| Server Sends | BlazePort Responds | Reason |
|---|---|---|
| `DO SUPPRESS-GO-AHEAD` | `WILL SGA` | Accept — standard for modern telnet |
| `DO <other>` | `WONT <option>` | Refuse — we don't support it |
| `WILL ECHO` | `DO ECHO` | Accept — let server handle echo |
| `WILL SUPPRESS-GO-AHEAD` | `DO SGA` | Accept — standard for modern telnet |
| `WILL <other>` | `DONT <option>` | Refuse — we don't need it |
| Subnegotiation (`SB...SE`) | Skipped | Not needed for banner reading |

## Features

- **Telnet-based port scanning** with IAC negotiation for accurate banner retrieval
- **ICMP ping** check for each target host
- **Multi-pass banner reading** — loops until idle timeout, collecting all server output
- **Mode-based port sets** — Client, Server, and Admin modes load different default ports
- **Custom port addition** — add any port/protocol to the scan list at runtime
- **Real-time progress** — results update live as each port is checked
- **Graceful error handling** — port-in-use detection, try-catch on all event handlers

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
│   └── Pages/           # Home (scan UI), Error
├── Data/                # IPortProvider interface + DefaultPortProvider
├── Models/              # ServiceEndpoint, PingResult, PortResult, enums
├── Runtime/             # AppArgs, AppMode, ArgsParser (CLI parsing)
├── Services/
│   ├── PortScanner.cs       # Ping + TCP/Telnet port check
│   └── TelnetProtocol.cs    # IAC parsing and negotiation logic
└── Program.cs               # Entry point and DI configuration
```

## Architecture

### Port Data (IPortProvider)

Port data is loaded through the `IPortProvider` interface. Currently `DefaultPortProvider` serves hardcoded port lists. To switch to a database, implement `IPortProvider` (e.g. `SqlitePortProvider`) and change one line in `Program.cs`.

### Scan Flow

```
User clicks Run
  │
  ├── For each port in the list:
  │     ├── PingAsync(host)        → PingResult (OK / FAIL)
  │     └── CheckAsync(host, port) → PortResult (Open / Closed / Timeout / ...)
  │           │
  │           └── TcpCheckAsync
  │                 ├── Socket.ConnectAsync (with timeout)
  │                 └── TelnetReadBannerAsync (if banner read enabled)
  │                       ├── Read raw bytes from stream
  │                       ├── TelnetProtocol.ParseIac → separate clean text + IAC responses
  │                       ├── Write IAC responses back to server (negotiation)
  │                       ├── Append clean text to banner buffer
  │                       └── Loop until idle timeout or total timeout
  │
  └── UI updates after each port (real-time)
```

## License

MIT

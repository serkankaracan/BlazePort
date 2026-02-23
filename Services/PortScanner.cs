using BlazePort.Models;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace BlazePort.Services;

public sealed class PortScanner
{
    public async Task<PingResult> PingAsync(string host, int timeoutMs)
    {
        try
        {
            using var ping = new Ping();
            var reply = await ping.SendPingAsync(host, timeoutMs);

            return reply.Status == IPStatus.Success
                ? PingResult.Success(reply.RoundtripTime)
                : PingResult.Fail(reply.Status.ToString());
        }
        catch (Exception ex)
        {
            return PingResult.Fail(ex.Message);
        }
    }

    public Task<PortResult> CheckAsync(
        string host,
        int port,
        int timeoutMs,
        TransportProtocol protocol = TransportProtocol.Tcp,
        bool readBanner = false,
        int bannerMaxBytes = 256)
    {
        return protocol switch
        {
            TransportProtocol.Tcp => TcpCheckAsync(host, port, timeoutMs, readBanner, bannerMaxBytes),
            TransportProtocol.Udp => Task.FromResult(PortResult.Fail(PortStatus.Unknown, "UDP scan not implemented yet.")),
            _ => Task.FromResult(PortResult.Fail(PortStatus.Error, "Unknown protocol."))
        };
    }

    private static async Task<PortResult> TcpCheckAsync(
        string host,
        int port,
        int timeoutMs,
        bool readBanner,
        int bannerMaxBytes)
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var sw = Stopwatch.StartNew();

        try
        {
            var connectTask = socket.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));

            if (completed != connectTask)
                return PortResult.Timeout();

            await connectTask;
            sw.Stop();

            if (!readBanner)
                return PortResult.Open(sw.ElapsedMilliseconds);

            return await TelnetReadBannerAsync(socket, host, port, sw.ElapsedMilliseconds, timeoutMs, bannerMaxBytes);
        }
        catch (SocketException ex)
        {
            return ex.SocketErrorCode switch
            {
                SocketError.ConnectionRefused
                    => PortResult.Fail(PortStatus.Closed, "Connection refused"),
                SocketError.TimedOut
                    => PortResult.Timeout(),
                SocketError.HostNotFound or SocketError.NoData
                    => PortResult.Fail(PortStatus.DnsFail, ex.Message),
                SocketError.NetworkUnreachable or SocketError.HostUnreachable
                    => PortResult.Fail(PortStatus.Unreachable, ex.Message),
                _
                    => PortResult.Fail(PortStatus.Error, $"{ex.SocketErrorCode}: {ex.Message}")
            };
        }
        catch (Exception ex)
        {
            return PortResult.Fail(PortStatus.Error, ex.Message);
        }
    }

    /// <summary>
    /// Phase 1: Passive read — wait for server to send data (handles IAC + immediate banners).
    /// Phase 2: Active probe — if no text received and a probe exists for this port, send it and read.
    /// </summary>
    private static async Task<PortResult> TelnetReadBannerAsync(
        Socket socket,
        string host,
        int port,
        long connectTimeMs,
        int timeoutMs,
        int bannerMaxBytes)
    {
        var banner = new StringBuilder();

        try
        {
            socket.ReceiveTimeout = timeoutMs;
            using var ns = new NetworkStream(socket, ownsSocket: false);

            var buffer = new byte[Math.Clamp(bannerMaxBytes, 1, 4096)];
            var totalTimeout = Stopwatch.StartNew();
            var idleTimeoutMs = Math.Min(timeoutMs, 500);

            // Phase 1: Passive — read what the server sends on its own
            while (totalTimeout.ElapsedMilliseconds < timeoutMs && banner.Length < bannerMaxBytes)
            {
                var readTask = ns.ReadAsync(buffer, 0, buffer.Length);
                var completed = await Task.WhenAny(readTask, Task.Delay(idleTimeoutMs));

                if (completed != readTask)
                    break;

                var read = await readTask;
                if (read == 0) break;

                var (cleanOutput, iacResponses) = TelnetProtocol.ParseIac(buffer, read);

                if (iacResponses.Length > 0)
                {
                    try
                    {
                        await ns.WriteAsync(iacResponses);
                        await ns.FlushAsync();
                    }
                    catch { break; }
                }

                if (cleanOutput.Length > 0)
                    banner.Append(Encoding.ASCII.GetString(cleanOutput));
            }

            // Phase 2: Active probe — if nothing received, try sending a known probe
            if (banner.Length == 0 && totalTimeout.ElapsedMilliseconds < timeoutMs)
            {
                var probe = GetProbe(port, host);

                if (probe is not null)
                {
                    try
                    {
                        await ns.WriteAsync(Encoding.ASCII.GetBytes(probe));
                        await ns.FlushAsync();
                    }
                    catch { goto done; }

                    while (totalTimeout.ElapsedMilliseconds < timeoutMs && banner.Length < bannerMaxBytes)
                    {
                        var readTask = ns.ReadAsync(buffer, 0, buffer.Length);
                        var completed = await Task.WhenAny(readTask, Task.Delay(idleTimeoutMs));

                        if (completed != readTask)
                            break;

                        var read = await readTask;
                        if (read == 0) break;

                        banner.Append(Encoding.ASCII.GetString(buffer, 0, read));
                    }
                }
            }
        }
        catch
        {
            // Banner read failed; port is still open.
        }

        done:
        var text = banner.ToString().Trim();
        return PortResult.Open(connectTimeMs, string.IsNullOrWhiteSpace(text) ? null : text);
    }

    /// <summary>
    /// Returns a probe request for known protocols.
    /// Falls back to a generic CRLF probe for unknown ports.
    /// Returns null only for ports where raw TCP probing is impossible (e.g. TLS).
    /// </summary>
    private static string? GetProbe(int port, string host)
    {
        return port switch
        {
            // HTTP — send a minimal request
            80 or 8080 or 8000 or 8888 or 3000 or 5000
                => $"HEAD / HTTP/1.0\r\nHost: {host}\r\nConnection: close\r\n\r\n",

            // TLS — raw TCP won't work, skip probe
            443 or 8443
                => null,

            // SMTP — identify ourselves
            25 or 587
                => "EHLO blazeport\r\n",

            // Generic fallback — a bare CRLF triggers a response from most text-based services
            _ => "\r\n"
        };
    }
}

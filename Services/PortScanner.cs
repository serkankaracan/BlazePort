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

            return await TryReadBannerAsync(socket, sw.ElapsedMilliseconds, timeoutMs, bannerMaxBytes);
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

    private static async Task<PortResult> TryReadBannerAsync(
        Socket socket,
        long connectTimeMs,
        int timeoutMs,
        int bannerMaxBytes)
    {
        try
        {
            socket.ReceiveTimeout = timeoutMs;
            using var ns = new NetworkStream(socket, ownsSocket: false);

            var buffer = new byte[Math.Clamp(bannerMaxBytes, 1, 4096)];
            var readTask = ns.ReadAsync(buffer, 0, buffer.Length);
            var readCompleted = await Task.WhenAny(readTask, Task.Delay(Math.Min(timeoutMs, 1000)));

            if (readCompleted == readTask)
            {
                var read = await readTask;
                if (read > 0)
                {
                    var text = StripTelnetIac(Encoding.ASCII.GetString(buffer, 0, read));
                    return PortResult.Open(connectTimeMs, text.Trim());
                }
            }
        }
        catch
        {
            // Banner read failed; port is still open.
        }

        return PortResult.Open(connectTimeMs);
    }

    /// <summary>
    /// Strips telnet IAC (0xFF) negotiation bytes from the banner string.
    /// </summary>
    private static string StripTelnetIac(string input)
    {
        var sb = new StringBuilder(input.Length);
        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == (char)0xFF)
            {
                i += 2; // Skip IAC + command + option (3 bytes total)
                continue;
            }
            sb.Append(input[i]);
        }
        return sb.ToString();
    }
}

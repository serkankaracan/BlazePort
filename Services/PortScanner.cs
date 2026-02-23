using BlazePort.Models;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;

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

    public async Task<PortResult> CheckAsync(string host, int port, int timeoutMs)
    {
        using var client = new TcpClient();
        var sw = Stopwatch.StartNew();

        try
        {
            var connectTask = client.ConnectAsync(host, port);
            var completed = await Task.WhenAny(connectTask, Task.Delay(timeoutMs));

            if (completed != connectTask)
                return PortResult.Timeout();

            await connectTask;
            sw.Stop();
            return PortResult.Open(sw.ElapsedMilliseconds);
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
}

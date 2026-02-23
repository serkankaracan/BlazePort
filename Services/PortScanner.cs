using BlazePort.Models;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace BlazePort.Services
{
    public sealed class PortScanner
    {
        public async Task<PingResult> PingAsync(string host, int timeoutMs)
        {
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, timeoutMs);

                if (reply.Status == IPStatus.Success)
                    return PingResult.Success(reply.RoundtripTime);

                return PingResult.Fail(reply.Status.ToString());
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

        // Telnet taraması dediğin şeyin “genelleştirilmiş” hali bu.
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

                // ConnectAsync exceptionlarını observe etmek için:
                await connectTask;

                sw.Stop();

                if (!readBanner)
                    return PortResult.Open(sw.ElapsedMilliseconds);

                // Banner read (telnet-like): kısa bir okuma denemesi
                socket.ReceiveTimeout = timeoutMs;
                using var ns = new NetworkStream(socket, ownsSocket: false);

                var buffer = new byte[Math.Clamp(bannerMaxBytes, 1, 4096)];

                // Bazı servisler banner basmaz; read timeout olabilir. Bu durumda "Open" yine Open.
                try
                {
                    var readTask = ns.ReadAsync(buffer, 0, buffer.Length);
                    var readCompleted = await Task.WhenAny(readTask, Task.Delay(Math.Min(timeoutMs, 1000)));

                    if (readCompleted == readTask)
                    {
                        var read = await readTask;
                        if (read > 0)
                        {
                            var text = Encoding.ASCII.GetString(buffer, 0, read);
                            text = StripTelnetIac(text); // telnet negotiation gürültüsünü azaltmak için minimal temizlik
                            return PortResult.Open(sw.ElapsedMilliseconds, text.Trim());
                        }
                    }
                }
                catch
                {
                    // Banner okuyamadık -> sorun değil; port yine Open.
                }

                return PortResult.Open(sw.ElapsedMilliseconds);
            }
            catch (SocketException ex)
            {
                // Ayrım: closed vs unreachable vs dns fail vs timeout
                return ex.SocketErrorCode switch
                {
                    SocketError.ConnectionRefused =>
                        PortResult.Fail(PortStatus.Closed, "Connection refused"),

                    SocketError.TimedOut =>
                        PortResult.Timeout(),

                    SocketError.HostNotFound or SocketError.NoData =>
                        PortResult.Fail(PortStatus.DnsFail, ex.Message),

                    SocketError.NetworkUnreachable or SocketError.HostUnreachable =>
                        PortResult.Fail(PortStatus.Unreachable, ex.Message),

                    _ =>
                        PortResult.Fail(PortStatus.Error, $"{ex.SocketErrorCode}: {ex.Message}")
                };
            }
            catch (Exception ex)
            {
                return PortResult.Fail(PortStatus.Error, ex.Message);
            }
        }

        // Telnet protokolü IAC (0xFF) ile başlayan negotiation byte’ları gönderir.
        // Bu "telnetin yaptığı her şey" değil ama banner stringini kirleten durumları azaltır.
        private static string StripTelnetIac(string input)
        {
            // Çok minimal: 0xFF karakterini ve hemen arkasındaki 1-2 byte'ı temizlemek gibi
            // İstersen bunu ileride tam telnet negotiation parser’a yükseltirsin.
            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == (char)0xFF)
                {
                    // IAC + command + option (çoğu durumda 2 byte daha)
                    i += 2;
                    continue;
                }
                sb.Append(input[i]);
            }
            return sb.ToString();
        }
    }
}

using BlazePort.Models;
using BlazePort.Runtime;

namespace BlazePort.Data;

internal static class PortSeeder
{
    private static readonly IReadOnlyList<ServiceEndpoint> ClientPorts =
        new List<ServiceEndpoint>
        {
            new("HTTP", 80),
            new("HTTPS", 443),
            new("RDP", 3389),
            new("TELNET", 23)
        }.AsReadOnly();

    private static readonly IReadOnlyList<ServiceEndpoint> ServerPorts =
        new List<ServiceEndpoint>
        {
            new("DNS", 53, TransportProtocol.Udp),
            new("SMTP", 25),
            new("SSH", 22)
        }.AsReadOnly();

    public static IReadOnlyList<ServiceEndpoint> GetPorts(AppMode mode)
    {
        if (mode == AppMode.Server)
            return ServerPorts;

        return ClientPorts; // Client + Admin
    }
}
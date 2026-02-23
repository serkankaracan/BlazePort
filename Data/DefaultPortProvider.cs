using BlazePort.Models;
using BlazePort.Runtime;

namespace BlazePort.Data;

internal sealed class DefaultPortProvider : IPortProvider
{
    private static readonly IReadOnlyList<ServiceEndpoint> ClientPorts = new ServiceEndpoint[]
    {
        new("HTTP", 80),
        new("HTTPS", 443),
        new("RDP", 3389),
        new("TELNET", 23)
    };

    private static readonly IReadOnlyList<ServiceEndpoint> ServerPorts = new ServiceEndpoint[]
    {
        new("DNS", 53),
        new("SMTP", 25),
        new("SSH", 22)
    };

    public IReadOnlyList<ServiceEndpoint> GetPorts(AppMode mode)
    {
        return mode == AppMode.Server ? ServerPorts : ClientPorts;
    }
}

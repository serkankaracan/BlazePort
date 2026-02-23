namespace BlazePort.Models;

public sealed record ServiceEndpoint(
    string ServiceName,
    int Port,
    TransportProtocol Protocol = TransportProtocol.Tcp)
{
    public override string ToString() => $"{ServiceName} {Port}/{Protocol}";
}

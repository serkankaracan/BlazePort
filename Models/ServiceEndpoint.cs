namespace BlazePort.Models;

public sealed record ServiceEndpoint(
    string ServiceName,
    int Port,
    string Modes = "")
{
    public override string ToString() => $"{ServiceName} {Port}";
}

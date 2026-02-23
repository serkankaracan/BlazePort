namespace BlazePort.Models;

public sealed class PortResult
{
    public bool Ok => Status == PortStatus.Open;
    public PortStatus Status { get; }
    public long? ConnectTimeMs { get; }
    public string? Error { get; }

    private PortResult(PortStatus status, long? connectTimeMs, string? error)
    {
        Status = status;
        ConnectTimeMs = connectTimeMs;
        Error = error;
    }

    public static PortResult Open(long connectTimeMs)
        => new(PortStatus.Open, connectTimeMs, null);

    public static PortResult Fail(PortStatus status, string error)
        => new(status, null, error);

    public static PortResult Timeout()
        => new(PortStatus.Timeout, null, "Timeout");
}

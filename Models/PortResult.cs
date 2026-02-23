namespace BlazePort.Models;

public sealed class PortResult
{
    public bool Ok => Status == PortStatus.Open;
    public PortStatus Status { get; }
    public long? ConnectTimeMs { get; }
    public string? Banner { get; }
    public string? Error { get; }

    private PortResult(PortStatus status, long? connectTimeMs, string? banner, string? error)
    {
        Status = status;
        ConnectTimeMs = connectTimeMs;
        Banner = banner;
        Error = error;
    }

    public static PortResult Open(long connectTimeMs, string? banner = null)
        => new(PortStatus.Open, connectTimeMs, banner, null);

    public static PortResult Fail(PortStatus status, string error)
        => new(status, null, null, error);

    public static PortResult Timeout()
        => new(PortStatus.Timeout, null, null, "Timeout");
}

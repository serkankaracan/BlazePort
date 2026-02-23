namespace BlazePort.Models;

public sealed class PingResult
{
    public bool Ok { get; }
    public long? RoundtripTime { get; }
    public string? Error { get; }

    private PingResult(bool ok, long? roundtripTime, string? error)
    {
        Ok = ok;
        RoundtripTime = roundtripTime;
        Error = error;
    }

    public static PingResult Success(long roundtripTime)
        => new(true, roundtripTime, null);

    public static PingResult Fail(string message)
        => new(false, null, message);
}

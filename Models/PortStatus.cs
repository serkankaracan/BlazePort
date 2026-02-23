namespace BlazePort.Models
{
    public enum PortStatus
    {
        Open,
        Closed,       // connection refused vb.
        Timeout,      // filtered / dropped
        DnsFail,
        Unreachable,  // network/host unreachable
        Error,
        Unknown
    }
}

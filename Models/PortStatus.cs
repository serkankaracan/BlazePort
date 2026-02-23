namespace BlazePort.Models;

public enum PortStatus
{
    Open,
    Closed,
    Timeout,
    DnsFail,
    Unreachable,
    Error,
    Unknown
}

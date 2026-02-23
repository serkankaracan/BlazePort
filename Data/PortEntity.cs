namespace BlazePort.Data;

/// <summary>Row in the SQLite Ports table.</summary>
public sealed class PortEntity
{
    public long Id { get; set; }
    public string Mode { get; set; } = string.Empty;
    public int Port { get; set; }
    public string Name { get; set; } = string.Empty;
}

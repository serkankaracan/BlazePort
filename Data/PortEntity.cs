namespace BlazePort.Data;

/// <summary>Row in the SQLite Ports table.</summary>
public sealed class PortEntity
{
    public long Id { get; set; } // The unique identifier of the port
    public string Mode { get; set; } = string.Empty; // Client, Server, Admin
    public int Port { get; set; } // The port number
    public string Name { get; set; } = string.Empty; // The name of the port
}

namespace BlazePort.Models
{
    public class PortInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Host { get; set; } = "127.0.0.1";
        public int? Port { get; set; }
        public CheckType Type { get; set; }
        public PortStatus? LastPortStatus { get; set; }
        public bool? LastPingOk { get; set; }
    }
}

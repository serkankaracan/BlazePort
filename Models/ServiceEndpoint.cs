namespace BlazePort.Models
{
    public sealed class ServiceEndpoint
    {
        public string ServiceName { get; }
        public int Port { get; }
        public TransportProtocol Protocol { get; }

        public ServiceEndpoint(string serviceName, int port, TransportProtocol protocol = TransportProtocol.Tcp)
        {
            ServiceName = serviceName;
            Port = port;
            Protocol = protocol;
        }

        public override string ToString() => $"{ServiceName} {Port}/{Protocol}";
    }
}

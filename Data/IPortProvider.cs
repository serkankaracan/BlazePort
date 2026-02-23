using BlazePort.Models;
using BlazePort.Runtime;

namespace BlazePort.Data;

internal interface IPortProvider
{
    IReadOnlyList<ServiceEndpoint> GetPorts(AppMode mode);
}

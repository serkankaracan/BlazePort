using BlazePort.Runtime;

namespace BlazePort.Data;

internal interface IPortRepository
{
    void EnsureSchema();
    IReadOnlyList<PortEntity> GetByMode(AppMode mode);
    IReadOnlyList<PortEntity> GetAll();
    PortEntity? GetById(long id);
    long Add(string mode, int port, string name);
    void Update(long id, int port, string name);
    void Delete(long id);
}

using BlazePort.Models;
using BlazePort.Runtime;

namespace BlazePort.Data;

internal sealed class SqlitePortProvider
{
    private readonly PortRepository _repo;

    public SqlitePortProvider(PortRepository repo)
    {
        _repo = repo;
    }

    public IReadOnlyList<ServiceEndpoint> GetPorts(AppMode mode)
    {
        if (mode == AppMode.Admin)
        {
            return _repo.GetAll()
                .GroupBy(e => e.Port)
                .OrderBy(g => g.Key)
                .Select(g => new ServiceEndpoint(
                    g.First().Name,
                    g.Key,
                    string.Join(", ", g.Select(e => e.Mode).Distinct())))
                .ToList();
        }

        var entities = _repo.GetByMode(mode);
        return entities
            .Select(e => new ServiceEndpoint(e.Name, e.Port, e.Mode))
            .ToList();
    }
}

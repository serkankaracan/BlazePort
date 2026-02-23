using BlazePort.Runtime;
using Microsoft.EntityFrameworkCore;

namespace BlazePort.Data;

internal sealed class PortRepository : IPortRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory;

    public PortRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }

    public void EnsureSchema()
    {
        using var db = _factory.CreateDbContext();
        db.Database.EnsureCreated();

        if (!db.Ports.Any())
            SeedDefaults(db);
    }

    private static void SeedDefaults(AppDbContext db)
    {
        db.Ports.AddRange(
            new PortEntity { Mode = "Client", Port = 80, Name = "HTTP" },
            new PortEntity { Mode = "Client", Port = 443, Name = "HTTPS" },
            new PortEntity { Mode = "Client", Port = 3389, Name = "RDP" },
            new PortEntity { Mode = "Client", Port = 23, Name = "TELNET" },
            new PortEntity { Mode = "Server", Port = 53, Name = "DNS" },
            new PortEntity { Mode = "Server", Port = 25, Name = "SMTP" },
            new PortEntity { Mode = "Server", Port = 22, Name = "SSH" }
        );
        db.SaveChanges();
    }

    public IReadOnlyList<PortEntity> GetByMode(AppMode mode)
    {
        var modeStr = mode == AppMode.Server ? "Server" : "Client";
        using var db = _factory.CreateDbContext();
        return db.Ports
            .Where(p => p.Mode == modeStr)
            .OrderBy(p => p.Port)
            .ToList();
    }

    public IReadOnlyList<PortEntity> GetAll()
    {
        using var db = _factory.CreateDbContext();
        return db.Ports
            .OrderBy(p => p.Mode)
            .ThenBy(p => p.Port)
            .ToList();
    }

    public PortEntity? GetById(long id)
    {
        using var db = _factory.CreateDbContext();
        return db.Ports.Find(id);
    }

    public long Add(string mode, int port, string name)
    {
        using var db = _factory.CreateDbContext();

        if (db.Ports.Any(p => p.Mode == mode && p.Port == port))
            throw new InvalidOperationException(
                $"Port {port} already exists in {mode} mode.");

        var entity = new PortEntity { Mode = mode, Port = port, Name = name ?? "" };
        db.Ports.Add(entity);
        db.SaveChanges();
        return entity.Id;
    }

    public void Update(long id, int port, string name)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Ports.Find(id);
        if (entity is null) return;

        if (db.Ports.Any(p => p.Mode == entity.Mode && p.Port == port && p.Id != id))
            throw new InvalidOperationException(
                $"Port {port} already exists in {entity.Mode} mode.");

        entity.Port = port;
        entity.Name = name ?? "";
        db.SaveChanges();
    }

    public void Delete(long id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Ports.Find(id);
        if (entity is null) return;

        db.Ports.Remove(entity);
        db.SaveChanges();
    }
}

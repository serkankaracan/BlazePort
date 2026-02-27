using BlazePort.Runtime;
using Microsoft.EntityFrameworkCore;

namespace BlazePort.Data;

/// <summary>Repository implementation for the ports table</summary>
internal sealed class PortRepository
{
    private readonly IDbContextFactory<AppDbContext> _factory; // The database context factory

    // The constructor
    public PortRepository(IDbContextFactory<AppDbContext> factory)
    {
        _factory = factory;
    }
    public void EnsureSchema()
    {

        using var db = _factory.CreateDbContext(); // Create the database context
        db.Database.EnsureCreated(); // Ensure the schema is created

        if (!db.Ports.Any()) // If there are no ports, seed the default ports
            SeedDefaults(db); // Seed the default ports
    }

    // Seed the default ports
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

    // Get the ports by mode
    public IReadOnlyList<PortEntity> GetByMode(AppMode mode)
    {
        var modeStr = mode == AppMode.Server ? "Server" : "Client";
        using var db = _factory.CreateDbContext(); // Create the database context
        return db.Ports
            .Where(p => p.Mode == modeStr)
            .OrderBy(p => p.Port)
            .ToList();
    }

    // Get all ports
    public IReadOnlyList<PortEntity> GetAll()
    {
        using var db = _factory.CreateDbContext();
        return db.Ports
            .OrderBy(p => p.Mode)
            .ThenBy(p => p.Port)
            .ToList();
    }

    // Get the port by id
    public PortEntity? GetById(long id)
    {
        using var db = _factory.CreateDbContext();
        return db.Ports.Find(id);
    }

    // Add a new port
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

    // Update a port
    public void Update(long id, int port, string name)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Ports.Find(id);
        if (entity == null) return;

        if (db.Ports.Any(p => p.Mode == entity.Mode && p.Port == port && p.Id != id))
            throw new InvalidOperationException(
                $"Port {port} already exists in {entity.Mode} mode.");

        entity.Port = port;
        entity.Name = name ?? "";
        db.SaveChanges();
    }

    // Delete a port
    public void Delete(long id)
    {
        using var db = _factory.CreateDbContext();
        var entity = db.Ports.Find(id);
        if (entity == null) return;

        db.Ports.Remove(entity);
        db.SaveChanges();
    }
}

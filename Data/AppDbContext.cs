using Microsoft.EntityFrameworkCore;

namespace BlazePort.Data;

internal sealed class AppDbContext : DbContext
{
    public DbSet<PortEntity> Ports => Set<PortEntity>();

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PortEntity>(entity =>
        {
            entity.ToTable("Ports");
            entity.HasKey(e => e.Id);

            // Composite unique constraint on Mode + Port
            entity.HasIndex(e => new { e.Mode, e.Port }).IsUnique();

            entity.HasIndex(e => e.Mode);
        });
    }
}

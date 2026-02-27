using Microsoft.EntityFrameworkCore;

namespace BlazePort.Data;

// The application database context
internal sealed class AppDbContext : DbContext
{
    // The ports table
    public DbSet<PortEntity> Ports => Set<PortEntity>();

    // The constructor
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // The model creating method
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // The model builder
        modelBuilder.Entity<PortEntity>(entity =>
        {
            entity.ToTable("Ports"); // The table name
            entity.HasKey(e => e.Id); // The primary key

            // Composite unique constraint on Mode + Port (to prevent duplicate entries within the same mode)
            entity.HasIndex(e => new { e.Mode, e.Port }).IsUnique(); // The composite unique constraint

            entity.HasIndex(e => e.Mode); // The index on the Mode column (to speed up queries)
        });
    }
}

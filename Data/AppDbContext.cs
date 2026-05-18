using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<CallRecord> CallRecords { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CallRecord>()
            .HasIndex(c => c.Timestamp);

        modelBuilder.Entity<CallRecord>()
            .Property(c => c.RoomNumber)
            .IsRequired();

        modelBuilder.Entity<CallRecord>()
            .Property(c => c.PatientNumber)
            .IsRequired();
    }
}

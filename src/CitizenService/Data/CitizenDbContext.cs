using CitizenService.Models;
using Microsoft.EntityFrameworkCore;

namespace CitizenService.Data;

public class CitizenDbContext : DbContext
{
    public CitizenDbContext(DbContextOptions<CitizenDbContext> options) : base(options) { }

    public DbSet<CitizenProfile> CitizenProfiles => Set<CitizenProfile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<CitizenProfile>(entity =>
        {
            entity.ToTable("CitizenProfiles");
            entity.HasIndex(c => c.UserId).IsUnique();
            entity.HasIndex(c => c.NationalId).IsUnique();
            entity.HasIndex(c => c.Email);
        });
    }
}

using Microsoft.EntityFrameworkCore;
using ServiceRequestService.Models;

namespace ServiceRequestService.Data;

public class ServiceRequestDbContext : DbContext
{
    public ServiceRequestDbContext(DbContextOptions<ServiceRequestDbContext> options) : base(options) { }

    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceRequest>(entity =>
        {
            entity.ToTable("ServiceRequests");
            entity.HasIndex(sr => sr.CitizenUserId);
            entity.HasIndex(sr => sr.Status);
            entity.HasIndex(sr => sr.AssignedOfficerId);
            entity.HasIndex(sr => sr.LinkedDocumentId);
        });
    }
}

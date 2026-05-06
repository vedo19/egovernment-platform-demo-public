using DocumentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Data;

public class DocumentDbContext : DbContext
{
    public DocumentDbContext(DbContextOptions<DocumentDbContext> options)
        : base(options) { }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<SupportingDocument> SupportingDocuments => Set<SupportingDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasIndex(d => d.CitizenUserId);
            entity.HasIndex(d => d.Status);
            entity.HasIndex(d => d.DocumentType);
            entity.HasIndex(d => d.ReferenceNumber).IsUnique()
                  .HasFilter("\"ReferenceNumber\" IS NOT NULL");
        });

        modelBuilder.Entity<SupportingDocument>(entity =>
        {
            entity.ToTable("SupportingDocuments");
            entity.HasIndex(d => d.CitizenUserId);
            entity.HasIndex(d => d.ServiceRequestId);
            entity.HasIndex(d => d.UploadedAt);
        });
    }
}

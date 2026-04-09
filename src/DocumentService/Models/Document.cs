using System.ComponentModel.DataAnnotations;

namespace DocumentService.Models;

public class Document
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CitizenUserId { get; set; }

    [Required, MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty; // BirthCertificate, NationalId, MarriageCertificate, DeathCertificate, DrivingLicense

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Pending"; // Pending, Processing, Ready, Rejected, Collected

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }

    public Guid? ProcessedByOfficerId { get; set; }

    [MaxLength(100)]
    public string? ReferenceNumber { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? CompletedAt { get; set; }

    // ── PDF storage & expiry ──
    public byte[]? FileContent { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? GeneratedAt { get; set; }
}

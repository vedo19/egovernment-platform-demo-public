using System.ComponentModel.DataAnnotations;

namespace DocumentService.DTOs;

// ── Create ──
public class CreateDocumentRequestDto
{
    [Required, MaxLength(100)]
    public string DocumentType { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; set; }
}

public class UploadDocumentFileDto
{
    [Required]
    public IFormFile File { get; set; } = default!;
}

// ── Update Status (Admin / Officer) ──
public class UpdateDocumentStatusDto
{
    [Required, MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
}

// ── Response ──
public class DocumentDto
{
    public Guid Id { get; set; }
    public Guid CitizenUserId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? RejectionReason { get; set; }
    public Guid? ProcessedByOfficerId { get; set; }
    public string? ReferenceNumber { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? GeneratedAt { get; set; }
}

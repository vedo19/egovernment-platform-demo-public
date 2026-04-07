using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

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

// ── Update Status (Admin / Officer) ──
public class UpdateDocumentStatusDto
{
    [Required, MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? RejectionReason { get; set; }
}

public class RejectDocumentDto
{
    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class UploadSupportingDocumentDto
{
    [Required]
    public Guid ServiceRequestId { get; set; }

    [Required]
    public IFormFile File { get; set; } = default!;
}

public class SupportingDocumentDto
{
    public Guid Id { get; set; }
    public Guid CitizenUserId { get; set; }
    public Guid ServiceRequestId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; }
}

public class SupportingDocumentFileDto
{
    public Guid Id { get; set; }
    public Guid CitizenUserId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public byte[] FileData { get; set; } = Array.Empty<byte>();
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
    public Guid? AssignedOfficerId { get; set; }
    public Guid? ProcessedByOfficerId { get; set; }
    public string? ReferenceNumber { get; set; }
    public int ProgressPercentage { get; set; }
    public string ProgressColor { get; set; } = "blue";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

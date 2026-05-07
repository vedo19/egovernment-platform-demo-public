using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ServiceRequestService.DTOs;

public class CreateServiceRequestDto
{
    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
}

public class UpdateStatusDto
{
    [Required, MaxLength(30)]
    public string Status { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? AdminNotes { get; set; }
}

public class AssignOfficerRequestDto
{
    [Required]
    public Guid OfficerId { get; set; }
}

public class RequestDocumentsDto
{
    [Required, MaxLength(1000)]
    public string OfficerNote { get; set; } = string.Empty;
}

public class RejectServiceRequestDto
{
    [Required, MaxLength(1000)]
    public string Reason { get; set; } = string.Empty;
}

public class UploadDocumentDto
{
    [Required]
    public IFormFile File { get; set; } = default!;
}

public class ServiceRequestDto
{
    public Guid Id { get; set; }
    public Guid CitizenUserId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AdminNotes { get; set; }
    public string? OfficerNote { get; set; }
    public Guid? AssignedOfficerId { get; set; }
    public Guid? LinkedDocumentId { get; set; }
    public bool IsResubmittable { get; set; }
    public int ProgressPercentage { get; set; }
    public string ProgressColor { get; set; } = "blue";
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

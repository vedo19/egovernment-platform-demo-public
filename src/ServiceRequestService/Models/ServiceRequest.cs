using System.ComponentModel.DataAnnotations;

namespace ServiceRequestService.Models;

public class ServiceRequest
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CitizenUserId { get; set; }

    [Required, MaxLength(50)]
    public string Type { get; set; } = string.Empty; // Permit, Complaint

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required, MaxLength(30)]
    public string Status { get; set; } = "Submitted";

    [MaxLength(1000)]
    public string? AdminNotes { get; set; }

    [MaxLength(1000)]
    public string? OfficerNote { get; set; }

    public Guid? AssignedOfficerId { get; set; }

    public Guid? LinkedDocumentId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public DateTime? ResolvedAt { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace DocumentService.Models;

public class SupportingDocument
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid CitizenUserId { get; set; }

    [Required]
    public Guid ServiceRequestId { get; set; }

    [Required, MaxLength(255)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string ContentType { get; set; } = "application/pdf";

    [Required]
    public byte[] FileData { get; set; } = Array.Empty<byte>();

    public long FileSize { get; set; }

    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}

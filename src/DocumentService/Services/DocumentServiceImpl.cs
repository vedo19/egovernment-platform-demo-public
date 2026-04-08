using DocumentService.Data;
using DocumentService.DTOs;
using DocumentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Services;

public class DocumentServiceImpl : IDocumentService
{
    private readonly DocumentDbContext _db;

    private static readonly HashSet<string> ValidDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BirthCertificate", "NationalId", "MarriageCertificate",
        "DeathCertificate", "DrivingLicense"
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Processing", "Ready", "Rejected", "Collected"
    };

    public DocumentServiceImpl(DocumentDbContext db)
    {
        _db = db;
    }

    public async Task<DocumentDto> CreateAsync(Guid citizenUserId, CreateDocumentRequestDto dto)
    {
        if (!ValidDocumentTypes.Contains(dto.DocumentType))
            throw new ArgumentException($"Invalid document type '{dto.DocumentType}'. Valid types: {string.Join(", ", ValidDocumentTypes)}");

        var document = new Document
        {
            CitizenUserId = citizenUserId,
            DocumentType = dto.DocumentType,
            Title = dto.Title,
            Description = dto.Description
        };

        _db.Documents.Add(document);
        await _db.SaveChangesAsync();

        return ToDto(document);
    }

    public async Task<DocumentDto?> GetByIdAsync(Guid id)
    {
        var document = await _db.Documents.FindAsync(id);
        return document is null ? null : ToDto(document);
    }

    public async Task<IEnumerable<DocumentDto>> GetByCitizenAsync(Guid citizenUserId)
    {
        var documents = await _db.Documents
            .Where(d => d.CitizenUserId == citizenUserId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        return documents.Select(ToDto);
    }

    public async Task<IEnumerable<DocumentDto>> GetAllAsync(string? status, string? documentType)
    {
        var query = _db.Documents.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(d => d.Status == status);

        if (!string.IsNullOrWhiteSpace(documentType))
            query = query.Where(d => d.DocumentType == documentType);

        var documents = await query.OrderByDescending(d => d.CreatedAt).ToListAsync();
        return documents.Select(ToDto);
    }

    public async Task<IEnumerable<DocumentDto>> GetByOfficerAsync(Guid officerId)
    {
        var documents = await _db.Documents
            .Where(d => d.ProcessedByOfficerId == officerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();
        return documents.Select(ToDto);
    }

    public async Task<DocumentDto?> UpdateStatusAsync(Guid id, Guid officerId, UpdateDocumentStatusDto dto)
    {
        if (!ValidStatuses.Contains(dto.Status))
            throw new ArgumentException($"Invalid status '{dto.Status}'. Valid statuses: {string.Join(", ", ValidStatuses)}");

        var document = await _db.Documents.FindAsync(id);
        if (document is null) return null;

        document.Status = dto.Status;
        document.UpdatedAt = DateTime.UtcNow;

        if (document.ProcessedByOfficerId is null)
            document.ProcessedByOfficerId = officerId;

        if (dto.Status is "Rejected")
            document.RejectionReason = dto.RejectionReason;

        if (dto.Status is "Ready" or "Collected")
        {
            document.CompletedAt = DateTime.UtcNow;
            document.ReferenceNumber ??= GenerateReferenceNumber(document.DocumentType);
        }

        await _db.SaveChangesAsync();
        return ToDto(document);
    }

    public async Task<DocumentDto?> AssignOfficerAsync(Guid id, Guid officerId)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null) return null;

        document.ProcessedByOfficerId = officerId;
        document.UpdatedAt = DateTime.UtcNow;

        if (document.Status is "Pending")
            document.Status = "Processing";

        await _db.SaveChangesAsync();
        return ToDto(document);
    }

    public async Task<(Stream FileStream, string FileName, string ContentType)> GetDocumentFileAsync(Guid documentId, Guid userId, string userRole)
    {
        var document = await _db.Documents.FindAsync(documentId)
            ?? throw new KeyNotFoundException($"Document '{documentId}' not found.");

        if (userRole is "Citizen" && document.CitizenUserId != userId)
            throw new UnauthorizedAccessException();

        if (document.Status is not ("Ready" or "Collected"))
            throw new InvalidOperationException("Document is not yet ready for download.");

        var templatePath = Path.Combine(AppContext.BaseDirectory, "Templates", $"{document.DocumentType}.pdf");

        if (!File.Exists(templatePath))
            throw new KeyNotFoundException($"Template file for '{document.DocumentType}' not found.");

        var stream = new FileStream(templatePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var fileName = $"{document.DocumentType}_{document.ReferenceNumber ?? document.Id.ToString()}.pdf";

        return (stream, fileName, "application/pdf");
    }

    private static string GenerateReferenceNumber(string documentType)
    {
        var prefix = documentType switch
        {
            "BirthCertificate" => "BC",
            "NationalId" => "NID",
            "MarriageCertificate" => "MC",
            "DeathCertificate" => "DC",
            "DrivingLicense" => "DL",
            _ => "DOC"
        };
        return $"{prefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpperInvariant()}";
    }

    private static DocumentDto ToDto(Document d) => new()
    {
        Id = d.Id,
        CitizenUserId = d.CitizenUserId,
        DocumentType = d.DocumentType,
        Title = d.Title,
        Description = d.Description,
        Status = d.Status,
        RejectionReason = d.RejectionReason,
        ProcessedByOfficerId = d.ProcessedByOfficerId,
        ReferenceNumber = d.ReferenceNumber,
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        CompletedAt = d.CompletedAt
    };
}

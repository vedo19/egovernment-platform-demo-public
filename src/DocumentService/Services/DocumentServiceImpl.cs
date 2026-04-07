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
        "Submitted", "UnderReview", "Approved", "Rejected"
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
            Description = dto.Description,
            Status = "Submitted"
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
        {
            var normalized = DocumentWorkflow.NormalizeStatus(status);
            query = query.Where(d => d.Status == normalized);
        }

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
        var normalized = DocumentWorkflow.NormalizeStatus(dto.Status);
        if (!ValidStatuses.Contains(normalized))
            throw new ArgumentException($"Invalid status '{dto.Status}'. Valid statuses: {string.Join(", ", ValidStatuses)}");

        var document = await _db.Documents.FindAsync(id);
        if (document is null) return null;

        return await TransitionAsync(document, normalized, officerId, dto.RejectionReason, assignWhenMissing: true);
    }

    public async Task<DocumentDto?> AssignOfficerAsync(Guid id, Guid officerId)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null) return null;

        document.AssignedOfficerId = officerId;
        document.ProcessedByOfficerId = officerId;
        document.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        return ToDto(document);
    }

    public async Task<DocumentDto?> StartReviewAsync(Guid id, Guid actorId, bool isAdmin)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null) return null;

        var canSelfAssign = !isAdmin
                            && !document.AssignedOfficerId.HasValue
                            && !document.ProcessedByOfficerId.HasValue
                            && DocumentWorkflow.NormalizeStatus(document.Status).Equals("Submitted", StringComparison.OrdinalIgnoreCase);
        if (!canSelfAssign)
            EnsureOfficerAccess(document, actorId, isAdmin);

        return await TransitionAsync(document, "UnderReview", actorId, assignWhenMissing: true);
    }

    public async Task<DocumentDto?> ApproveAsync(Guid id, Guid actorId, bool isAdmin)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null) return null;

        EnsureOfficerAccess(document, actorId, isAdmin);
        return await TransitionAsync(document, "Approved", actorId);
    }

    public async Task<DocumentDto?> RejectAsync(Guid id, Guid actorId, string reason, bool isAdmin)
    {
        var document = await _db.Documents.FindAsync(id);
        if (document is null) return null;

        EnsureOfficerAccess(document, actorId, isAdmin);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");

        return await TransitionAsync(document, "Rejected", actorId, reason);
    }

    public async Task<SupportingDocumentDto> UploadSupportingDocumentAsync(Guid citizenUserId, Guid serviceRequestId, IFormFile file)
    {
        if (file is null || file.Length == 0)
            throw new ArgumentException("PDF file is required.");

        if (!"application/pdf".Equals(file.ContentType, StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only PDF files are supported.");

        await using var stream = new MemoryStream();
        await file.CopyToAsync(stream);

        var supportingDocument = new SupportingDocument
        {
            CitizenUserId = citizenUserId,
            ServiceRequestId = serviceRequestId,
            FileName = Path.GetFileName(file.FileName),
            ContentType = "application/pdf",
            FileSize = file.Length,
            FileData = stream.ToArray(),
            UploadedAt = DateTime.UtcNow
        };

        _db.SupportingDocuments.Add(supportingDocument);
        await _db.SaveChangesAsync();

        return new SupportingDocumentDto
        {
            Id = supportingDocument.Id,
            CitizenUserId = supportingDocument.CitizenUserId,
            ServiceRequestId = supportingDocument.ServiceRequestId,
            FileName = supportingDocument.FileName,
            ContentType = supportingDocument.ContentType,
            FileSize = supportingDocument.FileSize,
            UploadedAt = supportingDocument.UploadedAt
        };
    }

    public async Task<SupportingDocumentFileDto?> GetSupportingDocumentFileAsync(Guid id)
    {
        var file = await _db.SupportingDocuments.FindAsync(id);
        if (file is null) return null;

        return new SupportingDocumentFileDto
        {
            Id = file.Id,
            CitizenUserId = file.CitizenUserId,
            FileName = file.FileName,
            ContentType = file.ContentType,
            FileData = file.FileData
        };
    }

    private async Task<DocumentDto> TransitionAsync(
        Document document,
        string targetStatus,
        Guid actorId,
        string? rejectionReason = null,
        bool assignWhenMissing = false)
    {
        var current = DocumentWorkflow.NormalizeStatus(document.Status);
        var target = DocumentWorkflow.NormalizeStatus(targetStatus);

        if (!DocumentWorkflow.CanTransition(current, target))
            throw new ArgumentException($"Invalid transition: {current} -> {target}.");

        document.Status = target;
        document.UpdatedAt = DateTime.UtcNow;

        if (assignWhenMissing && !document.AssignedOfficerId.HasValue)
            document.AssignedOfficerId = actorId;

        if (!document.ProcessedByOfficerId.HasValue)
            document.ProcessedByOfficerId = actorId;

        if (target == "Rejected")
            document.RejectionReason = rejectionReason;

        if (target == "Approved")
        {
            document.CompletedAt = DateTime.UtcNow;
            document.ReferenceNumber ??= GenerateReferenceNumber(document.DocumentType);
        }

        await _db.SaveChangesAsync();
        return ToDto(document);
    }

    private static void EnsureOfficerAccess(Document document, Guid actorId, bool isAdmin)
    {
        if (isAdmin) return;

        if (document.AssignedOfficerId.HasValue && document.AssignedOfficerId.Value == actorId)
            return;

        if (document.ProcessedByOfficerId.HasValue && document.ProcessedByOfficerId.Value == actorId)
            return;

        throw new ArgumentException("Only the assigned officer can perform this action.");
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
        AssignedOfficerId = d.AssignedOfficerId,
        ProcessedByOfficerId = d.ProcessedByOfficerId,
        ReferenceNumber = d.ReferenceNumber,
        ProgressPercentage = DocumentWorkflow.GetProgressPercentage(d.Status),
        ProgressColor = DocumentWorkflow.GetProgressColor(d.Status),
        CreatedAt = d.CreatedAt,
        UpdatedAt = d.UpdatedAt,
        CompletedAt = d.CompletedAt
    };
}

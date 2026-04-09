using System.Text.Json;
using DocumentService.Data;
using DocumentService.DTOs;
using DocumentService.Models;
using Microsoft.EntityFrameworkCore;

namespace DocumentService.Services;

public class DocumentServiceImpl : IDocumentService
{
    private readonly DocumentDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPdfGeneratorService _pdfGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly HashSet<string> ValidDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "BirthCertificate", "NationalId", "MarriageCertificate",
        "DeathCertificate", "DrivingLicense"
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "Processing", "Ready", "Rejected", "Collected"
    };

    public DocumentServiceImpl(
        DocumentDbContext db,
        IHttpClientFactory httpClientFactory,
        IPdfGeneratorService pdfGenerator,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _pdfGenerator = pdfGenerator;
        _httpContextAccessor = httpContextAccessor;
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

        // ── Generate PDF on approval ──
        if (dto.Status is "Ready")
        {
            var citizen = await FetchCitizenDataAsync(document.CitizenUserId);
            var expiresAt = GetExpiryDate(document.DocumentType);

            document.FileContent = _pdfGenerator.GenerateDocument(
                document.DocumentType, citizen, document.ReferenceNumber!, expiresAt);
            document.GeneratedAt = DateTime.UtcNow;
            document.ExpiresAt = expiresAt;
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

    public async Task<(byte[] FileContent, string FileName, string ContentType)> GetDocumentFileAsync(Guid documentId, Guid userId, string userRole)
    {
        var document = await _db.Documents.FindAsync(documentId)
            ?? throw new KeyNotFoundException($"Document '{documentId}' not found.");

        if (userRole is "Citizen" && document.CitizenUserId != userId)
            throw new UnauthorizedAccessException();

        if (document.Status is not ("Ready" or "Collected"))
            throw new InvalidOperationException("Document is not yet ready for download.");

        if (document.FileContent is null)
            throw new InvalidOperationException("Document file has not been generated yet.");

        var fileName = $"{document.DocumentType}_{document.ReferenceNumber ?? document.Id.ToString()}.pdf";
        return (document.FileContent, fileName, "application/pdf");
    }

    public async Task<(byte[] FileContent, string FileName, string ContentType)> GeneratePreviewAsync(Guid documentId)
    {
        var document = await _db.Documents.FindAsync(documentId)
            ?? throw new KeyNotFoundException($"Document '{documentId}' not found.");

        var citizen = await FetchCitizenDataAsync(document.CitizenUserId);
        var refNumber = document.ReferenceNumber ?? "PREVIEW-" + document.Id.ToString()[..8].ToUpperInvariant();
        var expiresAt = GetExpiryDate(document.DocumentType);

        var pdfBytes = _pdfGenerator.GenerateDocument(
            document.DocumentType, citizen, refNumber, expiresAt, isDraft: true);

        var fileName = $"PREVIEW_{document.DocumentType}_{document.Id}.pdf";
        return (pdfBytes, fileName, "application/pdf");
    }

    // ── Private helpers ──

    private async Task<CitizenData> FetchCitizenDataAsync(Guid citizenUserId)
    {
        var httpClient = _httpClientFactory.CreateClient("CitizenService");

        // Forward the current request's JWT token for auth
        var authHeader = _httpContextAccessor.HttpContext?.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader))
            httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", authHeader);

        var response = await httpClient.GetAsync($"/api/citizens/by-user/{citizenUserId}");

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to fetch citizen profile for user {citizenUserId}. Status: {response.StatusCode}");

        var json = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<CitizenProfileResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? throw new InvalidOperationException("Failed to deserialize citizen profile.");

        return new CitizenData(
            profile.FullName,
            profile.NationalId,
            profile.DateOfBirth,
            profile.Address,
            profile.City,
            profile.Gender,
            profile.Email,
            profile.PhoneNumber
        );
    }

    private static DateTime? GetExpiryDate(string documentType) => documentType switch
    {
        "DrivingLicense" => DateTime.UtcNow.AddYears(10),
        "NationalId" => DateTime.UtcNow.AddYears(10),
        _ => null
    };

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
        CompletedAt = d.CompletedAt,
        ExpiresAt = d.ExpiresAt,
        GeneratedAt = d.GeneratedAt,
    };

    // ── Internal DTO for CitizenService response deserialization ──
    private class CitizenProfileResponse
    {
        public string FullName { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public DateOnly DateOfBirth { get; set; }
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
    }
}

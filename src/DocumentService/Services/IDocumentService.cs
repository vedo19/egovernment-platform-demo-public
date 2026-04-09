using DocumentService.DTOs;

namespace DocumentService.Services;

public interface IDocumentService
{
    Task<DocumentDto> CreateAsync(Guid citizenUserId, CreateDocumentRequestDto dto);
    Task<DocumentDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<DocumentDto>> GetByCitizenAsync(Guid citizenUserId);
    Task<IEnumerable<DocumentDto>> GetAllAsync(string? status, string? documentType);
    Task<IEnumerable<DocumentDto>> GetByOfficerAsync(Guid officerId);
    Task<DocumentDto?> UpdateStatusAsync(Guid id, Guid officerId, UpdateDocumentStatusDto dto);
    Task<DocumentDto?> AssignOfficerAsync(Guid id, Guid officerId);
    Task<string?> SaveAttachmentAsync(Guid documentId, IFormFile file);
    Task<string?> GetAttachmentPathAsync(Guid documentId);
}

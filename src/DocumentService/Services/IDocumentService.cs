using DocumentService.DTOs;
using Microsoft.AspNetCore.Http;

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
    Task<DocumentDto?> StartReviewAsync(Guid id, Guid actorId, bool isAdmin);
    Task<DocumentDto?> ApproveAsync(Guid id, Guid actorId, bool isAdmin);
    Task<DocumentDto?> RejectAsync(Guid id, Guid actorId, string reason, bool isAdmin);
    Task<SupportingDocumentDto> UploadSupportingDocumentAsync(Guid citizenUserId, Guid serviceRequestId, IFormFile file);
    Task<SupportingDocumentFileDto?> GetSupportingDocumentFileAsync(Guid id);
}

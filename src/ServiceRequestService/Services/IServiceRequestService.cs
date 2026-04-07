using ServiceRequestService.DTOs;
using Microsoft.AspNetCore.Http;

namespace ServiceRequestService.Services;

public interface IServiceRequestService
{
    Task<ServiceRequestDto> CreateAsync(Guid citizenUserId, CreateServiceRequestDto request);
    Task<ServiceRequestDto?> GetByIdAsync(Guid id);
    Task<IEnumerable<ServiceRequestDto>> GetByCitizenAsync(Guid citizenUserId);
    Task<IEnumerable<ServiceRequestDto>> GetAllAsync(string? statusFilter);
    Task<IEnumerable<ServiceRequestDto>> GetByOfficerAsync(Guid officerId);
    Task<ServiceRequestDto> UpdateStatusAsync(Guid id, Guid officerId, UpdateStatusDto request);
    Task<ServiceRequestDto> AssignOfficerAsync(Guid id, Guid officerId);
    Task<ServiceRequestDto> RequestDocumentsAsync(Guid id, Guid actorId, string officerNote, bool isAdmin);
    Task<ServiceRequestDto> ApproveAsync(Guid id, Guid actorId, bool isAdmin);
    Task<ServiceRequestDto> RejectDocumentsAsync(Guid id, Guid actorId, string reason, bool isAdmin);
    Task<ServiceRequestDto> RejectAsync(Guid id, Guid actorId, string reason, bool isAdmin);
    Task<ServiceRequestDto> UploadDocumentAsync(Guid id, Guid citizenUserId, IFormFile file, string? authorizationHeader);
}

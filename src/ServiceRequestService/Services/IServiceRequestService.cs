using ServiceRequestService.DTOs;

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
}

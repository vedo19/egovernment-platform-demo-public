using CitizenService.DTOs;

namespace CitizenService.Services;

public interface ICitizenService
{
    Task<CitizenProfileDto> CreateProfileAsync(Guid userId, string fullName, string email, CreateCitizenProfileDto request);
    Task<CitizenProfileDto?> GetProfileByUserIdAsync(Guid userId);
    Task<CitizenProfileDto?> GetProfileByIdAsync(Guid id);
    Task<CitizenProfileDto> UpdateProfileAsync(Guid userId, UpdateCitizenProfileDto request);
    Task<IEnumerable<CitizenProfileDto>> GetAllProfilesAsync();
}

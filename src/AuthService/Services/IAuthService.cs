using AuthService.DTOs;
using AuthService.Models;

namespace AuthService.Services;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request);
    Task<UserDto?> GetUserByIdAsync(Guid id);
    Task<UserDto> UpdateUserRoleAsync(Guid userId, string newRole);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
}

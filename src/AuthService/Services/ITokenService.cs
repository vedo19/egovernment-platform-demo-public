using AuthService.Models;

namespace AuthService.Services;

public interface ITokenService
{
    string GenerateToken(User user);
}

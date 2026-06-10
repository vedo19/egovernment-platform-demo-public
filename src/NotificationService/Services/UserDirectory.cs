using System.Text.Json;

namespace NotificationService.Services;

public class UserDirectory : IUserDirectory
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserDirectory> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public UserDirectory(HttpClient httpClient, ILogger<UserDirectory> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserInfo?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"/api/auth/internal/users/{userId}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AuthService user lookup failed for {UserId} with status {Status}", userId, response.StatusCode);
                return null;
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<UserInfo>(stream, JsonOptions, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch user {UserId} from AuthService", userId);
            return null;
        }
    }

    public async Task<IReadOnlyList<UserInfo>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await _httpClient.GetAsync($"/api/auth/internal/users-by-role/{role}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("AuthService role lookup failed for {Role} with status {Status}", role, response.StatusCode);
                return Array.Empty<UserInfo>();
            }

            var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var users = await JsonSerializer.DeserializeAsync<List<UserInfo>>(stream, JsonOptions, cancellationToken);
            return users ?? new List<UserInfo>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch users for role {Role} from AuthService", role);
            return Array.Empty<UserInfo>();
        }
    }
}

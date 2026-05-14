namespace NotificationService.Services;

public class UserInfo
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public interface IUserDirectory
{
    Task<UserInfo?> GetUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserInfo>> GetUsersByRoleAsync(string role, CancellationToken cancellationToken = default);
}

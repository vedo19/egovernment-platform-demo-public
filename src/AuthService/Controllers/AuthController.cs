using AuthService.DTOs;
using AuthService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AuthService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>Register a new user.</summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequestDto request)
    {
        var result = await _authService.RegisterAsync(request);
        return CreatedAtAction(nameof(GetUserById), new { id = result.UserId }, result);
    }

    /// <summary>Authenticate a user and return a JWT token.</summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var result = await _authService.LoginAsync(request);
        return Ok(result);
    }

    /// <summary>Get the current authenticated user's profile.</summary>
    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst("sub");

        if (userIdClaim is null || !Guid.TryParse(userIdClaim.Value, out var userId))
            return Unauthorized(new { error = "Invalid token." });

        var user = await _authService.GetUserByIdAsync(userId);
        if (user is null)
            return NotFound(new { error = "User not found." });

        return Ok(user);
    }

    /// <summary>Get a user by ID (internal / admin).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("users/{id:guid}")]
    public async Task<IActionResult> GetUserById(Guid id)
    {
        var user = await _authService.GetUserByIdAsync(id);
        if (user is null)
            return NotFound(new { error = "User not found." });

        return Ok(user);
    }

    /// <summary>List all users (admin only).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        var users = await _authService.GetAllUsersAsync();
        return Ok(users);
    }

    /// <summary>Update a user's role (Admin only).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("users/{id:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid id, [FromBody] UpdateRoleDto request)
    {
        var result = await _authService.UpdateUserRoleAsync(id, request.Role);
        return Ok(result);
    }

    /// <summary>Health check endpoint.</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", service = "AuthService" });
}

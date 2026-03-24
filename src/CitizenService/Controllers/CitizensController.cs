using CitizenService.DTOs;
using CitizenService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CitizenService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CitizensController : ControllerBase
{
    private readonly ICitizenService _citizenService;

    public CitizensController(ICitizenService citizenService)
    {
        _citizenService = citizenService;
    }

    /// <summary>Create a citizen profile for the authenticated user.</summary>
    [Authorize(Roles = "Citizen")]
    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateCitizenProfileDto request)
    {
        var userId = GetUserId();
        var fullName = User.FindFirst("name")?.Value ?? string.Empty;
        var email = User.FindFirst("email")?.Value ?? string.Empty;

        var result = await _citizenService.CreateProfileAsync(userId, fullName, email, request);
        return CreatedAtAction(nameof(GetMyProfile), null, result);
    }

    /// <summary>Get the authenticated citizen's own profile.</summary>
    [Authorize(Roles = "Citizen")]
    [HttpGet("profile")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetUserId();
        var profile = await _citizenService.GetProfileByUserIdAsync(userId);

        if (profile is null)
            return NotFound(new { error = "Citizen profile not found. Please create one first." });

        return Ok(profile);
    }

    /// <summary>Update the authenticated citizen's own profile.</summary>
    [Authorize(Roles = "Citizen")]
    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateCitizenProfileDto request)
    {
        var userId = GetUserId();
        var result = await _citizenService.UpdateProfileAsync(userId, request);
        return Ok(result);
    }

    /// <summary>Get a citizen profile by ID (Admin/Officer or internal service call).</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCitizenById(Guid id)
    {
        var profile = await _citizenService.GetProfileByIdAsync(id);
        if (profile is null)
            return NotFound(new { error = "Citizen profile not found." });

        return Ok(profile);
    }

    /// <summary>Get a citizen profile by UserId (internal service-to-service).</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpGet("by-user/{userId:guid}")]
    public async Task<IActionResult> GetCitizenByUserId(Guid userId)
    {
        var profile = await _citizenService.GetProfileByUserIdAsync(userId);
        if (profile is null)
            return NotFound(new { error = "Citizen profile not found." });

        return Ok(profile);
    }

    /// <summary>List all citizen profiles (Admin only).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet]
    public async Task<IActionResult> GetAllCitizens()
    {
        var profiles = await _citizenService.GetAllProfilesAsync();
        return Ok(profiles);
    }

    /// <summary>Health check.</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", service = "CitizenService" });

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Invalid token: missing sub claim.");
        return Guid.Parse(sub);
    }
}

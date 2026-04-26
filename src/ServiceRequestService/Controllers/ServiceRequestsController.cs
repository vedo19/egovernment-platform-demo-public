using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceRequestService.DTOs;
using ServiceRequestService.Services;

namespace ServiceRequestService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ServiceRequestsController : ControllerBase
{
    private readonly IServiceRequestService _serviceRequestService;

    public ServiceRequestsController(IServiceRequestService serviceRequestService)
    {
        _serviceRequestService = serviceRequestService;
    }

    /// <summary>Submit a new service request (Citizen only).</summary>
    [Authorize(Roles = "Citizen")]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceRequestDto request)
    {
        var userId = GetUserId();
        var result = await _serviceRequestService.CreateAsync(userId, request);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Get a service request by ID.</summary>
    [Authorize]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var sr = await _serviceRequestService.GetByIdAsync(id);
        if (sr is null)
            return NotFound(new { error = "Service request not found." });

        // Citizens can only see their own requests
        var role = User.FindFirst("role")?.Value;
        if (role == "Citizen" && sr.CitizenUserId != GetUserId())
            return Forbid();

        return Ok(sr);
    }

    /// <summary>Get all requests for the authenticated citizen.</summary>
    [Authorize(Roles = "Citizen")]
    [HttpGet("my-requests")]
    public async Task<IActionResult> GetMyRequests()
    {
        var userId = GetUserId();
        var requests = await _serviceRequestService.GetByCitizenAsync(userId);
        return Ok(requests);
    }

    /// <summary>List all service requests with optional status filter (Admin/Officer).</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status)
    {
        var requests = await _serviceRequestService.GetAllAsync(status);
        return Ok(requests);
    }

    /// <summary>List all service requests (Admin).</summary>
    [Authorize(Roles = "Admin")]
    [HttpGet("all")]
    public async Task<IActionResult> GetAllForAdmin([FromQuery] string? status)
    {
        var requests = await _serviceRequestService.GetAllAsync(status);
        return Ok(requests);
    }

    /// <summary>Get requests assigned to the current officer.</summary>
    [Authorize(Roles = "Officer")]
    [HttpGet("my-assignments")]
    public async Task<IActionResult> GetMyAssignments()
    {
        var officerId = GetUserId();
        var requests = await _serviceRequestService.GetByOfficerAsync(officerId);
        return Ok(requests);
    }

    /// <summary>Get requests assigned to current officer.</summary>
    [Authorize(Roles = "Officer")]
    [HttpGet("assigned-to-me")]
    public async Task<IActionResult> GetAssignedToMe()
    {
        var officerId = GetUserId();
        var requests = await _serviceRequestService.GetByOfficerAsync(officerId);
        return Ok(requests);
    }

    /// <summary>Update the status of a service request (Admin/Officer only).</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpPut("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusDto request)
    {
        var officerId = GetUserId();
        var result = await _serviceRequestService.UpdateStatusAsync(id, officerId, request);
        return Ok(result);
    }

    /// <summary>Assign an officer to a service request (Admin only).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/assign")]
    public async Task<IActionResult> AssignOfficer(Guid id, [FromBody] AssignOfficerRequestDto request)
    {
        var result = await _serviceRequestService.AssignOfficerAsync(id, request.OfficerId);
        return Ok(result);
    }

    /// <summary>Assign an officer to a service request (Admin only).</summary>
    [Authorize(Roles = "Admin")]
    [HttpPut("{id:guid}/assign-officer")]
    public async Task<IActionResult> AssignOfficerV2(Guid id, [FromBody] AssignOfficerRequestDto request)
    {
        var result = await _serviceRequestService.AssignOfficerAsync(id, request.OfficerId);
        return Ok(result);
    }

    /// <summary>Officer or admin requests supporting documents for permit.</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpPut("{id:guid}/request-documents")]
    public async Task<IActionResult> RequestDocuments(Guid id, [FromBody] RequestDocumentsDto request)
    {
        var actorId = GetUserId();
        var isAdmin = IsInRole("Admin");
        var result = await _serviceRequestService.RequestDocumentsAsync(id, actorId, request.OfficerNote, isAdmin);
        return Ok(result);
    }

    /// <summary>Officer or admin approves a request.</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpPut("{id:guid}/approve")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var actorId = GetUserId();
        var isAdmin = IsInRole("Admin");
        var result = await _serviceRequestService.ApproveAsync(id, actorId, isAdmin);
        return Ok(result);
    }

    /// <summary>Officer or admin rejects a request with a reason.</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpPut("{id:guid}/reject")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectServiceRequestDto request)
    {
        var actorId = GetUserId();
        var isAdmin = IsInRole("Admin");
        var result = await _serviceRequestService.RejectAsync(id, actorId, request.Reason, isAdmin);
        return Ok(result);
    }

    /// <summary>Officer or admin rejects uploaded permit documents and requests resubmission.</summary>
    [Authorize(Roles = "Admin,Officer")]
    [HttpPut("{id:guid}/reject-documents")]
    public async Task<IActionResult> RejectDocuments(Guid id, [FromBody] RejectServiceRequestDto request)
    {
        var actorId = GetUserId();
        var isAdmin = IsInRole("Admin");
        var result = await _serviceRequestService.RejectDocumentsAsync(id, actorId, request.Reason, isAdmin);
        return Ok(result);
    }

    /// <summary>Citizen links an uploaded document to permit request and resumes review flow.</summary>
    [Authorize(Roles = "Citizen")]
    [HttpPost("{id:guid}/upload-document")]
    public async Task<IActionResult> UploadDocument(Guid id, [FromForm] UploadDocumentDto request)
    {
        var citizenId = GetUserId();
        var authorizationHeader = Request.Headers.Authorization.ToString();
        var result = await _serviceRequestService.UploadDocumentAsync(id, citizenId, request.File, authorizationHeader);
        return Ok(result);
    }

    /// <summary>Health check.</summary>
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", service = "ServiceRequestService" });

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Invalid token: missing sub claim.");
        return Guid.Parse(sub);
    }

    private bool IsInRole(string role)
    {
        return User.FindFirst("role")?.Value?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
    }
}

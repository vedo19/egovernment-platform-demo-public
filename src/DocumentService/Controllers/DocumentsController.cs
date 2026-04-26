using DocumentService.DTOs;
using DocumentService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocumentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    // ── POST /api/documents ── Citizen creates a document request
    [HttpPost]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> Create([FromBody] CreateDocumentRequestDto dto)
    {
        var userId = GetUserId();
        var result = await _documentService.CreateAsync(userId, dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // ── GET /api/documents/{id} ── Any authenticated user (citizens only see own)
    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> GetById(Guid id)
    {
        var document = await _documentService.GetByIdAsync(id);
        if (document is null) return NotFound();

        var role = User.FindFirst("role")?.Value;
        var userId = GetUserId();

        if (role is "Citizen" && document.CitizenUserId != userId)
            return Forbid();

        return Ok(document);
    }

    // ── GET /api/documents/my-documents ── Citizen gets own document requests
    [HttpGet("my-documents")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> GetMyDocuments()
    {
        var userId = GetUserId();
        var documents = await _documentService.GetByCitizenAsync(userId);
        return Ok(documents);
    }

    // ── GET /api/documents ── Admin/Officer list all, optional ?status=&documentType= filter
    [HttpGet]
    [Authorize(Roles = "Admin,Officer")]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? documentType)
    {
        var documents = await _documentService.GetAllAsync(status, documentType);
        return Ok(documents);
    }

    // ── GET /api/documents/my-assignments ── Officer gets assigned documents
    [HttpGet("my-assignments")]
    [Authorize(Roles = "Officer")]
    public async Task<IActionResult> GetMyAssignments()
    {
        var officerId = GetUserId();
        var documents = await _documentService.GetByOfficerAsync(officerId);
        return Ok(documents);
    }

    [HttpGet("assigned-to-me")]
    [Authorize(Roles = "Officer")]
    public async Task<IActionResult> GetAssignedToMe()
    {
        var officerId = GetUserId();
        var documents = await _documentService.GetByOfficerAsync(officerId);
        return Ok(documents);
    }

    // ── PUT /api/documents/{id}/status ── Admin/Officer updates status
    [HttpPut("{id:guid}/status")]
    [Authorize(Roles = "Admin,Officer")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateDocumentStatusDto dto)
    {
        var officerId = GetUserId();
        var result = await _documentService.UpdateStatusAsync(id, officerId, dto);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/start-review")]
    [Authorize(Roles = "Admin,Officer")]
    public async Task<IActionResult> StartReview(Guid id)
    {
        var actorId = GetUserId();
        var result = await _documentService.StartReviewAsync(id, actorId, IsInRole("Admin"));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Officer")]
    public async Task<IActionResult> Approve(Guid id)
    {
        var actorId = GetUserId();
        var result = await _documentService.ApproveAsync(id, actorId, IsInRole("Admin"));
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPut("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Officer")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] RejectDocumentDto dto)
    {
        var actorId = GetUserId();
        var result = await _documentService.RejectAsync(id, actorId, dto.Reason, IsInRole("Admin"));
        return result is null ? NotFound() : Ok(result);
    }

    // ── PUT /api/documents/{id}/assign ── Admin assigns officer
    [HttpPut("{id:guid}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignOfficer(Guid id, [FromBody] AssignOfficerDto dto)
    {
        var result = await _documentService.AssignOfficerAsync(id, dto.OfficerId);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("supporting-files")]
    [Authorize(Roles = "Citizen")]
    public async Task<IActionResult> UploadSupportingFile([FromForm] UploadSupportingDocumentDto dto)
    {
        var citizenId = GetUserId();
        var result = await _documentService.UploadSupportingDocumentAsync(citizenId, dto.ServiceRequestId, dto.File);
        return Ok(result);
    }

    [HttpGet("supporting-files/{id:guid}/download")]
    [Authorize]
    public async Task<IActionResult> DownloadSupportingFile(Guid id)
    {
        var file = await _documentService.GetSupportingDocumentFileAsync(id);
        if (file is null) return NotFound();

        var role = User.FindFirst("role")?.Value;
        var userId = GetUserId();

        if (role is "Citizen" && file.CitizenUserId != userId)
            return Forbid();

        return File(file.FileData, file.ContentType, file.FileName);
    }

    // ── GET /api/documents/health ──
    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { status = "healthy", service = "DocumentService" });

    private Guid GetUserId()
    {
        var sub = User.FindFirst("sub")?.Value
            ?? throw new UnauthorizedAccessException("Missing sub claim.");
        return Guid.Parse(sub);
    }

    private bool IsInRole(string role)
    {
        return User.FindFirst("role")?.Value?.Equals(role, StringComparison.OrdinalIgnoreCase) == true;
    }
}

public class AssignOfficerDto
{
    public Guid OfficerId { get; set; }
}

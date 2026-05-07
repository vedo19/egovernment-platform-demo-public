using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using ServiceRequestService.Data;
using ServiceRequestService.DTOs;
using ServiceRequestService.Models;

namespace ServiceRequestService.Services;

public class ServiceRequestServiceImpl : IServiceRequestService
{
    private readonly ServiceRequestDbContext _context;
    private readonly IDocumentStorageClient _documentStorageClient;

    public ServiceRequestServiceImpl(ServiceRequestDbContext context, IDocumentStorageClient documentStorageClient)
    {
        _context = context;
        _documentStorageClient = documentStorageClient;
    }

    public async Task<ServiceRequestDto> CreateAsync(Guid citizenUserId, CreateServiceRequestDto request)
    {
        if (!ServiceRequestWorkflow.ValidTypes.Contains(request.Type))
            throw new ArgumentException($"Invalid type '{request.Type}'. Valid types: Permit, Complaint.");

        var serviceRequest = new ServiceRequest
        {
            CitizenUserId = citizenUserId,
            Type = request.Type,
            Title = request.Title,
            Description = request.Description,
            Status = "Submitted",
            CreatedAt = DateTime.UtcNow
        };

        _context.ServiceRequests.Add(serviceRequest);
        await _context.SaveChangesAsync();

        return MapToDto(serviceRequest);
    }

    public async Task<ServiceRequestDto?> GetByIdAsync(Guid id)
    {
        var sr = await _context.ServiceRequests.FindAsync(id);
        return sr is null ? null : MapToDto(sr);
    }

    public async Task<IEnumerable<ServiceRequestDto>> GetByCitizenAsync(Guid citizenUserId)
    {
        return await _context.ServiceRequests
            .Where(sr => sr.CitizenUserId == citizenUserId)
            .OrderByDescending(sr => sr.CreatedAt)
            .Select(sr => MapToDto(sr))
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceRequestDto>> GetAllAsync(string? statusFilter)
    {
        var query = _context.ServiceRequests.AsQueryable();

        if (!string.IsNullOrWhiteSpace(statusFilter))
        {
            var normalized = ServiceRequestWorkflow.NormalizeStatus(statusFilter);
            if (!ServiceRequestWorkflow.ValidStatuses.Contains(normalized))
                throw new ArgumentException($"Invalid status filter '{statusFilter}'.");

            query = query.Where(sr => sr.Status == normalized);
        }

        return await query
            .OrderByDescending(sr => sr.CreatedAt)
            .Select(sr => MapToDto(sr))
            .ToListAsync();
    }

    public async Task<IEnumerable<ServiceRequestDto>> GetByOfficerAsync(Guid officerId)
    {
        return await _context.ServiceRequests
            .Where(sr => sr.AssignedOfficerId == officerId)
            .OrderByDescending(sr => sr.CreatedAt)
            .Select(sr => MapToDto(sr))
            .ToListAsync();
    }

    public async Task<ServiceRequestDto> UpdateStatusAsync(Guid id, Guid officerId, UpdateStatusDto request)
    {
        var normalizedTarget = ServiceRequestWorkflow.NormalizeStatus(request.Status);
        if (!ServiceRequestWorkflow.ValidStatuses.Contains(normalizedTarget))
            throw new ArgumentException($"Invalid status '{request.Status}'.");

        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        return await TransitionToAsync(sr, normalizedTarget, officerId, request.AdminNotes, assignWhenMissing: true);
    }

    public async Task<ServiceRequestDto> AssignOfficerAsync(Guid id, Guid officerId)
    {
        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        var normalizedCurrent = ServiceRequestWorkflow.NormalizeStatus(sr.Status);
        if (!ServiceRequestWorkflow.CanTransition(sr.Type, normalizedCurrent, "OfficerAssigned"))
            throw new ArgumentException($"Invalid transition for {sr.Type}: {normalizedCurrent} -> OfficerAssigned.");

        sr.AssignedOfficerId = officerId;
        sr.Status = "OfficerAssigned";
        sr.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(sr);
    }

    public async Task<ServiceRequestDto> RequestDocumentsAsync(Guid id, Guid actorId, string officerNote, bool isAdmin)
    {
        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        EnsureOfficerAccess(sr, actorId, isAdmin);

        if (!sr.Type.Equals("Permit", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Request documents action is only valid for Permit requests.");

        if (string.IsNullOrWhiteSpace(officerNote))
            throw new ArgumentException("Officer note is required when requesting documents.");

        return await TransitionToAsync(sr, "AwaitingDocuments", actorId, officerNote);
    }

    public async Task<ServiceRequestDto> ApproveAsync(Guid id, Guid actorId, bool isAdmin)
    {
        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        EnsureOfficerAccess(sr, actorId, isAdmin);

        return await TransitionToAsync(sr, "Approved", actorId);
    }

    public async Task<ServiceRequestDto> RejectDocumentsAsync(Guid id, Guid actorId, string reason, bool isAdmin)
    {
        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        EnsureOfficerAccess(sr, actorId, isAdmin);

        if (!sr.Type.Equals("Permit", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Reject documents action is only valid for Permit requests.");

        if (ServiceRequestWorkflow.NormalizeStatus(sr.Status) != "UnderReview")
            throw new ArgumentException("Reject documents action is only valid from UnderReview status.");

        if (!sr.LinkedDocumentId.HasValue)
            throw new ArgumentException("Cannot reject documents because no linked document exists.");

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");

        return await TransitionToAsync(sr, "DocumentsRejected", actorId, reason);
    }

    public async Task<ServiceRequestDto> RejectAsync(Guid id, Guid actorId, string reason, bool isAdmin)
    {
        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        EnsureOfficerAccess(sr, actorId, isAdmin);

        if (string.IsNullOrWhiteSpace(reason))
            throw new ArgumentException("Rejection reason is required.");

        return await TransitionToAsync(sr, "Rejected", actorId, reason);
    }

    public async Task<ServiceRequestDto> UploadDocumentAsync(Guid id, Guid citizenUserId, IFormFile file, string? authorizationHeader)
    {
        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        if (sr.CitizenUserId != citizenUserId)
            throw new UnauthorizedAccessException("You can only upload documents for your own request.");

        if (!sr.Type.Equals("Permit", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Document upload is only valid for Permit requests.");

        if (file is null || file.Length == 0)
            throw new ArgumentException("PDF file is required.");

        if (!"application/pdf".Equals(file.ContentType, StringComparison.OrdinalIgnoreCase)
            && !Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Only PDF files are supported.");

        var current = ServiceRequestWorkflow.NormalizeStatus(sr.Status);
        if (current is not "AwaitingDocuments" and not "DocumentsRejected")
            throw new ArgumentException("Documents can only be uploaded when status is AwaitingDocuments or DocumentsRejected.");

        var linkedDocumentId = await _documentStorageClient.UploadSupportingDocumentAsync(sr.Id, file, authorizationHeader);
        sr.LinkedDocumentId = linkedDocumentId;
        return await TransitionToAsync(sr, "UnderReview", sr.AssignedOfficerId ?? Guid.Empty);
    }

    private async Task<ServiceRequestDto> TransitionToAsync(
        ServiceRequest sr,
        string targetStatus,
        Guid actorId,
        string? officerNote = null,
        bool assignWhenMissing = false)
    {
        var normalizedCurrent = ServiceRequestWorkflow.NormalizeStatus(sr.Status);
        var normalizedTarget = ServiceRequestWorkflow.NormalizeStatus(targetStatus);

        if (!ServiceRequestWorkflow.CanTransition(sr.Type, normalizedCurrent, normalizedTarget))
            throw new ArgumentException($"Invalid transition for {sr.Type}: {normalizedCurrent} -> {normalizedTarget}.");

        sr.Status = normalizedTarget;
        sr.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(officerNote))
        {
            sr.OfficerNote = officerNote;
            sr.AdminNotes = officerNote;
        }

        if (assignWhenMissing && sr.AssignedOfficerId is null && actorId != Guid.Empty)
            sr.AssignedOfficerId = actorId;

        if (ServiceRequestWorkflow.IsClosed(normalizedTarget))
            sr.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(sr);
    }

    private static void EnsureOfficerAccess(ServiceRequest sr, Guid actorId, bool isAdmin)
    {
        if (isAdmin)
            return;

        if (!sr.AssignedOfficerId.HasValue)
            throw new ArgumentException("Request has no assigned officer.");

        if (sr.AssignedOfficerId != actorId)
            throw new ArgumentException("Only the assigned officer can perform this action.");
    }

    private static ServiceRequestDto MapToDto(ServiceRequest sr) => new()
    {
        Id = sr.Id,
        CitizenUserId = sr.CitizenUserId,
        Type = sr.Type,
        Title = sr.Title,
        Description = sr.Description,
        Status = sr.Status,
        AdminNotes = sr.AdminNotes,
        OfficerNote = sr.OfficerNote,
        AssignedOfficerId = sr.AssignedOfficerId,
        LinkedDocumentId = sr.LinkedDocumentId,
        IsResubmittable = ServiceRequestWorkflow.IsResubmittable(sr.Type, sr.Status),
        ProgressPercentage = ServiceRequestWorkflow.GetProgressPercentage(sr.Type, sr.Status),
        ProgressColor = ServiceRequestWorkflow.GetProgressColor(sr.Status),
        CreatedAt = sr.CreatedAt,
        UpdatedAt = sr.UpdatedAt,
        ResolvedAt = sr.ResolvedAt
    };
}

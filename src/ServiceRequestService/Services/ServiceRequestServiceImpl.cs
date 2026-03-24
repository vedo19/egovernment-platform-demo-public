using Microsoft.EntityFrameworkCore;
using ServiceRequestService.Data;
using ServiceRequestService.DTOs;
using ServiceRequestService.Models;

namespace ServiceRequestService.Services;

public class ServiceRequestServiceImpl : IServiceRequestService
{
    private readonly ServiceRequestDbContext _context;

    private static readonly HashSet<string> ValidTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Permit", "Complaint"
    };

    private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pending", "InProgress", "Resolved", "Rejected"
    };

    public ServiceRequestServiceImpl(ServiceRequestDbContext context)
    {
        _context = context;
    }

    public async Task<ServiceRequestDto> CreateAsync(Guid citizenUserId, CreateServiceRequestDto request)
    {
        if (!ValidTypes.Contains(request.Type))
            throw new ArgumentException($"Invalid type '{request.Type}'. Valid types: Permit, Complaint.");

        var serviceRequest = new ServiceRequest
        {
            CitizenUserId = citizenUserId,
            Type = request.Type,
            Title = request.Title,
            Description = request.Description,
            Status = "Pending",
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
            if (!ValidStatuses.Contains(statusFilter))
                throw new ArgumentException($"Invalid status filter '{statusFilter}'.");

            query = query.Where(sr => sr.Status == statusFilter);
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
        if (!ValidStatuses.Contains(request.Status))
            throw new ArgumentException($"Invalid status '{request.Status}'. Valid statuses: Pending, InProgress, Resolved, Rejected.");

        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        sr.Status = request.Status;
        sr.AdminNotes = request.AdminNotes ?? sr.AdminNotes;
        sr.UpdatedAt = DateTime.UtcNow;

        if (sr.AssignedOfficerId is null)
            sr.AssignedOfficerId = officerId;

        if (request.Status is "Resolved" or "Rejected")
            sr.ResolvedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(sr);
    }

    public async Task<ServiceRequestDto> AssignOfficerAsync(Guid id, Guid officerId)
    {
        var sr = await _context.ServiceRequests.FindAsync(id)
            ?? throw new KeyNotFoundException("Service request not found.");

        sr.AssignedOfficerId = officerId;
        sr.UpdatedAt = DateTime.UtcNow;

        if (sr.Status == "Pending")
            sr.Status = "InProgress";

        await _context.SaveChangesAsync();

        return MapToDto(sr);
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
        AssignedOfficerId = sr.AssignedOfficerId,
        CreatedAt = sr.CreatedAt,
        UpdatedAt = sr.UpdatedAt,
        ResolvedAt = sr.ResolvedAt
    };
}

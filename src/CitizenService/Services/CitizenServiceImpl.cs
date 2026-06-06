using CitizenService.Data;
using CitizenService.DTOs;
using CitizenService.Models;
using Microsoft.EntityFrameworkCore;

namespace CitizenService.Services;

public class CitizenServiceImpl : ICitizenService
{
    private readonly CitizenDbContext _context;

    public CitizenServiceImpl(CitizenDbContext context)
    {
        _context = context;
    }

    public async Task<CitizenProfileDto> CreateProfileAsync(
        Guid userId, string fullName, string email, CreateCitizenProfileDto request)
    {
        var existing = await _context.CitizenProfiles.AnyAsync(c => c.UserId == userId);
        if (existing)
            throw new InvalidOperationException("A profile already exists for this user.");

        var nationalIdExists = await _context.CitizenProfiles
            .AnyAsync(c => c.NationalId == request.NationalId);
        if (nationalIdExists)
            throw new InvalidOperationException("A profile with this National ID already exists.");

        var profile = new CitizenProfile
        {
            UserId = userId,
            FullName = fullName,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = email.ToLowerInvariant(),
            PhoneNumber = request.PhoneNumber,
            NationalId = request.NationalId,
            DateOfBirth = request.DateOfBirth,
            Address = request.Address,
            City = request.City,
            Gender = request.Gender,
            PlaceOfBirth = request.PlaceOfBirth,
            PlaceOfResidence = request.PlaceOfResidence,
            ZipCode = request.ZipCode,
            Citizenship = request.Citizenship,
            CreatedAt = DateTime.UtcNow
        };

        _context.CitizenProfiles.Add(profile);
        await _context.SaveChangesAsync();

        return MapToDto(profile);
    }

    public async Task<CitizenProfileDto?> GetProfileByUserIdAsync(Guid userId)
    {
        var profile = await _context.CitizenProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId);

        return profile is null ? null : MapToDto(profile);
    }

    public async Task<CitizenProfileDto?> GetProfileByIdAsync(Guid id)
    {
        var profile = await _context.CitizenProfiles.FindAsync(id);
        return profile is null ? null : MapToDto(profile);
    }

    public async Task<CitizenProfileDto> UpdateProfileAsync(Guid userId, UpdateCitizenProfileDto request)
    {
        var profile = await _context.CitizenProfiles
            .FirstOrDefaultAsync(c => c.UserId == userId)
            ?? throw new KeyNotFoundException("Citizen profile not found.");

        if (!string.IsNullOrWhiteSpace(request.NationalId) && request.NationalId != profile.NationalId)
        {
            var nationalIdExists = await _context.CitizenProfiles
                .AnyAsync(c => c.NationalId == request.NationalId && c.UserId != userId);
            if (nationalIdExists)
                throw new InvalidOperationException("A profile with this National ID already exists.");

            profile.NationalId = request.NationalId;
        }

        if (request.DateOfBirth is not null) profile.DateOfBirth = request.DateOfBirth.Value;
        if (request.PhoneNumber is not null) profile.PhoneNumber = request.PhoneNumber;
        if (request.FirstName is not null) profile.FirstName = request.FirstName;
        if (request.LastName is not null) profile.LastName = request.LastName;
        if (request.Address is not null) profile.Address = request.Address;
        if (request.City is not null) profile.City = request.City;
        if (request.Gender is not null) profile.Gender = request.Gender;
        if (request.PlaceOfBirth is not null) profile.PlaceOfBirth = request.PlaceOfBirth;
        if (request.PlaceOfResidence is not null) profile.PlaceOfResidence = request.PlaceOfResidence;
        if (request.ZipCode is not null) profile.ZipCode = request.ZipCode;
        if (request.Citizenship is not null) profile.Citizenship = request.Citizenship;

        profile.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToDto(profile);
    }

    public async Task<IEnumerable<CitizenProfileDto>> GetAllProfilesAsync()
    {
        return await _context.CitizenProfiles
            .Select(c => MapToDto(c))
            .ToListAsync();
    }

    private static CitizenProfileDto MapToDto(CitizenProfile profile) => new()
    {
        Id = profile.Id,
        UserId = profile.UserId,
        FullName = profile.FullName,
        FirstName = profile.FirstName,
        LastName = profile.LastName,
        Email = profile.Email,
        PhoneNumber = profile.PhoneNumber,
        NationalId = profile.NationalId,
        DateOfBirth = profile.DateOfBirth,
        Address = profile.Address,
        City = profile.City,
        Gender = profile.Gender,
        PlaceOfBirth = profile.PlaceOfBirth,
        PlaceOfResidence = profile.PlaceOfResidence,
        ZipCode = profile.ZipCode,
        Citizenship = profile.Citizenship,
        CreatedAt = profile.CreatedAt,
        UpdatedAt = profile.UpdatedAt
    };
}

using api.Data;
using api.DTOs;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Endpoints;

public static class CasterEndpoints
{
    public static void MapCasterEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/casters").WithTags("Casters");

        group.MapGet("/", GetAllCasters);
        group.MapGet("/{id}", GetCasterById);
        group.MapPost("/", CreateCaster);
        group.MapPut("/{id}", UpdateCaster);
        group.MapDelete("/{id}", DeleteCaster);
    }

    private static async Task<IResult> GetAllCasters(ApplicationDbContext db)
    {
        var casters = await db.Casters.ToListAsync();
        return Results.Ok(casters.Select(c => MapToCasterDto(c)));
    }

    private static async Task<IResult> GetCasterById(int id, ApplicationDbContext db)
    {
        var caster = await db.Casters.FindAsync(id);
        
        if (caster is null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(MapToCasterDto(caster));
    }

    private static async Task<IResult> CreateCaster(
        CasterCreateDto casterDto, 
        IStorageService storageService,
        ApplicationDbContext db)
    {
        // Check if email already exists
        if (await db.Casters.AnyAsync(c => c.Email == casterDto.Email))
        {
            return Results.BadRequest("Email already in use");
        }

        // Upload profile image if provided
        string? profileImageUrl = null;
        if (casterDto.ProfileImage != null)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(casterDto.ProfileImage.FileName)}";
            profileImageUrl = await storageService.UploadFileAsync(casterDto.ProfileImage, "profile-images", fileName);
        }

        var caster = new Caster
        {
            FirstName = casterDto.FirstName,
            LastName = casterDto.LastName,
            Age = casterDto.Age,
            City = casterDto.City,
            State = casterDto.State,
            Country = casterDto.Country,
            ZipCode = casterDto.ZipCode,
            Phone = casterDto.Phone,
            Email = casterDto.Email,
            HasTransportation = casterDto.HasTransportation,
            HasGimbal = casterDto.HasGimbal,
            Bio = casterDto.Bio,
            ProfileImageUrl = profileImageUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Casters.Add(caster);
        await db.SaveChangesAsync();

        return Results.Created($"/api/casters/{caster.Id}", MapToCasterDto(caster));
    }

    private static async Task<IResult> UpdateCaster(
        int id, 
        CasterDto casterDto, 
        IStorageService storageService,
        ApplicationDbContext db)
    {
        if (id != casterDto.Id)
        {
            return Results.BadRequest();
        }

        var caster = await db.Casters.FindAsync(id);
        if (caster is null)
        {
            return Results.NotFound();
        }

        // Check if email is being changed and if it's already in use
        if (caster.Email != casterDto.Email && await db.Casters.AnyAsync(c => c.Email == casterDto.Email))
        {
            return Results.BadRequest("Email already in use");
        }

        caster.FirstName = casterDto.FirstName;
        caster.LastName = casterDto.LastName;
        caster.Age = casterDto.Age;
        caster.City = casterDto.City;
        caster.State = casterDto.State;
        caster.Country = casterDto.Country;
        caster.ZipCode = casterDto.ZipCode;
        caster.Phone = casterDto.Phone;
        caster.Email = casterDto.Email;
        caster.HasTransportation = casterDto.HasTransportation;
        caster.HasGimbal = casterDto.HasGimbal;
        caster.Bio = casterDto.Bio;
        caster.ProfileImageUrl = casterDto.ProfileImageUrl;
        caster.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteCaster(
        int id, 
        IStorageService storageService,
        ApplicationDbContext db)
    {
        var caster = await db.Casters.FindAsync(id);
        if (caster is null)
        {
            return Results.NotFound();
        }

        // Delete profile image if exists
        if (!string.IsNullOrEmpty(caster.ProfileImageUrl))
        {
            await storageService.DeleteFileAsync(caster.ProfileImageUrl);
        }

        db.Casters.Remove(caster);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    
    private static CasterDto MapToCasterDto(Caster caster)
    {
        return new CasterDto
        {
            Id = caster.Id,
            FirstName = caster.FirstName,
            LastName = caster.LastName,
            Age = caster.Age,
            City = caster.City,
            State = caster.State,
            Country = caster.Country,
            ZipCode = caster.ZipCode,
            Phone = caster.Phone,
            Email = caster.Email,
            HasTransportation = caster.HasTransportation,
            HasGimbal = caster.HasGimbal,
            Bio = caster.Bio,
            ProfileImageUrl = caster.ProfileImageUrl,
            CreatedAt = caster.CreatedAt,
            UpdatedAt = caster.UpdatedAt
        };
    }
}

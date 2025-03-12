using api.Data;
using api.DTOs;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Endpoints;

public static class PhotoEndpoints
{
    public static void MapPhotoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/photos").WithTags("Photos");

        group.MapGet("/", GetAllPhotos);
        group.MapGet("/{id}", GetPhotoById);
        group.MapPost("/", CreatePhoto);
        group.MapDelete("/{id}", DeletePhoto);
        group.MapGet("/user/{userId}", GetPhotosByUserId);
    }

    private static async Task<IResult> GetAllPhotos(ApplicationDbContext db)
    {
        var photos = await db.Photos
            .Include(p => p.User)
            .ToListAsync();
            
        return Results.Ok(photos.Select(p => MapToPhotoDto(p)));
    }

    private static async Task<IResult> GetPhotoById(int id, ApplicationDbContext db)
    {
        var photo = await db.Photos
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == id);
            
        if (photo is null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(MapToPhotoDto(photo));
    }

    private static async Task<IResult> GetPhotosByUserId(int userId, ApplicationDbContext db)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
        {
            return Results.NotFound("User not found");
        }
        
        var photos = await db.Photos
            .Where(p => p.UserId == userId)
            .Include(p => p.User)
            .ToListAsync();
            
        return Results.Ok(photos.Select(p => MapToPhotoDto(p)));
    }

    private static async Task<IResult> CreatePhoto(
        PhotoCreateDto photoDto, 
        int userId, 
        IStorageService storageService,
        ApplicationDbContext db)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
        {
            return Results.NotFound("User not found");
        }

        // Upload photo file to S3
        var photoFileName = $"{Guid.NewGuid()}{Path.GetExtension(photoDto.PhotoFile.FileName)}";
        var photoUrl = await storageService.UploadFileAsync(photoDto.PhotoFile, "photos", photoFileName);

        var photo = new Photo
        {
            Title = photoDto.Title,
            Description = photoDto.Description,
            PhotoUrl = photoUrl,
            UserId = userId
        };

        db.Photos.Add(photo);
        await db.SaveChangesAsync();

        return Results.Created($"/api/photos/{photo.Id}", MapToPhotoDto(photo));
    }

    private static async Task<IResult> DeletePhoto(
        int id, 
        IStorageService storageService,
        ApplicationDbContext db)
    {
        var photo = await db.Photos.FindAsync(id);
        if (photo is null)
        {
            return Results.NotFound();
        }

        // Delete photo file from S3
        await storageService.DeleteFileAsync(photo.PhotoUrl);

        db.Photos.Remove(photo);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    
    private static PhotoDto MapToPhotoDto(Photo photo)
    {
        return new PhotoDto
        {
            Id = photo.Id,
            Title = photo.Title,
            Description = photo.Description,
            PhotoUrl = photo.PhotoUrl,
            UserId = photo.UserId,
            UserName = photo.User != null ? $"{photo.User.FirstName} {photo.User.LastName}" : string.Empty,
            CreatedAt = photo.CreatedAt,
            UpdatedAt = photo.UpdatedAt
        };
    }
}

using api.Data;
using api.DTOs;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Endpoints;

public static class VideoEndpoints
{
    public static void MapVideoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/videos").WithTags("Videos");

        group.MapGet("/", GetAllVideos);
        group.MapGet("/{id}", GetVideoById);
        group.MapPost("/", CreateVideo);
        group.MapPut("/{id}", UpdateVideo);
        group.MapDelete("/{id}", DeleteVideo);
        group.MapGet("/user/{userId}", GetVideosByUserId);
    }

    private static async Task<IResult> GetAllVideos(ApplicationDbContext db)
    {
        var videos = await db.Videos
            .Include(v => v.User)
            .ToListAsync();
            
        return Results.Ok(videos.Select(v => MapToVideoDto(v)));
    }

    private static async Task<IResult> GetVideoById(int id, ApplicationDbContext db)
    {
        var video = await db.Videos
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.Id == id);
            
        if (video is null)
        {
            return Results.NotFound();
        }
        
        // Increment view count
        video.ViewCount++;
        await db.SaveChangesAsync();
        
        return Results.Ok(MapToVideoDto(video));
    }

    private static async Task<IResult> GetVideosByUserId(int userId, ApplicationDbContext db)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
        {
            return Results.NotFound("User not found");
        }
        
        var videos = await db.Videos
            .Where(v => v.UserId == userId)
            .Include(v => v.User)
            .ToListAsync();
            
        return Results.Ok(videos.Select(v => MapToVideoDto(v)));
    }

    private static async Task<IResult> CreateVideo(
        VideoCreateDto videoDto, 
        int userId, 
        IStorageService storageService,
        ApplicationDbContext db)
    {
        var user = await db.Users.FindAsync(userId);
        if (user is null)
        {
            return Results.NotFound("User not found");
        }

        // Upload video file to S3
        var videoFileName = $"{Guid.NewGuid()}{Path.GetExtension(videoDto.VideoFile.FileName)}";
        var videoUrl = await storageService.UploadFileAsync(videoDto.VideoFile, "videos", videoFileName);
        
        // Upload thumbnail if provided
        string? thumbnailUrl = null;
        if (videoDto.ThumbnailFile != null)
        {
            var thumbnailFileName = $"{Guid.NewGuid()}{Path.GetExtension(videoDto.ThumbnailFile.FileName)}";
            thumbnailUrl = await storageService.UploadFileAsync(videoDto.ThumbnailFile, "thumbnails", thumbnailFileName);
        }

        var video = new Video
        {
            Title = videoDto.Title,
            Description = videoDto.Description,
            VideoUrl = videoUrl,
            ThumbnailUrl = thumbnailUrl,
            UserId = userId
        };

        db.Videos.Add(video);
        await db.SaveChangesAsync();

        return Results.Created($"/api/videos/{video.Id}", MapToVideoDto(video));
    }

    private static async Task<IResult> UpdateVideo(
        int id, 
        VideoUpdateDto videoDto, 
        ApplicationDbContext db)
    {
        var video = await db.Videos.FindAsync(id);
        if (video is null)
        {
            return Results.NotFound();
        }

        video.Title = videoDto.Title;
        video.Description = videoDto.Description;
        video.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteVideo(
        int id, 
        IStorageService storageService,
        ApplicationDbContext db)
    {
        var video = await db.Videos.FindAsync(id);
        if (video is null)
        {
            return Results.NotFound();
        }

        // Delete video file from S3
        await storageService.DeleteFileAsync(video.VideoUrl);
        
        // Delete thumbnail if exists
        if (!string.IsNullOrEmpty(video.ThumbnailUrl))
        {
            await storageService.DeleteFileAsync(video.ThumbnailUrl);
        }

        db.Videos.Remove(video);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    
    private static VideoDto MapToVideoDto(Video video)
    {
        return new VideoDto
        {
            Id = video.Id,
            Title = video.Title,
            Description = video.Description,
            VideoUrl = video.VideoUrl,
            ThumbnailUrl = video.ThumbnailUrl,
            UserId = video.UserId,
            UserName = video.User != null ? $"{video.User.FirstName} {video.User.LastName}" : string.Empty,
            ViewCount = video.ViewCount,
            LikeCount = video.LikeCount,
            CreatedAt = video.CreatedAt,
            UpdatedAt = video.UpdatedAt
        };
    }
}

using api.Data;
using api.DTOs;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users");

        group.MapGet("/", GetAllUsers);
        group.MapGet("/{id}", GetUserById);
        group.MapPost("/", CreateUser);
        group.MapPut("/{id}", UpdateUser);
        group.MapDelete("/{id}", DeleteUser);
        group.MapGet("/{id}/profile", GetUserProfile);
    }

    private static async Task<IResult> GetAllUsers(ApplicationDbContext db)
    {
        var users = await db.Users.ToListAsync();
        return Results.Ok(users.Select(u => MapToUserDto(u)));
    }

    private static async Task<IResult> GetUserById(int id, ApplicationDbContext db)
    {
        var user = await db.Users.FindAsync(id);
        
        if (user is null)
        {
            return Results.NotFound();
        }
        
        return Results.Ok(MapToUserDto(user));
    }

    private static async Task<IResult> GetUserProfile(int id, ApplicationDbContext db)
    {
        var user = await db.Users
            .Include(u => u.Videos)
            .Include(u => u.Photos)
            .FirstOrDefaultAsync(u => u.Id == id);
        
        if (user is null)
        {
            return Results.NotFound();
        }
        
        var profile = new UserProfileDto
        {
            User = MapToUserDto(user),
            Videos = user.Videos.Select(v => new VideoDto
            {
                Id = v.Id,
                Title = v.Title,
                Description = v.Description,
                VideoUrl = v.VideoUrl,
                ThumbnailUrl = v.ThumbnailUrl,
                UserId = v.UserId,
                UserName = $"{user.FirstName} {user.LastName}",
                ViewCount = v.ViewCount,
                LikeCount = v.LikeCount,
                CreatedAt = v.CreatedAt,
                UpdatedAt = v.UpdatedAt
            }).ToList(),
            Photos = user.Photos.Select(p => new PhotoDto
            {
                Id = p.Id,
                Title = p.Title,
                Description = p.Description,
                PhotoUrl = p.PhotoUrl,
                UserId = p.UserId,
                UserName = $"{user.FirstName} {user.LastName}",
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).ToList()
        };
        
        return Results.Ok(profile);
    }

    private static async Task<IResult> CreateUser(UserCreateDto userDto, ApplicationDbContext db)
    {
        // Check if email already exists
        if (await db.Users.AnyAsync(u => u.Email == userDto.Email))
        {
            return Results.BadRequest("Email already in use");
        }

        var user = new User
        {
            FirstName = userDto.FirstName,
            LastName = userDto.LastName,
            Age = userDto.Age,
            Rank = userDto.Rank,
            City = userDto.City,
            State = userDto.State,
            Zip = userDto.Zip,
            Phone = userDto.Phone,
            Email = userDto.Email,
            Bio = userDto.Bio,
            FacebookLink = userDto.FacebookLink,
            TwitterLink = userDto.TwitterLink,
            InstagramLink = userDto.InstagramLink,
            YoutubeLink = userDto.YoutubeLink,
            TikTokLink = userDto.TikTokLink,
            WebsiteLink = userDto.WebsiteLink
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return Results.Created($"/api/users/{user.Id}", MapToUserDto(user));
    }

    private static async Task<IResult> UpdateUser(int id, UserDto userDto, ApplicationDbContext db)
    {
        if (id != userDto.Id)
        {
            return Results.BadRequest();
        }

        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        // Check if email is being changed and if it's already in use
        if (user.Email != userDto.Email && await db.Users.AnyAsync(u => u.Email == userDto.Email))
        {
            return Results.BadRequest("Email already in use");
        }

        user.FirstName = userDto.FirstName;
        user.LastName = userDto.LastName;
        user.Age = userDto.Age;
        user.Rank = userDto.Rank;
        user.City = userDto.City;
        user.State = userDto.State;
        user.Zip = userDto.Zip;
        user.Phone = userDto.Phone;
        user.Email = userDto.Email;
        user.Bio = userDto.Bio;
        user.FacebookLink = userDto.FacebookLink;
        user.TwitterLink = userDto.TwitterLink;
        user.InstagramLink = userDto.InstagramLink;
        user.YoutubeLink = userDto.YoutubeLink;
        user.TikTokLink = userDto.TikTokLink;
        user.WebsiteLink = userDto.WebsiteLink;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> DeleteUser(int id, ApplicationDbContext db)
    {
        var user = await db.Users.FindAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        db.Users.Remove(user);
        await db.SaveChangesAsync();
        return Results.NoContent();
    }
    
    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Age = user.Age,
            Rank = user.Rank,
            City = user.City,
            State = user.State,
            Zip = user.Zip,
            Phone = user.Phone,
            Email = user.Email,
            Bio = user.Bio,
            FacebookLink = user.FacebookLink,
            TwitterLink = user.TwitterLink,
            InstagramLink = user.InstagramLink,
            YoutubeLink = user.YoutubeLink,
            TikTokLink = user.TikTokLink,
            WebsiteLink = user.WebsiteLink,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt
        };
    }
}

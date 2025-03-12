using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace api.DTOs;

public class VideoCreateDto
{
    [Required]
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public IFormFile VideoFile { get; set; } = null!;
    
    public IFormFile? ThumbnailFile { get; set; }
}

public class VideoUpdateDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

public class VideoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string VideoUrl { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

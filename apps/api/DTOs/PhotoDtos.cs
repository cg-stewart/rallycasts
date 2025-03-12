using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace api.DTOs;

public class PhotoCreateDto
{
    public string Title { get; set; } = string.Empty;
    
    public string Description { get; set; } = string.Empty;
    
    [Required]
    public IFormFile PhotoFile { get; set; } = null!;
}

public class PhotoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string PhotoUrl { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

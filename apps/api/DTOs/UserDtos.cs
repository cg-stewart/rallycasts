using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class UserCreateDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    public int Age { get; set; }
    
    public string Rank { get; set; } = string.Empty;
    
    public string City { get; set; } = string.Empty;
    
    public string State { get; set; } = string.Empty;
    
    public string Zip { get; set; } = string.Empty;
    
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    public string Bio { get; set; } = string.Empty;
    
    public string? FacebookLink { get; set; }
    
    public string? TwitterLink { get; set; }
    
    public string? InstagramLink { get; set; }
    
    public string? YoutubeLink { get; set; }
    
    public string? TikTokLink { get; set; }
    
    public string? WebsiteLink { get; set; }
}

public class UserDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string Rank { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Zip { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Bio { get; set; } = string.Empty;
    public string? FacebookLink { get; set; }
    public string? TwitterLink { get; set; }
    public string? InstagramLink { get; set; }
    public string? YoutubeLink { get; set; }
    public string? TikTokLink { get; set; }
    public string? WebsiteLink { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class UserProfileDto
{
    public UserDto User { get; set; } = null!;
    public List<VideoDto> Videos { get; set; } = new();
    public List<PhotoDto> Photos { get; set; } = new();
}

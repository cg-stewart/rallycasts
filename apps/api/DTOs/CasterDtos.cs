using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace api.DTOs;

public class CasterCreateDto
{
    [Required]
    public string FirstName { get; set; } = string.Empty;
    
    [Required]
    public string LastName { get; set; } = string.Empty;
    
    [Required]
    [Range(18, int.MaxValue, ErrorMessage = "Casters must be at least 18 years old")]
    public int Age { get; set; }
    
    [Required]
    public string City { get; set; } = string.Empty;
    
    [Required]
    public string State { get; set; } = string.Empty;
    
    public string Country { get; set; } = string.Empty;
    
    [Required]
    public string ZipCode { get; set; } = string.Empty;
    
    [Required]
    public string Phone { get; set; } = string.Empty;
    
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    public string Password { get; set; } = string.Empty;
    
    [Required]
    public bool HasTransportation { get; set; }
    
    public bool HasGimbal { get; set; }
    
    public string? Bio { get; set; }
    
    public IFormFile? ProfileImage { get; set; }
}

public class CasterDto
{
    public int Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public int Age { get; set; }
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool HasTransportation { get; set; }
    public bool HasGimbal { get; set; }
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

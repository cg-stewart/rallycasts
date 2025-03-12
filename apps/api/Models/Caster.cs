using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a caster (videographer) in the Rallycasts application
/// </summary>
public class Caster
{
    /// <summary>
    /// The unique identifier for the caster
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The caster's first name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's age
    /// </summary>
    [Required]
    [Range(18, 120, ErrorMessage = "Casters must be at least 18 years old")]
    public int Age { get; set; }
    
    /// <summary>
    /// The caster's city
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's state/province
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's country
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Country { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's postal/zip code
    /// </summary>
    [Required]
    [MaxLength(20)]
    public string ZipCode { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's phone number
    /// </summary>
    [Required]
    [MaxLength(20)]
    [Phone]
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's email address
    /// </summary>
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// The caster's password hash
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates whether the caster has transportation
    /// </summary>
    public bool HasTransportation { get; set; }
    
    /// <summary>
    /// Indicates whether the caster has a gimbal
    /// </summary>
    public bool HasGimbal { get; set; }
    
    /// <summary>
    /// The caster's biography
    /// </summary>
    [MaxLength(1000)]
    public string? Bio { get; set; }
    
    /// <summary>
    /// The URL to the caster's profile image
    /// </summary>
    [MaxLength(500)]
    [Url]
    public string? ProfileImageUrl { get; set; }
    
    /// <summary>
    /// The caster's hourly rate
    /// </summary>
    [Range(0, 1000)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal HourlyRate { get; set; } = 0;
    
    /// <summary>
    /// The caster's experience level (years)
    /// </summary>
    [Range(0, 50)]
    public int ExperienceYears { get; set; } = 0;
    
    /// <summary>
    /// Indicates whether the caster is currently available for booking
    /// </summary>
    public bool IsAvailable { get; set; } = true;
    
    /// <summary>
    /// The date and time when the caster was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the caster was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the full name of the caster
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a photo in the Rallycasts application
/// </summary>
public class Photo
{
    /// <summary>
    /// The unique identifier for the photo
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The title of the photo
    /// </summary>
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The description of the photo
    /// </summary>
    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The URL to the photo file
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Url]
    public string PhotoUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the user who uploaded the photo
    /// </summary>
    [Required]
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    
    /// <summary>
    /// The user who uploaded the photo
    /// </summary>
    public virtual User? User { get; set; }
    
    /// <summary>
    /// Tags associated with the photo for search and categorization
    /// </summary>
    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates whether the photo is public or private
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    /// The number of likes the photo has received
    /// </summary>
    public int LikeCount { get; set; } = 0;
    
    /// <summary>
    /// The date and time when the photo was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the photo was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
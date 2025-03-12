using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a video in the Rallycasts application
/// </summary>
public class Video
{
    /// <summary>
    /// The unique identifier for the video
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The title of the video
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The description of the video
    /// </summary>
    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// The URL to the video file
    /// </summary>
    [Required]
    [MaxLength(500)]
    [Url]
    public string VideoUrl { get; set; } = string.Empty;
    
    /// <summary>
    /// The URL to the video thumbnail image
    /// </summary>
    [MaxLength(500)]
    [Url]
    public string? ThumbnailUrl { get; set; }
    
    /// <summary>
    /// The duration of the video in seconds
    /// </summary>
    public int? DurationSeconds { get; set; }
    
    /// <summary>
    /// The ID of the user who uploaded the video
    /// </summary>
    [Required]
    [ForeignKey(nameof(User))]
    public int UserId { get; set; }
    
    /// <summary>
    /// The user who uploaded the video
    /// </summary>
    public virtual User? User { get; set; }
    
    /// <summary>
    /// The number of times the video has been viewed
    /// </summary>
    public int ViewCount { get; set; } = 0;
    
    /// <summary>
    /// The number of likes the video has received
    /// </summary>
    public int LikeCount { get; set; } = 0;
    
    /// <summary>
    /// Tags associated with the video for search and categorization
    /// </summary>
    [MaxLength(500)]
    public string Tags { get; set; } = string.Empty;
    
    /// <summary>
    /// Indicates whether the video is public or private
    /// </summary>
    public bool IsPublic { get; set; } = true;
    
    /// <summary>
    /// The date and time when the video was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the video was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the formatted duration of the video (MM:SS)
    /// </summary>
    [NotMapped]
    public string FormattedDuration
    {
        get
        {
            if (!DurationSeconds.HasValue) return "00:00";
            var minutes = DurationSeconds.Value / 60;
            var seconds = DurationSeconds.Value % 60;
            return $"{minutes:D2}:{seconds:D2}";
        }
    }
}
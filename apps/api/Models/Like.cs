using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a like on a video or photo
/// </summary>
public class Like
{
    /// <summary>
    /// The unique identifier for the like
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the user who created the like
    /// </summary>
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    /// <summary>
    /// The user who created the like
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// The ID of the video being liked (null if liking a photo)
    /// </summary>
    [ForeignKey("Video")]
    public int? VideoId { get; set; }
    
    /// <summary>
    /// The video being liked (null if liking a photo)
    /// </summary>
    public virtual Video? Video { get; set; }
    
    /// <summary>
    /// The ID of the photo being liked (null if liking a video)
    /// </summary>
    [ForeignKey("Photo")]
    public int? PhotoId { get; set; }
    
    /// <summary>
    /// The photo being liked (null if liking a video)
    /// </summary>
    public virtual Photo? Photo { get; set; }
    
    /// <summary>
    /// The date and time when the like was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

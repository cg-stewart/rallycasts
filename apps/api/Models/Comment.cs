using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a comment on a video or photo
/// </summary>
public class Comment
{
    /// <summary>
    /// The unique identifier for the comment
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The content of the comment
    /// </summary>
    [Required]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the user who created the comment
    /// </summary>
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    /// <summary>
    /// The user who created the comment
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// The ID of the video being commented on (null if commenting on a photo)
    /// </summary>
    [ForeignKey("Video")]
    public int? VideoId { get; set; }
    
    /// <summary>
    /// The video being commented on (null if commenting on a photo)
    /// </summary>
    public virtual Video? Video { get; set; }
    
    /// <summary>
    /// The ID of the photo being commented on (null if commenting on a video)
    /// </summary>
    [ForeignKey("Photo")]
    public int? PhotoId { get; set; }
    
    /// <summary>
    /// The photo being commented on (null if commenting on a video)
    /// </summary>
    public virtual Photo? Photo { get; set; }
    
    /// <summary>
    /// The ID of the parent comment if this is a reply (null if it's a top-level comment)
    /// </summary>
    [ForeignKey("ParentComment")]
    public int? ParentCommentId { get; set; }
    
    /// <summary>
    /// The parent comment if this is a reply (null if it's a top-level comment)
    /// </summary>
    public virtual Comment? ParentComment { get; set; }
    
    /// <summary>
    /// The replies to this comment
    /// </summary>
    public virtual ICollection<Comment> Replies { get; set; } = new List<Comment>();
    
    /// <summary>
    /// The date and time when the comment was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the comment was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

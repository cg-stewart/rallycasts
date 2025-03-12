using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a notification for a user
/// </summary>
public class Notification
{
    /// <summary>
    /// The unique identifier for the notification
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the user who will receive the notification
    /// </summary>
    [Required]
    [ForeignKey("User")]
    public int UserId { get; set; }
    
    /// <summary>
    /// The user who will receive the notification
    /// </summary>
    public virtual User User { get; set; } = null!;
    
    /// <summary>
    /// The type of notification (e.g., "like", "comment", "follow", "message")
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// The title of the notification
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Title { get; set; } = string.Empty;
    
    /// <summary>
    /// The content of the notification
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the user who triggered the notification (if applicable)
    /// </summary>
    [ForeignKey("Sender")]
    public int? SenderId { get; set; }
    
    /// <summary>
    /// The user who triggered the notification (if applicable)
    /// </summary>
    public virtual User? Sender { get; set; }
    
    /// <summary>
    /// The URL to redirect to when the notification is clicked
    /// </summary>
    [MaxLength(500)]
    public string? RedirectUrl { get; set; }
    
    /// <summary>
    /// Indicates whether the notification has been read
    /// </summary>
    public bool IsRead { get; set; } = false;
    
    /// <summary>
    /// The date and time when the notification was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the notification was read (if it has been read)
    /// </summary>
    public DateTime? ReadAt { get; set; }
}

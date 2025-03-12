using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a direct message between users
/// </summary>
public class Message
{
    /// <summary>
    /// The unique identifier for the message
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The content of the message
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the user who sent the message
    /// </summary>
    [Required]
    [ForeignKey("Sender")]
    public int SenderId { get; set; }
    
    /// <summary>
    /// The user who sent the message
    /// </summary>
    public virtual User Sender { get; set; } = null!;
    
    /// <summary>
    /// The ID of the user who received the message
    /// </summary>
    [Required]
    [ForeignKey("Recipient")]
    public int RecipientId { get; set; }
    
    /// <summary>
    /// The user who received the message
    /// </summary>
    public virtual User Recipient { get; set; } = null!;
    
    /// <summary>
    /// Indicates whether the message has been read by the recipient
    /// </summary>
    public bool IsRead { get; set; } = false;
    
    /// <summary>
    /// The date and time when the message was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the message was read (if it has been read)
    /// </summary>
    public DateTime? ReadAt { get; set; }
}

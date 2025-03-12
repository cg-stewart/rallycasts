using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a follow relationship between users
/// </summary>
public class UserFollow
{
    /// <summary>
    /// The unique identifier for the follow relationship
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The ID of the user who is following
    /// </summary>
    [Required]
    [ForeignKey("Follower")]
    public int FollowerId { get; set; }
    
    /// <summary>
    /// The ID of the user being followed
    /// </summary>
    [Required]
    [ForeignKey("Following")]
    public int FollowingId { get; set; }
    
    /// <summary>
    /// The user who is following
    /// </summary>
    public virtual User Follower { get; set; } = null!;
    
    /// <summary>
    /// The user being followed
    /// </summary>
    public virtual User Following { get; set; } = null!;
    
    /// <summary>
    /// The date and time when the follow relationship was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

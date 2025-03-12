using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

/// <summary>
/// Represents a user in the Rallycasts application
/// </summary>
public class User
{
    /// <summary>
    /// The unique identifier for the user
    /// </summary>
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// The user's first name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string FirstName { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's last name
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string LastName { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's age
    /// </summary>
    [Range(0, 120)]
    public int Age { get; set; }
    
    /// <summary>
    /// The user's pickleball rank/skill level
    /// </summary>
    [MaxLength(20)]
    public string Rank { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's city
    /// </summary>
    [MaxLength(100)]
    public string City { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's state/province
    /// </summary>
    [MaxLength(50)]
    public string State { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's postal/zip code
    /// </summary>
    [MaxLength(20)]
    public string Zip { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's phone number
    /// </summary>
    [MaxLength(20)]
    [Phone]
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's email address
    /// </summary>
    [Required]
    [MaxLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's AWS Cognito sub (subject) identifier
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string CognitoSub { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's biography
    /// </summary>
    [MaxLength(1000)]
    public string Bio { get; set; } = string.Empty;
    
    /// <summary>
    /// The user's Facebook profile link
    /// </summary>
    [MaxLength(255)]
    [Url]
    public string? FacebookLink { get; set; }
    
    /// <summary>
    /// The user's Twitter/X profile link
    /// </summary>
    [MaxLength(255)]
    [Url]
    public string? TwitterLink { get; set; }
    
    /// <summary>
    /// The user's Instagram profile link
    /// </summary>
    [MaxLength(255)]
    [Url]
    public string? InstagramLink { get; set; }
    
    /// <summary>
    /// The user's YouTube channel link
    /// </summary>
    [MaxLength(255)]
    [Url]
    public string? YoutubeLink { get; set; }
    
    /// <summary>
    /// The user's TikTok profile link
    /// </summary>
    [MaxLength(255)]
    [Url]
    public string? TikTokLink { get; set; }
    
    /// <summary>
    /// The user's personal website link
    /// </summary>
    [MaxLength(255)]
    [Url]
    public string? WebsiteLink { get; set; }
    
    /// <summary>
    /// The user's profile picture URL
    /// </summary>
    [MaxLength(500)]
    [Url]
    public string? ProfilePictureUrl { get; set; }
    
    /// <summary>
    /// Collection of videos uploaded by the user
    /// </summary>
    public virtual ICollection<Video> Videos { get; set; } = new List<Video>();
    
    /// <summary>
    /// Collection of photos uploaded by the user
    /// </summary>
    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
    
    /// <summary>
    /// Collection of users that this user is following
    /// </summary>
    [InverseProperty("Follower")]
    public virtual ICollection<UserFollow> Following { get; set; } = new List<UserFollow>();
    
    /// <summary>
    /// Collection of users that are following this user
    /// </summary>
    [InverseProperty("Following")]
    public virtual ICollection<UserFollow> Followers { get; set; } = new List<UserFollow>();
    
    /// <summary>
    /// Collection of likes created by this user
    /// </summary>
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    
    /// <summary>
    /// Collection of comments created by this user
    /// </summary>
    public virtual ICollection<Comment> Comments { get; set; } = new List<Comment>();
    
    /// <summary>
    /// Collection of messages sent by this user
    /// </summary>
    [InverseProperty("Sender")]
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    
    /// <summary>
    /// Collection of messages received by this user
    /// </summary>
    [InverseProperty("Recipient")]
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    
    /// <summary>
    /// Collection of notifications for this user
    /// </summary>
    [InverseProperty("User")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    
    /// <summary>
    /// The date and time when the user was created
    /// </summary>
    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the user was last updated
    /// </summary>
    [Required]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Gets the full name of the user
    /// </summary>
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
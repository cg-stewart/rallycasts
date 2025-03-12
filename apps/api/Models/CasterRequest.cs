using System.ComponentModel.DataAnnotations;
using Amazon.DynamoDBv2.DataModel;

namespace api.Models;

/// <summary>
/// Represents a request for a caster (videographer) in the Rallycasts application
/// </summary>
[DynamoDBTable("CasterRequests")]
public class CasterRequest
{
    /// <summary>
    /// The unique identifier for the caster request
    /// </summary>
    [DynamoDBHashKey]
    [Required]
    public string Id { get; set; } = string.Empty;
    
    /// <summary>
    /// The name of the event for which a caster is requested
    /// </summary>
    [Required]
    [DynamoDBProperty]
    [StringLength(100, MinimumLength = 3)]
    public string EventName { get; set; } = string.Empty;
    
    /// <summary>
    /// The location of the event
    /// </summary>
    [Required]
    [DynamoDBProperty]
    [StringLength(200)]
    public string Location { get; set; } = string.Empty;
    
    /// <summary>
    /// The date and time of the event
    /// </summary>
    [Required]
    [DynamoDBProperty]
    public DateTime EventDate { get; set; }
    
    /// <summary>
    /// A description of the event
    /// </summary>
    [DynamoDBProperty]
    [StringLength(2000)]
    public string EventDescription { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the user who made the request
    /// </summary>
    [Required]
    [DynamoDBProperty]
    public int RequesterId { get; set; }
    
    /// <summary>
    /// The name of the user who made the request
    /// </summary>
    [Required]
    [DynamoDBProperty]
    [StringLength(100)]
    public string RequesterName { get; set; } = string.Empty;
    
    /// <summary>
    /// The ID of the caster assigned to the request (if any)
    /// </summary>
    [DynamoDBProperty]
    public int? CasterId { get; set; }
    
    /// <summary>
    /// The name of the caster assigned to the request (if any)
    /// </summary>
    [DynamoDBProperty]
    [StringLength(100)]
    public string? CasterName { get; set; }
    
    /// <summary>
    /// The current status of the request (Pending, Accepted, Rejected, Completed, Cancelled)
    /// </summary>
    [Required]
    [DynamoDBProperty]
    [StringLength(20)]
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// The price agreed for the caster service
    /// </summary>
    [DynamoDBProperty]
    [Range(0, 10000)]
    public decimal Price { get; set; }
    
    /// <summary>
    /// The duration of the event in hours
    /// </summary>
    [DynamoDBProperty]
    [Range(0, 24)]
    public decimal DurationHours { get; set; } = 1;
    
    /// <summary>
    /// Any special requirements for the event
    /// </summary>
    [DynamoDBProperty]
    [StringLength(1000)]
    public string? SpecialRequirements { get; set; }
    
    /// <summary>
    /// The date and time when the request was created
    /// </summary>
    [Required]
    [DynamoDBProperty]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// The date and time when the request was last updated
    /// </summary>
    [Required]
    [DynamoDBProperty]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
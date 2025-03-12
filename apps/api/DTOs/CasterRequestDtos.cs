using System.ComponentModel.DataAnnotations;

namespace api.DTOs;

public class CasterRequestCreateDto
{
    [Required]
    public string EventName { get; set; } = string.Empty;
    
    [Required]
    public string Location { get; set; } = string.Empty;
    
    [Required]
    public DateTime EventDate { get; set; }
    
    public string EventDescription { get; set; } = string.Empty;
    
    public int? CasterId { get; set; }
    
    public decimal Price { get; set; }
}

public class CasterRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string EventName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    public string EventDescription { get; set; } = string.Empty;
    public int RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public int? CasterId { get; set; }
    public string? CasterName { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CasterRequestUpdateDto
{
    public string Status { get; set; } = string.Empty;
    public decimal? Price { get; set; }
}

namespace api.Models;

/// <summary>
/// Standard error response model for API errors
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// HTTP status code
    /// </summary>
    public int StatusCode { get; set; }
    
    /// <summary>
    /// Error message
    /// </summary>
    public string Message { get; set; } = string.Empty;
    
    /// <summary>
    /// Stack trace (only included in development environment)
    /// </summary>
    public string? StackTrace { get; set; }
    
    /// <summary>
    /// Additional error details if available
    /// </summary>
    public object? Details { get; set; }
}

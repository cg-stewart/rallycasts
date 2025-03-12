using System.Runtime.Serialization;

namespace api.Models;

/// <summary>
/// Base exception for API-specific exceptions
/// </summary>
[Serializable]
public class ApiException : Exception
{
    public ApiException() { }
    public ApiException(string message) : base(message) { }
    public ApiException(string message, Exception inner) : base(message, inner) { }
    protected ApiException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when a requested resource is not found
/// </summary>
[Serializable]
public class ResourceNotFoundException : ApiException
{
    public ResourceNotFoundException() : base("The requested resource was not found") { }
    public ResourceNotFoundException(string message) : base(message) { }
    public ResourceNotFoundException(string message, Exception inner) : base(message, inner) { }
    public ResourceNotFoundException(string resourceType, object resourceId) 
        : base($"{resourceType} with ID {resourceId} was not found") { }
    protected ResourceNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when the request is invalid
/// </summary>
[Serializable]
public class BadRequestException : ApiException
{
    public BadRequestException() : base("The request is invalid") { }
    public BadRequestException(string message) : base(message) { }
    public BadRequestException(string message, Exception inner) : base(message, inner) { }
    protected BadRequestException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when the user is not authorized to perform the requested action
/// </summary>
[Serializable]
public class UnauthorizedException : ApiException
{
    public UnauthorizedException() : base("You are not authorized to perform this action") { }
    public UnauthorizedException(string message) : base(message) { }
    public UnauthorizedException(string message, Exception inner) : base(message, inner) { }
    protected UnauthorizedException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when the user is authenticated but forbidden from accessing a resource
/// </summary>
[Serializable]
public class ForbiddenException : ApiException
{
    public ForbiddenException() : base("You do not have permission to access this resource") { }
    public ForbiddenException(string message) : base(message) { }
    public ForbiddenException(string message, Exception inner) : base(message, inner) { }
    protected ForbiddenException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when there is a conflict with the current state of the resource
/// </summary>
[Serializable]
public class ConflictException : ApiException
{
    public ConflictException() : base("There is a conflict with the current state of the resource") { }
    public ConflictException(string message) : base(message) { }
    public ConflictException(string message, Exception inner) : base(message, inner) { }
    protected ConflictException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

/// <summary>
/// Exception thrown when a rate limit has been exceeded
/// </summary>
[Serializable]
public class RateLimitExceededException : ApiException
{
    public RateLimitExceededException() : base("Rate limit exceeded. Please try again later") { }
    public RateLimitExceededException(string message) : base(message) { }
    public RateLimitExceededException(string message, Exception inner) : base(message, inner) { }
    protected RateLimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}

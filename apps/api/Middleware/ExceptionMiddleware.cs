using System.Net;
using System.Text.Json;
using api.Models;

namespace api.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;
    private readonly IHostEnvironment _env;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger, IHostEnvironment env)
    {
        _next = next;
        _logger = logger;
        _env = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred during request execution");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        
        var statusCode = HttpStatusCode.InternalServerError;
        var errorMessage = "An unexpected error occurred";
        var stackTrace = string.Empty;

        // Determine the status code and error message based on exception type
        if (exception is ResourceNotFoundException)
        {
            statusCode = HttpStatusCode.NotFound;
            errorMessage = exception.Message;
        }
        else if (exception is BadRequestException)
        {
            statusCode = HttpStatusCode.BadRequest;
            errorMessage = exception.Message;
        }
        else if (exception is UnauthorizedException)
        {
            statusCode = HttpStatusCode.Unauthorized;
            errorMessage = exception.Message;
        }
        else if (exception is ForbiddenException)
        {
            statusCode = HttpStatusCode.Forbidden;
            errorMessage = exception.Message;
        }
        else if (exception is ConflictException)
        {
            statusCode = HttpStatusCode.Conflict;
            errorMessage = exception.Message;
        }

        // Include stack trace in development environment
        if (_env.IsDevelopment())
        {
            stackTrace = exception.StackTrace ?? string.Empty;
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new ErrorResponse
        {
            StatusCode = (int)statusCode,
            Message = errorMessage,
            StackTrace = stackTrace
        };

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var json = JsonSerializer.Serialize(response, options);
        await context.Response.WriteAsync(json);
    }
}

// Extension method to add the middleware to the HTTP request pipeline
public static class ExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseExceptionMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<ExceptionMiddleware>();
    }
}

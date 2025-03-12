using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationService _notificationService;
    private readonly ILogger<NotificationController> _logger;

    public NotificationController(
        ApplicationDbContext dbContext,
        NotificationService notificationService,
        ILogger<NotificationController> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool unreadOnly = false)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Get notifications
            var query = _dbContext.Notifications
                .Where(n => n.UserId == currentUserId);

            // Filter by unread if requested
            if (unreadOnly)
            {
                query = query.Where(n => !n.IsRead);
            }

            // Order by creation date
            query = query.OrderByDescending(n => n.CreatedAt);

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var notifications = await query
                .Include(n => n.Sender)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(n => new
                {
                    n.Id,
                    n.Type,
                    n.Title,
                    n.Content,
                    n.SenderId,
                    Sender = n.Sender != null ? new
                    {
                        n.Sender.Id,
                        n.Sender.FirstName,
                        n.Sender.LastName,
                        n.Sender.FullName,
                        n.Sender.ProfilePictureUrl
                    } : null,
                    n.RedirectUrl,
                    n.IsRead,
                    n.CreatedAt,
                    n.ReadAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Notifications = notifications
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting notifications");
            return StatusCode(500, new { Message = "An error occurred while getting notifications" });
        }
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Count unread notifications
            int unreadCount = await _dbContext.Notifications
                .CountAsync(n => n.UserId == currentUserId && !n.IsRead);

            return Ok(new { UnreadCount = unreadCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread notification count");
            return StatusCode(500, new { Message = "An error occurred while getting unread notification count" });
        }
    }

    [HttpPost("{id}/mark-read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find notification
            var notification = await _dbContext.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound(new { Message = "Notification not found" });
            }

            // Check if user is the notification owner
            if (notification.UserId != currentUserId)
            {
                return Forbid();
            }

            // Mark as read if not already
            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { Message = "Notification marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification as read");
            return StatusCode(500, new { Message = "An error occurred while marking notification as read" });
        }
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find unread notifications
            var unreadNotifications = await _dbContext.Notifications
                .Where(n => n.UserId == currentUserId && !n.IsRead)
                .ToListAsync();

            // Mark all as read
            foreach (var notification in unreadNotifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "All notifications marked as read", Count = unreadNotifications.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications as read");
            return StatusCode(500, new { Message = "An error occurred while marking all notifications as read" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteNotification(int id)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find notification
            var notification = await _dbContext.Notifications.FindAsync(id);
            if (notification == null)
            {
                return NotFound(new { Message = "Notification not found" });
            }

            // Check if user is the notification owner
            if (notification.UserId != currentUserId)
            {
                return Forbid();
            }

            // Delete notification
            _dbContext.Notifications.Remove(notification);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Notification deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting notification");
            return StatusCode(500, new { Message = "An error occurred while deleting notification" });
        }
    }

    [HttpDelete("clear-all")]
    public async Task<IActionResult> ClearAllNotifications()
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find user's notifications
            var notifications = await _dbContext.Notifications
                .Where(n => n.UserId == currentUserId)
                .ToListAsync();

            // Delete all notifications
            _dbContext.Notifications.RemoveRange(notifications);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "All notifications cleared successfully", Count = notifications.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error clearing all notifications");
            return StatusCode(500, new { Message = "An error occurred while clearing all notifications" });
        }
    }

    [HttpPost("register-device")]
    public async Task<IActionResult> RegisterDevice([FromBody] RegisterDeviceRequest request)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Validate request
            if (string.IsNullOrWhiteSpace(request.DeviceToken))
            {
                return BadRequest(new { Message = "Device token is required" });
            }

            if (string.IsNullOrWhiteSpace(request.Platform) || (request.Platform != "ios" && request.Platform != "android"))
            {
                return BadRequest(new { Message = "Platform must be 'ios' or 'android'" });
            }

            // Get platform application ARN based on platform
            string platformApplicationArn = request.Platform == "ios"
                ? _notificationService._settings.IosPlatformApplicationArn
                : _notificationService._settings.AndroidPlatformApplicationArn;

            // Create platform endpoint
            string endpointArn = await _notificationService.CreatePlatformEndpointAsync(
                platformApplicationArn,
                request.DeviceToken,
                currentUserId.ToString());

            // Subscribe endpoint to topic
            await _notificationService.SubscribeEndpointToTopicAsync(endpointArn);

            return Ok(new { Message = "Device registered successfully", EndpointArn = endpointArn });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device");
            return StatusCode(500, new { Message = "An error occurred while registering device" });
        }
    }
}

// Request models
public class RegisterDeviceRequest
{
    public string DeviceToken { get; set; } = string.Empty;
    public string Platform { get; set; } = string.Empty; // "ios" or "android"
}

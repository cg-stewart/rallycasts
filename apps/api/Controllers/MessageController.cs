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
public class MessageController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationService _notificationService;
    private readonly ILogger<MessageController> _logger;

    public MessageController(
        ApplicationDbContext dbContext,
        NotificationService notificationService,
        ILogger<MessageController> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest request)
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
            if (string.IsNullOrWhiteSpace(request.Content))
            {
                return BadRequest(new { Message = "Message content is required" });
            }

            // Check if recipient exists
            var recipient = await _dbContext.Users.FindAsync(request.RecipientId);
            if (recipient == null)
            {
                return NotFound(new { Message = "Recipient not found" });
            }

            // Create message
            var message = new Message
            {
                SenderId = currentUserId,
                RecipientId = request.RecipientId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Messages.Add(message);
            await _dbContext.SaveChangesAsync();

            // Create notification for recipient
            var sender = await _dbContext.Users.FindAsync(currentUserId);
            var notification = new Notification
            {
                UserId = request.RecipientId,
                Type = "message",
                Title = "New Message",
                Content = $"{sender?.FullName} sent you a message",
                SenderId = currentUserId,
                RedirectUrl = $"/messages/{currentUserId}",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();

            // Send notification
            await _notificationService.CreateAndSendNotificationAsync(
                notification,
                sendEmail: false,
                sendPush: true);

            // Return message with sender info
            var messageResponse = new
            {
                message.Id,
                message.SenderId,
                Sender = new
                {
                    sender?.Id,
                    sender?.FirstName,
                    sender?.LastName,
                    sender?.FullName,
                    sender?.ProfilePictureUrl
                },
                message.RecipientId,
                Recipient = new
                {
                    recipient.Id,
                    recipient.FirstName,
                    recipient.LastName,
                    recipient.FullName,
                    recipient.ProfilePictureUrl
                },
                message.Content,
                message.IsRead,
                message.ReadAt,
                message.CreatedAt
            };

            return Ok(messageResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            return StatusCode(500, new { Message = "An error occurred while sending message" });
        }
    }

    [HttpGet("conversations")]
    public async Task<IActionResult> GetConversations([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Get latest message from each conversation
            var latestMessages = await _dbContext.Messages
                .Where(m => m.SenderId == currentUserId || m.RecipientId == currentUserId)
                .GroupBy(m => m.SenderId == currentUserId ? m.RecipientId : m.SenderId)
                .Select(g => g.OrderByDescending(m => m.CreatedAt).First())
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Get total count
            var totalCount = await _dbContext.Messages
                .Where(m => m.SenderId == currentUserId || m.RecipientId == currentUserId)
                .GroupBy(m => m.SenderId == currentUserId ? m.RecipientId : m.SenderId)
                .CountAsync();

            // Get user details for each conversation
            var conversations = new List<object>();
            foreach (var message in latestMessages)
            {
                int otherUserId = message.SenderId == currentUserId ? message.RecipientId : message.SenderId;
                var otherUser = await _dbContext.Users.FindAsync(otherUserId);

                // Count unread messages
                int unreadCount = await _dbContext.Messages
                    .CountAsync(m => m.RecipientId == currentUserId && m.SenderId == otherUserId && !m.IsRead);

                conversations.Add(new
                {
                    UserId = otherUserId,
                    User = new
                    {
                        otherUser?.Id,
                        otherUser?.FirstName,
                        otherUser?.LastName,
                        otherUser?.FullName,
                        otherUser?.ProfilePictureUrl
                    },
                    LastMessage = new
                    {
                        message.Id,
                        message.SenderId,
                        message.RecipientId,
                        message.Content,
                        message.IsRead,
                        message.ReadAt,
                        message.CreatedAt,
                        IsSent = message.SenderId == currentUserId
                    },
                    UnreadCount = unreadCount
                });
            }

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Conversations = conversations
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversations");
            return StatusCode(500, new { Message = "An error occurred while getting conversations" });
        }
    }

    [HttpGet("conversation/{userId}")]
    public async Task<IActionResult> GetConversation(int userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Check if other user exists
            var otherUser = await _dbContext.Users.FindAsync(userId);
            if (otherUser == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Get messages between the two users
            var query = _dbContext.Messages
                .Where(m => (m.SenderId == currentUserId && m.RecipientId == userId) ||
                           (m.SenderId == userId && m.RecipientId == currentUserId))
                .OrderByDescending(m => m.CreatedAt);

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var messages = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Mark unread messages as read
            var unreadMessages = messages
                .Where(m => m.RecipientId == currentUserId && !m.IsRead)
                .ToList();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.IsRead = true;
                    message.ReadAt = DateTime.UtcNow;
                }

                await _dbContext.SaveChangesAsync();
            }

            // Get current user
            var currentUser = await _dbContext.Users.FindAsync(currentUserId);

            // Format messages
            var formattedMessages = messages.Select(m => new
            {
                m.Id,
                m.SenderId,
                Sender = m.SenderId == currentUserId
                    ? new
                    {
                        currentUser?.Id,
                        currentUser?.FirstName,
                        currentUser?.LastName,
                        currentUser?.FullName,
                        currentUser?.ProfilePictureUrl
                    }
                    : new
                    {
                        otherUser.Id,
                        otherUser.FirstName,
                        otherUser.LastName,
                        otherUser.FullName,
                        otherUser.ProfilePictureUrl
                    },
                m.RecipientId,
                Recipient = m.RecipientId == currentUserId
                    ? new
                    {
                        currentUser?.Id,
                        currentUser?.FirstName,
                        currentUser?.LastName,
                        currentUser?.FullName,
                        currentUser?.ProfilePictureUrl
                    }
                    : new
                    {
                        otherUser.Id,
                        otherUser.FirstName,
                        otherUser.LastName,
                        otherUser.FullName,
                        otherUser.ProfilePictureUrl
                    },
                m.Content,
                m.IsRead,
                m.ReadAt,
                m.CreatedAt,
                IsSent = m.SenderId == currentUserId
            }).ToList();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Messages = formattedMessages,
                OtherUser = new
                {
                    otherUser.Id,
                    otherUser.FirstName,
                    otherUser.LastName,
                    otherUser.FullName,
                    otherUser.ProfilePictureUrl
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting conversation");
            return StatusCode(500, new { Message = "An error occurred while getting conversation" });
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

            // Count unread messages
            int unreadCount = await _dbContext.Messages
                .CountAsync(m => m.RecipientId == currentUserId && !m.IsRead);

            return Ok(new { UnreadCount = unreadCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count");
            return StatusCode(500, new { Message = "An error occurred while getting unread count" });
        }
    }

    [HttpPost("mark-read/{messageId}")]
    public async Task<IActionResult> MarkAsRead(int messageId)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find message
            var message = await _dbContext.Messages.FindAsync(messageId);
            if (message == null)
            {
                return NotFound(new { Message = "Message not found" });
            }

            // Check if user is the recipient
            if (message.RecipientId != currentUserId)
            {
                return Forbid();
            }

            // Mark as read if not already
            if (!message.IsRead)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
                await _dbContext.SaveChangesAsync();
            }

            return Ok(new { Message = "Message marked as read" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking message as read");
            return StatusCode(500, new { Message = "An error occurred while marking message as read" });
        }
    }

    [HttpPost("mark-all-read/{userId}")]
    public async Task<IActionResult> MarkAllAsRead(int userId)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find unread messages from the specified user
            var unreadMessages = await _dbContext.Messages
                .Where(m => m.SenderId == userId && m.RecipientId == currentUserId && !m.IsRead)
                .ToListAsync();

            // Mark all as read
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
                message.ReadAt = DateTime.UtcNow;
            }

            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "All messages marked as read", Count = unreadMessages.Count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all messages as read");
            return StatusCode(500, new { Message = "An error occurred while marking all messages as read" });
        }
    }
}

// Request models
public class SendMessageRequest
{
    public int RecipientId { get; set; }
    public string Content { get; set; } = string.Empty;
}

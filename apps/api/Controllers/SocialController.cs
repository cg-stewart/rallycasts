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
public class SocialController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationService _notificationService;
    private readonly ILogger<SocialController> _logger;

    public SocialController(
        ApplicationDbContext dbContext,
        NotificationService notificationService,
        ILogger<SocialController> logger)
    {
        _dbContext = dbContext;
        _notificationService = notificationService;
        _logger = logger;
    }

    #region Follow

    [HttpPost("follow/{userId}")]
    public async Task<IActionResult> FollowUser(int userId)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Check if user exists
            var userToFollow = await _dbContext.Users.FindAsync(userId);
            if (userToFollow == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            // Check if already following
            var existingFollow = await _dbContext.UserFollows
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUserId && uf.FollowingId == userId);

            if (existingFollow != null)
            {
                return BadRequest(new { Message = "You are already following this user" });
            }

            // Create follow relationship
            var follow = new UserFollow
            {
                FollowerId = currentUserId,
                FollowingId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.UserFollows.Add(follow);
            await _dbContext.SaveChangesAsync();

            // Create notification for the followed user
            var currentUser = await _dbContext.Users.FindAsync(currentUserId);
            var notification = new Notification
            {
                UserId = userId,
                Type = "follow",
                Title = "New Follower",
                Content = $"{currentUser?.FullName} started following you",
                SenderId = currentUserId,
                RedirectUrl = $"/profile/{currentUserId}",
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Notifications.Add(notification);
            await _dbContext.SaveChangesAsync();

            // Send notification
            await _notificationService.CreateAndSendNotificationAsync(
                notification,
                sendEmail: false,
                sendPush: true);

            return Ok(new { Message = "User followed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error following user");
            return StatusCode(500, new { Message = "An error occurred while following the user" });
        }
    }

    [HttpDelete("unfollow/{userId}")]
    public async Task<IActionResult> UnfollowUser(int userId)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find follow relationship
            var follow = await _dbContext.UserFollows
                .FirstOrDefaultAsync(uf => uf.FollowerId == currentUserId && uf.FollowingId == userId);

            if (follow == null)
            {
                return NotFound(new { Message = "You are not following this user" });
            }

            // Remove follow relationship
            _dbContext.UserFollows.Remove(follow);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "User unfollowed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unfollowing user");
            return StatusCode(500, new { Message = "An error occurred while unfollowing the user" });
        }
    }

    [HttpGet("followers")]
    public async Task<IActionResult> GetFollowers([FromQuery] int? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Get current user ID from claims if userId not provided
            int targetUserId = userId ?? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (targetUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Get followers
            var query = _dbContext.UserFollows
                .Where(uf => uf.FollowingId == targetUserId)
                .Include(uf => uf.Follower)
                .OrderByDescending(uf => uf.CreatedAt);

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var followers = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(uf => new
                {
                    uf.FollowerId,
                    User = new
                    {
                        uf.Follower.Id,
                        uf.Follower.FirstName,
                        uf.Follower.LastName,
                        uf.Follower.FullName,
                        uf.Follower.ProfilePictureUrl,
                        uf.Follower.Rank,
                        uf.Follower.City,
                        uf.Follower.State,
                        FollowedAt = uf.CreatedAt
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Followers = followers
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followers");
            return StatusCode(500, new { Message = "An error occurred while getting followers" });
        }
    }

    [HttpGet("following")]
    public async Task<IActionResult> GetFollowing([FromQuery] int? userId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Get current user ID from claims if userId not provided
            int targetUserId = userId ?? int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (targetUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Get following
            var query = _dbContext.UserFollows
                .Where(uf => uf.FollowerId == targetUserId)
                .Include(uf => uf.Following)
                .OrderByDescending(uf => uf.CreatedAt);

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var following = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(uf => new
                {
                    uf.FollowingId,
                    User = new
                    {
                        uf.Following.Id,
                        uf.Following.FirstName,
                        uf.Following.LastName,
                        uf.Following.FullName,
                        uf.Following.ProfilePictureUrl,
                        uf.Following.Rank,
                        uf.Following.City,
                        uf.Following.State,
                        FollowedAt = uf.CreatedAt
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Following = following
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting following");
            return StatusCode(500, new { Message = "An error occurred while getting following" });
        }
    }

    [HttpGet("is-following/{userId}")]
    public async Task<IActionResult> IsFollowing(int userId)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Check if following
            bool isFollowing = await _dbContext.UserFollows
                .AnyAsync(uf => uf.FollowerId == currentUserId && uf.FollowingId == userId);

            return Ok(new { IsFollowing = isFollowing });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if following");
            return StatusCode(500, new { Message = "An error occurred while checking if following" });
        }
    }

    #endregion

    #region Like

    [HttpPost("like")]
    public async Task<IActionResult> AddLike([FromBody] LikeRequest request)
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
            if ((request.VideoId == null && request.PhotoId == null) ||
                (request.VideoId != null && request.PhotoId != null))
            {
                return BadRequest(new { Message = "Either VideoId or PhotoId must be provided, but not both" });
            }

            // Check if content exists
            if (request.VideoId != null)
            {
                var video = await _dbContext.Videos.FindAsync(request.VideoId);
                if (video == null)
                {
                    return NotFound(new { Message = "Video not found" });
                }
            }
            else if (request.PhotoId != null)
            {
                var photo = await _dbContext.Photos.FindAsync(request.PhotoId);
                if (photo == null)
                {
                    return NotFound(new { Message = "Photo not found" });
                }
            }

            // Check if already liked
            var existingLike = await _dbContext.Likes
                .FirstOrDefaultAsync(l => l.UserId == currentUserId &&
                                         (l.VideoId == request.VideoId || l.PhotoId == request.PhotoId));

            if (existingLike != null)
            {
                return BadRequest(new { Message = "You have already liked this content" });
            }

            // Create like
            var like = new Like
            {
                UserId = currentUserId,
                VideoId = request.VideoId,
                PhotoId = request.PhotoId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Likes.Add(like);
            await _dbContext.SaveChangesAsync();

            // Get content owner ID for notification
            int? contentOwnerId = null;
            string contentType = "";
            int contentId = 0;

            if (request.VideoId != null)
            {
                var video = await _dbContext.Videos.FindAsync(request.VideoId);
                contentOwnerId = video?.UserId;
                contentType = "video";
                contentId = video?.Id ?? 0;
            }
            else if (request.PhotoId != null)
            {
                var photo = await _dbContext.Photos.FindAsync(request.PhotoId);
                contentOwnerId = photo?.UserId;
                contentType = "photo";
                contentId = photo?.Id ?? 0;
            }

            // Create notification for content owner if not the current user
            if (contentOwnerId != null && contentOwnerId != currentUserId)
            {
                var currentUser = await _dbContext.Users.FindAsync(currentUserId);
                var notification = new Notification
                {
                    UserId = contentOwnerId.Value,
                    Type = "like",
                    Title = "New Like",
                    Content = $"{currentUser?.FullName} liked your {contentType}",
                    SenderId = currentUserId,
                    RedirectUrl = $"/{contentType}/{contentId}",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                // Send notification
                await _notificationService.CreateAndSendNotificationAsync(
                    notification,
                    sendEmail: false,
                    sendPush: true);
            }

            return Ok(new { Message = "Content liked successfully", LikeId = like.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding like");
            return StatusCode(500, new { Message = "An error occurred while adding like" });
        }
    }

    [HttpDelete("unlike")]
    public async Task<IActionResult> RemoveLike([FromBody] LikeRequest request)
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
            if ((request.VideoId == null && request.PhotoId == null) ||
                (request.VideoId != null && request.PhotoId != null))
            {
                return BadRequest(new { Message = "Either VideoId or PhotoId must be provided, but not both" });
            }

            // Find like
            var like = await _dbContext.Likes
                .FirstOrDefaultAsync(l => l.UserId == currentUserId &&
                                         (l.VideoId == request.VideoId || l.PhotoId == request.PhotoId));

            if (like == null)
            {
                return NotFound(new { Message = "Like not found" });
            }

            // Remove like
            _dbContext.Likes.Remove(like);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Like removed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing like");
            return StatusCode(500, new { Message = "An error occurred while removing like" });
        }
    }

    [HttpGet("likes")]
    public async Task<IActionResult> GetLikes([FromQuery] int? videoId, [FromQuery] int? photoId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Validate request
            if ((videoId == null && photoId == null) ||
                (videoId != null && photoId != null))
            {
                return BadRequest(new { Message = "Either videoId or photoId must be provided, but not both" });
            }

            // Get likes
            var query = _dbContext.Likes
                .Where(l => (videoId != null && l.VideoId == videoId) ||
                           (photoId != null && l.PhotoId == photoId))
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt);

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var likes = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(l => new
                {
                    l.Id,
                    l.UserId,
                    User = new
                    {
                        l.User.Id,
                        l.User.FirstName,
                        l.User.LastName,
                        l.User.FullName,
                        l.User.ProfilePictureUrl
                    },
                    l.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Likes = likes
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting likes");
            return StatusCode(500, new { Message = "An error occurred while getting likes" });
        }
    }

    [HttpGet("has-liked")]
    public async Task<IActionResult> HasLiked([FromQuery] int? videoId, [FromQuery] int? photoId)
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
            if ((videoId == null && photoId == null) ||
                (videoId != null && photoId != null))
            {
                return BadRequest(new { Message = "Either videoId or photoId must be provided, but not both" });
            }

            // Check if liked
            bool hasLiked = await _dbContext.Likes
                .AnyAsync(l => l.UserId == currentUserId &&
                              ((videoId != null && l.VideoId == videoId) ||
                               (photoId != null && l.PhotoId == photoId)));

            return Ok(new { HasLiked = hasLiked });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if liked");
            return StatusCode(500, new { Message = "An error occurred while checking if liked" });
        }
    }

    #endregion

    #region Comment

    [HttpPost("comment")]
    public async Task<IActionResult> AddComment([FromBody] CommentRequest request)
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
                return BadRequest(new { Message = "Comment content is required" });
            }

            if ((request.VideoId == null && request.PhotoId == null) ||
                (request.VideoId != null && request.PhotoId != null))
            {
                return BadRequest(new { Message = "Either VideoId or PhotoId must be provided, but not both" });
            }

            // Check if content exists
            int? contentOwnerId = null;
            string contentType = "";
            int contentId = 0;

            if (request.VideoId != null)
            {
                var video = await _dbContext.Videos.FindAsync(request.VideoId);
                if (video == null)
                {
                    return NotFound(new { Message = "Video not found" });
                }
                contentOwnerId = video.UserId;
                contentType = "video";
                contentId = video.Id;
            }
            else if (request.PhotoId != null)
            {
                var photo = await _dbContext.Photos.FindAsync(request.PhotoId);
                if (photo == null)
                {
                    return NotFound(new { Message = "Photo not found" });
                }
                contentOwnerId = photo.UserId;
                contentType = "photo";
                contentId = photo.Id;
            }

            // Check if parent comment exists if provided
            if (request.ParentCommentId != null)
            {
                var parentComment = await _dbContext.Comments.FindAsync(request.ParentCommentId);
                if (parentComment == null)
                {
                    return NotFound(new { Message = "Parent comment not found" });
                }
            }

            // Create comment
            var comment = new Comment
            {
                UserId = currentUserId,
                VideoId = request.VideoId,
                PhotoId = request.PhotoId,
                ParentCommentId = request.ParentCommentId,
                Content = request.Content,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Comments.Add(comment);
            await _dbContext.SaveChangesAsync();

            // Create notification for content owner if not the current user
            if (contentOwnerId != null && contentOwnerId != currentUserId)
            {
                var currentUser = await _dbContext.Users.FindAsync(currentUserId);
                var notification = new Notification
                {
                    UserId = contentOwnerId.Value,
                    Type = "comment",
                    Title = "New Comment",
                    Content = $"{currentUser?.FullName} commented on your {contentType}",
                    SenderId = currentUserId,
                    RedirectUrl = $"/{contentType}/{contentId}",
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Notifications.Add(notification);
                await _dbContext.SaveChangesAsync();

                // Send notification
                await _notificationService.CreateAndSendNotificationAsync(
                    notification,
                    sendEmail: false,
                    sendPush: true);
            }

            // Create notification for parent comment owner if not the current user
            if (request.ParentCommentId != null)
            {
                var parentComment = await _dbContext.Comments
                    .Include(c => c.User)
                    .FirstOrDefaultAsync(c => c.Id == request.ParentCommentId);

                if (parentComment != null && parentComment.UserId != currentUserId)
                {
                    var currentUser = await _dbContext.Users.FindAsync(currentUserId);
                    var notification = new Notification
                    {
                        UserId = parentComment.UserId,
                        Type = "reply",
                        Title = "New Reply",
                        Content = $"{currentUser?.FullName} replied to your comment",
                        SenderId = currentUserId,
                        RedirectUrl = $"/{contentType}/{contentId}",
                        CreatedAt = DateTime.UtcNow
                    };

                    _dbContext.Notifications.Add(notification);
                    await _dbContext.SaveChangesAsync();

                    // Send notification
                    await _notificationService.CreateAndSendNotificationAsync(
                        notification,
                        sendEmail: false,
                        sendPush: true);
                }
            }

            // Return comment with user info
            var user = await _dbContext.Users.FindAsync(currentUserId);
            var commentResponse = new
            {
                comment.Id,
                comment.UserId,
                User = new
                {
                    user?.Id,
                    user?.FirstName,
                    user?.LastName,
                    user?.FullName,
                    user?.ProfilePictureUrl
                },
                comment.VideoId,
                comment.PhotoId,
                comment.ParentCommentId,
                comment.Content,
                comment.CreatedAt,
                comment.UpdatedAt,
                Replies = new List<object>()
            };

            return Ok(commentResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding comment");
            return StatusCode(500, new { Message = "An error occurred while adding comment" });
        }
    }

    [HttpPut("comment/{id}")]
    public async Task<IActionResult> UpdateComment(int id, [FromBody] UpdateCommentRequest request)
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
                return BadRequest(new { Message = "Comment content is required" });
            }

            // Find comment
            var comment = await _dbContext.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound(new { Message = "Comment not found" });
            }

            // Check if user is the comment owner
            if (comment.UserId != currentUserId)
            {
                return Forbid();
            }

            // Update comment
            comment.Content = request.Content;
            comment.UpdatedAt = DateTime.UtcNow;

            _dbContext.Comments.Update(comment);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Comment updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating comment");
            return StatusCode(500, new { Message = "An error occurred while updating comment" });
        }
    }

    [HttpDelete("comment/{id}")]
    public async Task<IActionResult> DeleteComment(int id)
    {
        try
        {
            // Get current user ID from claims
            int currentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (currentUserId == 0)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            // Find comment
            var comment = await _dbContext.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound(new { Message = "Comment not found" });
            }

            // Check if user is the comment owner
            if (comment.UserId != currentUserId)
            {
                return Forbid();
            }

            // Delete comment
            _dbContext.Comments.Remove(comment);
            await _dbContext.SaveChangesAsync();

            return Ok(new { Message = "Comment deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting comment");
            return StatusCode(500, new { Message = "An error occurred while deleting comment" });
        }
    }

    [HttpGet("comments")]
    public async Task<IActionResult> GetComments([FromQuery] int? videoId, [FromQuery] int? photoId, [FromQuery] int? parentCommentId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        try
        {
            // Validate request
            if ((videoId == null && photoId == null) ||
                (videoId != null && photoId != null))
            {
                return BadRequest(new { Message = "Either videoId or photoId must be provided, but not both" });
            }

            // Get comments
            var query = _dbContext.Comments
                .Where(c => (videoId != null && c.VideoId == videoId) ||
                           (photoId != null && c.PhotoId == photoId))
                .Where(c => c.ParentCommentId == parentCommentId)
                .Include(c => c.User)
                .OrderByDescending(c => c.CreatedAt);

            // Get total count
            int totalCount = await query.CountAsync();

            // Apply pagination
            var comments = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(c => new
                {
                    c.Id,
                    c.UserId,
                    User = new
                    {
                        c.User.Id,
                        c.User.FirstName,
                        c.User.LastName,
                        c.User.FullName,
                        c.User.ProfilePictureUrl
                    },
                    c.VideoId,
                    c.PhotoId,
                    c.ParentCommentId,
                    c.Content,
                    c.CreatedAt,
                    c.UpdatedAt,
                    RepliesCount = _dbContext.Comments.Count(r => r.ParentCommentId == c.Id)
                })
                .ToListAsync();

            return Ok(new
            {
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                Comments = comments
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting comments");
            return StatusCode(500, new { Message = "An error occurred while getting comments" });
        }
    }

    #endregion
}

// Request models
public class LikeRequest
{
    public int? VideoId { get; set; }
    public int? PhotoId { get; set; }
}

public class CommentRequest
{
    public int? VideoId { get; set; }
    public int? PhotoId { get; set; }
    public int? ParentCommentId { get; set; }
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentRequest
{
    public string Content { get; set; } = string.Empty;
}

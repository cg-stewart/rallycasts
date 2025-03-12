using api.Controllers;
using api.Data;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace api.Tests.Controllers;

public class SocialControllerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<SocialController>> _loggerMock;
    private readonly Mock<NotificationService> _notificationServiceMock;
    private readonly SocialController _controller;

    public SocialControllerTests()
    {
        _dbContext = TestHelper.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<SocialController>>();
        _notificationServiceMock = new Mock<NotificationService>();
        _controller = new SocialController(_dbContext, _notificationServiceMock.Object, _loggerMock.Object);

        // Setup controller context with authenticated user
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "john.doe@example.com")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task FollowUser_ValidUser_ReturnsOk()
    {
        // Arrange
        var userToFollowId = 3; // Bob Johnson

        _notificationServiceMock.Setup(x => x.CreateAndSendNotificationAsync(
            It.IsAny<Notification>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new ServiceResponse<object>
            {
                IsSuccess = true,
                Message = "Notification sent successfully"
            });

        // Act
        var result = await _controller.FollowUser(userToFollowId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var userFollow = Assert.IsType<UserFollow>(okResult.Value);
        Assert.Equal(1, userFollow.FollowerId);
        Assert.Equal(userToFollowId, userFollow.FollowingId);

        // Verify notification was sent
        _notificationServiceMock.Verify(x => x.CreateAndSendNotificationAsync(
            It.IsAny<Notification>(), true, true), Times.Once);
    }

    [Fact]
    public async Task FollowUser_InvalidUser_ReturnsBadRequest()
    {
        // Arrange
        var invalidUserId = 999;

        // Act
        var result = await _controller.FollowUser(invalidUserId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task FollowUser_AlreadyFollowing_ReturnsBadRequest()
    {
        // Arrange
        var userToFollowId = 2; // Jane Smith
        
        // Add existing follow relationship
        var existingFollow = new UserFollow
        {
            FollowerId = 1,
            FollowingId = userToFollowId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.UserFollows.Add(existingFollow);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.FollowUser(userToFollowId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already following", badRequestResult.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnfollowUser_ValidUser_ReturnsOk()
    {
        // Arrange
        var userToUnfollowId = 2; // Jane Smith
        
        // Add existing follow relationship
        var existingFollow = new UserFollow
        {
            FollowerId = 1,
            FollowingId = userToUnfollowId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.UserFollows.Add(existingFollow);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.UnfollowUser(userToUnfollowId);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        // Verify the follow relationship was removed
        var followExists = await _dbContext.UserFollows
            .AnyAsync(uf => uf.FollowerId == 1 && uf.FollowingId == userToUnfollowId);
        Assert.False(followExists);
    }

    [Fact]
    public async Task UnfollowUser_NotFollowing_ReturnsBadRequest()
    {
        // Arrange
        var userToUnfollowId = 3; // Bob Johnson (not being followed)

        // Act
        var result = await _controller.UnfollowUser(userToUnfollowId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("not following", badRequestResult.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetFollowers_ReturnsOkWithFollowers()
    {
        // Arrange
        var userId = 2; // Jane Smith
        
        // Add followers for Jane
        var follower1 = new UserFollow
        {
            FollowerId = 1, // John Doe
            FollowingId = userId,
            CreatedAt = DateTime.UtcNow.AddDays(-5)
        };
        var follower2 = new UserFollow
        {
            FollowerId = 3, // Bob Johnson
            FollowingId = userId,
            CreatedAt = DateTime.UtcNow.AddDays(-2)
        };
        _dbContext.UserFollows.AddRange(follower1, follower2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetFollowers(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var followers = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Equal(2, followers.Count());
    }

    [Fact]
    public async Task GetFollowing_ReturnsOkWithFollowing()
    {
        // Arrange
        var userId = 1; // John Doe
        
        // Add users that John is following
        var following1 = new UserFollow
        {
            FollowerId = userId,
            FollowingId = 2, // Jane Smith
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };
        var following2 = new UserFollow
        {
            FollowerId = userId,
            FollowingId = 3, // Bob Johnson
            CreatedAt = DateTime.UtcNow.AddDays(-3)
        };
        _dbContext.UserFollows.AddRange(following1, following2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.GetFollowing(userId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var following = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.Equal(2, following.Count());
    }

    [Fact]
    public async Task LikeVideo_ValidVideo_ReturnsOk()
    {
        // Arrange
        var videoId = 1;

        _notificationServiceMock.Setup(x => x.CreateAndSendNotificationAsync(
            It.IsAny<Notification>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new ServiceResponse<object>
            {
                IsSuccess = true,
                Message = "Notification sent successfully"
            });

        // Act
        var result = await _controller.LikeVideo(videoId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var like = Assert.IsType<Like>(okResult.Value);
        Assert.Equal(1, like.UserId);
        Assert.Equal(videoId, like.VideoId);

        // Verify notification was sent
        _notificationServiceMock.Verify(x => x.CreateAndSendNotificationAsync(
            It.IsAny<Notification>(), true, true), Times.Once);
    }

    [Fact]
    public async Task LikeVideo_InvalidVideo_ReturnsBadRequest()
    {
        // Arrange
        var invalidVideoId = 999;

        // Act
        var result = await _controller.LikeVideo(invalidVideoId);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task LikeVideo_AlreadyLiked_ReturnsBadRequest()
    {
        // Arrange
        var videoId = 2;
        
        // Add existing like
        var existingLike = new Like
        {
            UserId = 1,
            VideoId = videoId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Likes.Add(existingLike);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.LikeVideo(videoId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("already liked", badRequestResult.Value.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UnlikeVideo_ValidVideo_ReturnsOk()
    {
        // Arrange
        var videoId = 1;
        
        // Add existing like
        var existingLike = new Like
        {
            UserId = 1,
            VideoId = videoId,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Likes.Add(existingLike);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.UnlikeVideo(videoId);

        // Assert
        Assert.IsType<OkObjectResult>(result);

        // Verify the like was removed
        var likeExists = await _dbContext.Likes
            .AnyAsync(l => l.UserId == 1 && l.VideoId == videoId);
        Assert.False(likeExists);
    }

    [Fact]
    public async Task AddComment_ValidVideo_ReturnsCreated()
    {
        // Arrange
        var videoId = 1;
        var comment = new Comment
        {
            VideoId = videoId,
            Content = "Test comment content",
            CreatedAt = DateTime.UtcNow
        };

        _notificationServiceMock.Setup(x => x.CreateAndSendNotificationAsync(
            It.IsAny<Notification>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .ReturnsAsync(new ServiceResponse<object>
            {
                IsSuccess = true,
                Message = "Notification sent successfully"
            });

        // Act
        var result = await _controller.AddComment(comment);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdComment = Assert.IsType<Comment>(createdResult.Value);
        Assert.Equal(1, createdComment.UserId);
        Assert.Equal(videoId, createdComment.VideoId);
        Assert.Equal(comment.Content, createdComment.Content);

        // Verify notification was sent
        _notificationServiceMock.Verify(x => x.CreateAndSendNotificationAsync(
            It.IsAny<Notification>(), true, true), Times.Once);
    }

    [Fact]
    public async Task DeleteComment_OwnComment_ReturnsNoContent()
    {
        // Arrange
        var commentId = 1;
        
        // Add a comment by the current user
        var userComment = new Comment
        {
            Id = commentId,
            UserId = 1,
            VideoId = 2,
            Content = "Comment to delete",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(userComment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify the comment was deleted
        var commentExists = await _dbContext.Comments.AnyAsync(c => c.Id == commentId);
        Assert.False(commentExists);
    }

    [Fact]
    public async Task DeleteComment_OtherUserComment_ReturnsForbid()
    {
        // Arrange
        var commentId = 2;
        
        // Add a comment by another user
        var otherUserComment = new Comment
        {
            Id = commentId,
            UserId = 2, // Jane Smith
            VideoId = 1,
            Content = "Comment by another user",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Comments.Add(otherUserComment);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteComment(commentId);

        // Assert
        Assert.IsType<ForbidResult>(result);

        // Verify the comment was not deleted
        var commentExists = await _dbContext.Comments.AnyAsync(c => c.Id == commentId);
        Assert.True(commentExists);
    }
}

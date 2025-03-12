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

public class MessageControllerTests
{
    private readonly ApplicationDbContext _dbContext;
    private readonly Mock<ILogger<MessageController>> _loggerMock;
    private readonly Mock<NotificationService> _notificationServiceMock;
    private readonly MessageController _controller;

    public MessageControllerTests()
    {
        _dbContext = TestHelper.CreateInMemoryDbContext();
        _loggerMock = new Mock<ILogger<MessageController>>();
        _notificationServiceMock = new Mock<NotificationService>();
        _controller = new MessageController(_dbContext, _notificationServiceMock.Object, _loggerMock.Object);

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
    public async Task GetConversations_ReturnsOkResult()
    {
        // Arrange
        // Test data is already seeded in TestHelper

        // Act
        var result = await _controller.GetConversations();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var conversations = Assert.IsAssignableFrom<IEnumerable<object>>(okResult.Value);
        Assert.NotEmpty(conversations);
    }

    [Fact]
    public async Task GetMessages_WithValidUserId_ReturnsOkResult()
    {
        // Arrange
        var recipientId = 2; // Jane Smith

        // Act
        var result = await _controller.GetMessages(recipientId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var messages = Assert.IsAssignableFrom<IEnumerable<Message>>(okResult.Value);
        Assert.NotNull(messages);
    }

    [Fact]
    public async Task GetMessages_WithInvalidUserId_ReturnsNotFound()
    {
        // Arrange
        var recipientId = 999; // Non-existent user

        // Act
        var result = await _controller.GetMessages(recipientId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task SendMessage_WithValidData_ReturnsCreatedResult()
    {
        // Arrange
        var message = new Message
        {
            RecipientId = 2,
            Content = "Test message content",
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
        var result = await _controller.SendMessage(message);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdMessage = Assert.IsType<Message>(createdResult.Value);
        Assert.Equal(message.Content, createdMessage.Content);
        Assert.Equal(1, createdMessage.SenderId);
        Assert.Equal(2, createdMessage.RecipientId);
        Assert.False(createdMessage.IsRead);

        // Verify notification was sent
        _notificationServiceMock.Verify(x => x.CreateAndSendNotificationAsync(
            It.IsAny<Notification>(), true, true), Times.Once);
    }

    [Fact]
    public async Task SendMessage_WithInvalidRecipient_ReturnsBadRequest()
    {
        // Arrange
        var message = new Message
        {
            RecipientId = 999, // Non-existent user
            Content = "Test message content"
        };

        // Act
        var result = await _controller.SendMessage(message);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task MarkMessageAsRead_WithValidId_ReturnsOkResult()
    {
        // Arrange
        // Add a test message to mark as read
        var testMessage = new Message
        {
            SenderId = 2,
            RecipientId = 1,
            Content = "Message to mark as read",
            CreatedAt = DateTime.UtcNow,
            IsRead = false
        };
        _dbContext.Messages.Add(testMessage);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.MarkMessageAsRead(testMessage.Id);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var message = Assert.IsType<Message>(okResult.Value);
        Assert.True(message.IsRead);
        Assert.NotNull(message.ReadAt);
    }

    [Fact]
    public async Task MarkMessageAsRead_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidMessageId = 999;

        // Act
        var result = await _controller.MarkMessageAsRead(invalidMessageId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMessage_WithValidId_ReturnsNoContent()
    {
        // Arrange
        // Add a test message to delete
        var testMessage = new Message
        {
            SenderId = 1,
            RecipientId = 2,
            Content = "Message to delete",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Messages.Add(testMessage);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteMessage(testMessage.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);

        // Verify the message was deleted
        var deletedMessage = await _dbContext.Messages.FindAsync(testMessage.Id);
        Assert.Null(deletedMessage);
    }

    [Fact]
    public async Task DeleteMessage_WithInvalidId_ReturnsNotFound()
    {
        // Arrange
        var invalidMessageId = 999;

        // Act
        var result = await _controller.DeleteMessage(invalidMessageId);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task DeleteMessage_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        // Add a message where the current user is not the sender
        var testMessage = new Message
        {
            SenderId = 2, // Jane Smith, not the current user
            RecipientId = 3,
            Content = "Message from another user",
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Messages.Add(testMessage);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _controller.DeleteMessage(testMessage.Id);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }
}

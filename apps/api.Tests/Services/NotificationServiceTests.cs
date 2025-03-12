using api.Data;
using api.Models;
using api.Services;
using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SimpleQueueService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace api.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IAmazonSimpleNotificationService> _snsMock;
    private readonly Mock<IAmazonSimpleEmailService> _sesMock;
    private readonly Mock<IAmazonSQS> _sqsMock;
    private readonly Mock<IOptions<NotificationSettings>> _settingsMock;
    private readonly Mock<ILogger<NotificationService>> _loggerMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly NotificationService _notificationService;

    public NotificationServiceTests()
    {
        _snsMock = new Mock<IAmazonSimpleNotificationService>();
        _sesMock = new Mock<IAmazonSimpleEmailService>();
        _sqsMock = new Mock<IAmazonSQS>();
        _settingsMock = new Mock<IOptions<NotificationSettings>>();
        _loggerMock = new Mock<ILogger<NotificationService>>();
        _dbContext = TestHelper.CreateInMemoryDbContext();

        _settingsMock.Setup(x => x.Value).Returns(new NotificationSettings
        {
            EmailSourceAddress = "notifications@rallycasts.com",
            SnsTopicArn = "arn:aws:sns:us-east-1:123456789012:test-topic",
            SqsQueueUrl = "https://sqs.us-east-1.amazonaws.com/123456789012/test-queue",
            IosPlatformApplicationArn = "arn:aws:sns:us-east-1:123456789012:app/APNS/test-ios",
            AndroidPlatformApplicationArn = "arn:aws:sns:us-east-1:123456789012:app/GCM/test-android"
        });

        _notificationService = new NotificationService(
            _snsMock.Object,
            _sesMock.Object,
            _sqsMock.Object,
            _settingsMock.Object,
            _dbContext,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateAndSendNotification_SendEmail_CallsSES()
    {
        // Arrange
        var notification = new Notification
        {
            Id = 3,
            UserId = 1,
            Type = "system",
            Title = "Test Notification",
            Content = "This is a test notification",
            RedirectUrl = "/test",
            CreatedAt = DateTime.UtcNow
        };

        _sesMock.Setup(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendEmailResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                MessageId = "test-message-id"
            });

        // Act
        var result = await _notificationService.CreateAndSendNotificationAsync(notification, sendEmail: true, sendPush: false);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Notification sent successfully", result.Message);

        // Verify SES was called with correct parameters
        _sesMock.Verify(x => x.SendEmailAsync(
            It.Is<SendEmailRequest>(r => 
                r.Source == _settingsMock.Object.Value.EmailSourceAddress && 
                r.Destination.ToAddresses.Contains("john.doe@example.com") && 
                r.Message.Subject.Data == "Test Notification"),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify SNS was not called
        _snsMock.Verify(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAndSendNotification_SendPush_CallsSNS()
    {
        // Arrange
        var notification = new Notification
        {
            Id = 4,
            UserId = 1,
            Type = "system",
            Title = "Test Push Notification",
            Content = "This is a test push notification",
            RedirectUrl = "/test",
            CreatedAt = DateTime.UtcNow
        };

        _snsMock.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                MessageId = "test-message-id"
            });

        // Act
        var result = await _notificationService.CreateAndSendNotificationAsync(notification, sendEmail: false, sendPush: true);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Notification sent successfully", result.Message);

        // Verify SNS was called with correct parameters
        _snsMock.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(r => 
                r.TopicArn == _settingsMock.Object.Value.SnsTopicArn && 
                r.Message.Contains("Test Push Notification")),
            It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify SES was not called
        _sesMock.Verify(x => x.SendEmailAsync(It.IsAny<SendEmailRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreatePlatformEndpoint_ValidInput_ReturnsEndpointArn()
    {
        // Arrange
        var platformApplicationArn = "arn:aws:sns:us-east-1:123456789012:app/APNS/test-ios";
        var deviceToken = "device-token-123";
        var userId = "1";
        var expectedEndpointArn = "arn:aws:sns:us-east-1:123456789012:endpoint/APNS/test-ios/123456789012-123";

        _snsMock.Setup(x => x.CreatePlatformEndpointAsync(It.IsAny<CreatePlatformEndpointRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreatePlatformEndpointResponse
            {
                EndpointArn = expectedEndpointArn
            });

        // Act
        var result = await _notificationService.CreatePlatformEndpointAsync(platformApplicationArn, deviceToken, userId);

        // Assert
        Assert.Equal(expectedEndpointArn, result);

        // Verify SNS was called with correct parameters
        _snsMock.Verify(x => x.CreatePlatformEndpointAsync(
            It.Is<CreatePlatformEndpointRequest>(r => 
                r.PlatformApplicationArn == platformApplicationArn && 
                r.Token == deviceToken && 
                r.CustomUserData == userId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SubscribeEndpointToTopic_ValidInput_ReturnsSubscriptionArn()
    {
        // Arrange
        var endpointArn = "arn:aws:sns:us-east-1:123456789012:endpoint/APNS/test-ios/123456789012-123";
        var expectedSubscriptionArn = "arn:aws:sns:us-east-1:123456789012:test-topic:123456789012-123";

        _snsMock.Setup(x => x.SubscribeAsync(It.IsAny<SubscribeRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SubscribeResponse
            {
                SubscriptionArn = expectedSubscriptionArn
            });

        // Act
        var result = await _notificationService.SubscribeEndpointToTopicAsync(endpointArn);

        // Assert
        Assert.Equal(expectedSubscriptionArn, result);

        // Verify SNS was called with correct parameters
        _snsMock.Verify(x => x.SubscribeAsync(
            It.Is<SubscribeRequest>(r => 
                r.TopicArn == _settingsMock.Object.Value.SnsTopicArn && 
                r.Protocol == "application" && 
                r.Endpoint == endpointArn),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendDirectPushNotification_ValidInput_CallsSNS()
    {
        // Arrange
        var endpointArn = "arn:aws:sns:us-east-1:123456789012:endpoint/APNS/test-ios/123456789012-123";
        var title = "Direct Push Test";
        var body = "This is a direct push notification test";
        var data = new Dictionary<string, string>
        {
            { "key1", "value1" },
            { "key2", "value2" }
        };

        _snsMock.Setup(x => x.PublishAsync(It.IsAny<PublishRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PublishResponse
            {
                HttpStatusCode = HttpStatusCode.OK,
                MessageId = "test-message-id"
            });

        // Act
        var result = await _notificationService.SendDirectPushNotificationAsync(endpointArn, title, body, data);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Push notification sent successfully", result.Message);

        // Verify SNS was called with correct parameters
        _snsMock.Verify(x => x.PublishAsync(
            It.Is<PublishRequest>(r => 
                r.TargetArn == endpointArn && 
                r.Message.Contains(title) && 
                r.Message.Contains(body) && 
                r.Message.Contains("value1") && 
                r.Message.Contains("value2")),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SendBulkNotifications_ValidInput_CallsSQS()
    {
        // Arrange
        var notifications = new List<Notification>
        {
            new Notification
            {
                Id = 5,
                UserId = 1,
                Type = "system",
                Title = "Bulk Test 1",
                Content = "This is bulk test notification 1",
                RedirectUrl = "/test1",
                CreatedAt = DateTime.UtcNow
            },
            new Notification
            {
                Id = 6,
                UserId = 2,
                Type = "system",
                Title = "Bulk Test 2",
                Content = "This is bulk test notification 2",
                RedirectUrl = "/test2",
                CreatedAt = DateTime.UtcNow
            }
        };

        _sqsMock.Setup(x => x.SendMessageBatchAsync(It.IsAny<string>(), It.IsAny<List<SendMessageBatchRequestEntry>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SendMessageBatchResponse
            {
                Successful = new List<SendMessageBatchResultEntry>
                {
                    new SendMessageBatchResultEntry { Id = "5", MessageId = "message-id-5" },
                    new SendMessageBatchResultEntry { Id = "6", MessageId = "message-id-6" }
                }
            });

        // Act
        var result = await _notificationService.SendBulkNotificationsAsync(notifications);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Bulk notifications queued successfully", result.Message);

        // Verify SQS was called with correct parameters
        _sqsMock.Verify(x => x.SendMessageBatchAsync(
            _settingsMock.Object.Value.SqsQueueUrl,
            It.Is<List<SendMessageBatchRequestEntry>>(entries => 
                entries.Count == 2 && 
                entries[0].Id == "5" && 
                entries[1].Id == "6"),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

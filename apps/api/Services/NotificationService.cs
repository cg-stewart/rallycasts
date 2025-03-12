using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace api.Services;

/// <summary>
/// Service for handling notifications using AWS SNS, SES, and SQS
/// </summary>
public class NotificationService
{
    private readonly IAmazonSimpleNotificationService _snsClient;
    private readonly IAmazonSimpleEmailService _sesClient;
    private readonly IAmazonSQS _sqsClient;
    private readonly NotificationSettings _settings;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        IAmazonSimpleNotificationService snsClient,
        IAmazonSimpleEmailService sesClient,
        IAmazonSQS sqsClient,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationService> logger)
    {
        _snsClient = snsClient;
        _sesClient = sesClient;
        _sqsClient = sqsClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Sends an email notification using AWS SES
    /// </summary>
    public async Task<SendEmailResponse> SendEmailAsync(string toEmail, string subject, string htmlBody, string textBody = "")
    {
        try
        {
            var request = new SendEmailRequest
            {
                Source = _settings.SenderEmail,
                Destination = new Destination
                {
                    ToAddresses = new List<string> { toEmail }
                },
                Message = new Message
                {
                    Subject = new Content(subject),
                    Body = new Body
                    {
                        Html = new Content
                        {
                            Charset = "UTF-8",
                            Data = htmlBody
                        }
                    }
                }
            };

            if (!string.IsNullOrEmpty(textBody))
            {
                request.Message.Body.Text = new Content
                {
                    Charset = "UTF-8",
                    Data = textBody
                };
            }

            var response = await _sesClient.SendEmailAsync(request);
            _logger.LogInformation("Email sent to {Email} with subject {Subject}", toEmail, subject);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending email to {Email} with subject {Subject}", toEmail, subject);
            throw;
        }
    }

    /// <summary>
    /// Sends a push notification using AWS SNS
    /// </summary>
    public async Task<PublishResponse> SendPushNotificationAsync(string targetArn, string title, string message, Dictionary<string, object>? data = null)
    {
        try
        {
            var payload = new
            {
                default = message,
                APNS = JsonSerializer.Serialize(new
                {
                    aps = new
                    {
                        alert = new
                        {
                            title = title,
                            body = message
                        },
                        sound = "default",
                        data
                    }
                }),
                GCM = JsonSerializer.Serialize(new
                {
                    notification = new
                    {
                        title = title,
                        body = message
                    },
                    data
                })
            };

            var request = new PublishRequest
            {
                TargetArn = targetArn,
                Message = JsonSerializer.Serialize(payload),
                MessageStructure = "json"
            };

            var response = await _snsClient.PublishAsync(request);
            _logger.LogInformation("Push notification sent to {TargetArn} with title {Title}", targetArn, title);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending push notification to {TargetArn} with title {Title}", targetArn, title);
            throw;
        }
    }

    /// <summary>
    /// Sends a notification to an SNS topic
    /// </summary>
    public async Task<PublishResponse> SendTopicNotificationAsync(string message, Dictionary<string, object>? attributes = null)
    {
        try
        {
            var request = new PublishRequest
            {
                TopicArn = _settings.NotificationTopicArn,
                Message = message
            };

            if (attributes != null)
            {
                request.MessageAttributes = attributes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = kvp.Value.ToString()
                    }
                );
            }

            var response = await _snsClient.PublishAsync(request);
            _logger.LogInformation("Topic notification sent to {TopicArn}", _settings.NotificationTopicArn);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending topic notification to {TopicArn}", _settings.NotificationTopicArn);
            throw;
        }
    }

    /// <summary>
    /// Sends a message to an SQS queue
    /// </summary>
    public async Task<SendMessageResponse> SendQueueMessageAsync(string message, Dictionary<string, object>? attributes = null)
    {
        try
        {
            var request = new SendMessageRequest
            {
                QueueUrl = _settings.NotificationQueueUrl,
                MessageBody = message
            };

            if (attributes != null)
            {
                request.MessageAttributes = attributes.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new Amazon.SQS.Model.MessageAttributeValue
                    {
                        DataType = "String",
                        StringValue = kvp.Value.ToString()
                    }
                );
            }

            var response = await _sqsClient.SendMessageAsync(request);
            _logger.LogInformation("Queue message sent to {QueueUrl}", _settings.NotificationQueueUrl);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending queue message to {QueueUrl}", _settings.NotificationQueueUrl);
            throw;
        }
    }

    /// <summary>
    /// Creates a platform endpoint for a device
    /// </summary>
    public async Task<string> CreatePlatformEndpointAsync(string platformApplicationArn, string deviceToken, string userId)
    {
        try
        {
            var request = new CreatePlatformEndpointRequest
            {
                PlatformApplicationArn = platformApplicationArn,
                Token = deviceToken,
                CustomUserData = userId
            };

            var response = await _snsClient.CreatePlatformEndpointAsync(request);
            _logger.LogInformation("Platform endpoint created for user {UserId} with token {DeviceToken}", userId, deviceToken);
            return response.EndpointArn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating platform endpoint for user {UserId} with token {DeviceToken}", userId, deviceToken);
            throw;
        }
    }

    /// <summary>
    /// Subscribes an endpoint to a topic
    /// </summary>
    public async Task<string> SubscribeEndpointToTopicAsync(string endpointArn)
    {
        try
        {
            var request = new SubscribeRequest
            {
                TopicArn = _settings.NotificationTopicArn,
                Protocol = "application",
                Endpoint = endpointArn
            };

            var response = await _snsClient.SubscribeAsync(request);
            _logger.LogInformation("Endpoint {EndpointArn} subscribed to topic {TopicArn}", endpointArn, _settings.NotificationTopicArn);
            return response.SubscriptionArn;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error subscribing endpoint {EndpointArn} to topic {TopicArn}", endpointArn, _settings.NotificationTopicArn);
            throw;
        }
    }

    /// <summary>
    /// Creates a database notification and sends it through appropriate channels
    /// </summary>
    public async Task CreateAndSendNotificationAsync(
        api.Models.Notification notification,
        bool sendEmail = false,
        bool sendPush = false,
        string? emailSubject = null,
        string? emailHtmlBody = null,
        string? pushTargetArn = null)
    {
        // Save notification to database
        // This would typically be done through a repository or DbContext
        // For now, we'll just log it
        _logger.LogInformation("Notification created: {Title} for user {UserId}", notification.Title, notification.UserId);

        // Send email if requested
        if (sendEmail && !string.IsNullOrEmpty(emailSubject) && !string.IsNullOrEmpty(emailHtmlBody))
        {
            // Get user's email from database
            // For now, we'll just use a placeholder
            string userEmail = "user@example.com";
            await SendEmailAsync(userEmail, emailSubject, emailHtmlBody);
        }

        // Send push notification if requested
        if (sendPush && !string.IsNullOrEmpty(pushTargetArn))
        {
            await SendPushNotificationAsync(pushTargetArn, notification.Title, notification.Content);
        }

        // Queue notification for processing
        var notificationData = new
        {
            notification.Id,
            notification.UserId,
            notification.Type,
            notification.Title,
            notification.Content,
            notification.SenderId,
            notification.RedirectUrl
        };

        await SendQueueMessageAsync(JsonSerializer.Serialize(notificationData), new Dictionary<string, object>
        {
            { "NotificationType", notification.Type }
        });
    }
}

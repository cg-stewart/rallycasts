namespace api.Services;

/// <summary>
/// Settings for AWS notification services (SNS, SES, SQS)
/// </summary>
public class NotificationSettings
{
    public string Region { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string NotificationTopicArn { get; set; } = string.Empty;
    public string NotificationQueueUrl { get; set; } = string.Empty;
    public string IosPlatformApplicationArn { get; set; } = string.Empty;
    public string AndroidPlatformApplicationArn { get; set; } = string.Empty;
}

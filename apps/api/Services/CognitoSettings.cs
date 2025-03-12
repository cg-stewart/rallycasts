namespace api.Services;

/// <summary>
/// Settings for AWS Cognito authentication
/// </summary>
public class CognitoSettings
{
    public string Region { get; set; } = string.Empty;
    public string UserPoolId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
}

using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace api.Services;

/// <summary>
/// Service for handling AWS Cognito authentication
/// </summary>
public class AuthService
{
    private readonly IAmazonCognitoIdentityProvider _cognitoClient;
    private readonly CognitoSettings _cognitoSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IAmazonCognitoIdentityProvider cognitoClient,
        IOptions<CognitoSettings> cognitoSettings,
        ILogger<AuthService> logger)
    {
        _cognitoClient = cognitoClient;
        _cognitoSettings = cognitoSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Registers a new user in Cognito
    /// </summary>
    public async Task<SignUpResponse> RegisterUserAsync(string email, string password, string firstName, string lastName)
    {
        try
        {
            var request = new SignUpRequest
            {
                ClientId = _cognitoSettings.ClientId,
                Password = password,
                Username = email,
                UserAttributes = new List<AttributeType>
                {
                    new AttributeType { Name = "email", Value = email },
                    new AttributeType { Name = "given_name", Value = firstName },
                    new AttributeType { Name = "family_name", Value = lastName }
                }
            };

            var response = await _cognitoClient.SignUpAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Confirms a user's registration using the confirmation code
    /// </summary>
    public async Task<ConfirmSignUpResponse> ConfirmSignUpAsync(string email, string confirmationCode)
    {
        try
        {
            var request = new ConfirmSignUpRequest
            {
                ClientId = _cognitoSettings.ClientId,
                Username = email,
                ConfirmationCode = confirmationCode
            };

            var response = await _cognitoClient.ConfirmSignUpAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming user registration in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Authenticates a user and returns the authentication tokens
    /// </summary>
    public async Task<AuthenticationResultType> LoginAsync(string email, string password)
    {
        try
        {
            var request = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.USER_PASSWORD_AUTH,
                ClientId = _cognitoSettings.ClientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "USERNAME", email },
                    { "PASSWORD", password }
                }
            };

            var response = await _cognitoClient.InitiateAuthAsync(request);
            return response.AuthenticationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating user in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Refreshes the authentication tokens using a refresh token
    /// </summary>
    public async Task<AuthenticationResultType> RefreshTokenAsync(string refreshToken)
    {
        try
        {
            var request = new InitiateAuthRequest
            {
                AuthFlow = AuthFlowType.REFRESH_TOKEN_AUTH,
                ClientId = _cognitoSettings.ClientId,
                AuthParameters = new Dictionary<string, string>
                {
                    { "REFRESH_TOKEN", refreshToken }
                }
            };

            var response = await _cognitoClient.InitiateAuthAsync(request);
            return response.AuthenticationResult;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Gets the user attributes from Cognito
    /// </summary>
    public async Task<List<AttributeType>> GetUserAttributesAsync(string accessToken)
    {
        try
        {
            var request = new GetUserRequest
            {
                AccessToken = accessToken
            };

            var response = await _cognitoClient.GetUserAsync(request);
            return response.UserAttributes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user attributes from Cognito");
            throw;
        }
    }

    /// <summary>
    /// Updates user attributes in Cognito
    /// </summary>
    public async Task<UpdateUserAttributesResponse> UpdateUserAttributesAsync(string accessToken, Dictionary<string, string> attributes)
    {
        try
        {
            var userAttributes = attributes.Select(a => new AttributeType { Name = a.Key, Value = a.Value }).ToList();

            var request = new UpdateUserAttributesRequest
            {
                AccessToken = accessToken,
                UserAttributes = userAttributes
            };

            var response = await _cognitoClient.UpdateUserAttributesAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user attributes in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Changes the user's password in Cognito
    /// </summary>
    public async Task<ChangePasswordResponse> ChangePasswordAsync(string accessToken, string oldPassword, string newPassword)
    {
        try
        {
            var request = new ChangePasswordRequest
            {
                AccessToken = accessToken,
                PreviousPassword = oldPassword,
                ProposedPassword = newPassword
            };

            var response = await _cognitoClient.ChangePasswordAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Initiates the forgot password flow in Cognito
    /// </summary>
    public async Task<ForgotPasswordResponse> ForgotPasswordAsync(string email)
    {
        try
        {
            var request = new ForgotPasswordRequest
            {
                ClientId = _cognitoSettings.ClientId,
                Username = email
            };

            var response = await _cognitoClient.ForgotPasswordAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating forgot password flow in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Confirms the new password using the confirmation code
    /// </summary>
    public async Task<ConfirmForgotPasswordResponse> ConfirmForgotPasswordAsync(string email, string confirmationCode, string newPassword)
    {
        try
        {
            var request = new ConfirmForgotPasswordRequest
            {
                ClientId = _cognitoSettings.ClientId,
                Username = email,
                ConfirmationCode = confirmationCode,
                Password = newPassword
            };

            var response = await _cognitoClient.ConfirmForgotPasswordAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming forgot password in Cognito");
            throw;
        }
    }

    /// <summary>
    /// Signs out the user from Cognito
    /// </summary>
    public async Task<GlobalSignOutResponse> SignOutAsync(string accessToken)
    {
        try
        {
            var request = new GlobalSignOutRequest
            {
                AccessToken = accessToken
            };

            var response = await _cognitoClient.GlobalSignOutAsync(request);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error signing out user from Cognito");
            throw;
        }
    }
}

using api.Data;
using api.Models;
using api.Services;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Xunit;

namespace api.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IAmazonCognitoIdentityProvider> _cognitoClientMock;
    private readonly Mock<IOptions<CognitoSettings>> _cognitoSettingsMock;
    private readonly Mock<ILogger<AuthService>> _loggerMock;
    private readonly ApplicationDbContext _dbContext;
    private readonly AuthService _authService;

    public AuthServiceTests()
    {
        _cognitoClientMock = new Mock<IAmazonCognitoIdentityProvider>();
        _cognitoSettingsMock = new Mock<IOptions<CognitoSettings>>();
        _loggerMock = new Mock<ILogger<AuthService>>();
        _dbContext = TestHelper.CreateInMemoryDbContext();

        _cognitoSettingsMock.Setup(x => x.Value).Returns(new CognitoSettings
        {
            Region = "us-east-1",
            UserPoolId = "us-east-1_testpool",
            AppClientId = "testclientid",
            AppClientSecret = "testclientsecret"
        });

        _authService = new AuthService(
            _cognitoClientMock.Object,
            _cognitoSettingsMock.Object,
            _dbContext,
            _loggerMock.Object);
    }

    [Fact]
    public async Task RegisterUser_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        _cognitoClientMock.Setup(x => x.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SignUpResponse
            {
                UserConfirmed = false,
                UserSub = "test-user-id"
            });

        // Act
        var result = await _authService.RegisterUserAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("User registered successfully. Please check your email for verification code.", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("test-user-id", result.Data.UserId);
        Assert.False(result.Data.IsConfirmed);

        // Verify Cognito client was called with correct parameters
        _cognitoClientMock.Verify(x => x.SignUpAsync(
            It.Is<SignUpRequest>(r => 
                r.Username == request.Email && 
                r.Password == request.Password && 
                r.ClientId == _cognitoSettingsMock.Object.Value.AppClientId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RegisterUser_UserAlreadyExists_ReturnsError()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "Existing",
            LastName = "User"
        };

        _cognitoClientMock.Setup(x => x.SignUpAsync(It.IsAny<SignUpRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UsernameExistsException("User already exists"));

        // Act
        var result = await _authService.RegisterUserAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User with this email already exists", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ConfirmRegistration_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var request = new ConfirmRegistrationRequest
        {
            Email = "test@example.com",
            ConfirmationCode = "123456"
        };

        _cognitoClientMock.Setup(x => x.ConfirmSignUpAsync(It.IsAny<Amazon.CognitoIdentityProvider.Model.ConfirmSignUpRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmSignUpResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await _authService.ConfirmRegistrationAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("User registration confirmed successfully", result.Message);

        // Verify Cognito client was called with correct parameters
        _cognitoClientMock.Verify(x => x.ConfirmSignUpAsync(
            It.Is<Amazon.CognitoIdentityProvider.Model.ConfirmSignUpRequest>(r => 
                r.Username == request.Email && 
                r.ConfirmationCode == request.ConfirmationCode && 
                r.ClientId == _cognitoSettingsMock.Object.Value.AppClientId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokens()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        _cognitoClientMock.Setup(x => x.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType
                {
                    IdToken = "test-id-token",
                    AccessToken = "test-access-token",
                    RefreshToken = "test-refresh-token",
                    ExpiresIn = 3600
                }
            });

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Login successful", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("test-id-token", result.Data.IdToken);
        Assert.Equal("test-access-token", result.Data.AccessToken);
        Assert.Equal("test-refresh-token", result.Data.RefreshToken);
        Assert.Equal(3600, result.Data.ExpiresIn);

        // Verify Cognito client was called with correct parameters
        _cognitoClientMock.Verify(x => x.InitiateAuthAsync(
            It.Is<InitiateAuthRequest>(r => 
                r.AuthFlow == AuthFlowType.USER_PASSWORD_AUTH && 
                r.ClientId == _cognitoSettingsMock.Object.Value.AppClientId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsError()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        _cognitoClientMock.Setup(x => x.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new NotAuthorizedException("Incorrect username or password"));

        // Act
        var result = await _authService.LoginAsync(request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password", result.Message);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsNewTokens()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        _cognitoClientMock.Setup(x => x.InitiateAuthAsync(It.IsAny<InitiateAuthRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new InitiateAuthResponse
            {
                AuthenticationResult = new AuthenticationResultType
                {
                    IdToken = "new-id-token",
                    AccessToken = "new-access-token",
                    ExpiresIn = 3600
                }
            });

        // Act
        var result = await _authService.RefreshTokenAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Token refreshed successfully", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("new-id-token", result.Data.IdToken);
        Assert.Equal("new-access-token", result.Data.AccessToken);
        Assert.Equal(3600, result.Data.ExpiresIn);

        // Verify Cognito client was called with correct parameters
        _cognitoClientMock.Verify(x => x.InitiateAuthAsync(
            It.Is<InitiateAuthRequest>(r => 
                r.AuthFlow == AuthFlowType.REFRESH_TOKEN_AUTH && 
                r.ClientId == _cognitoSettingsMock.Object.Value.AppClientId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsSuccess()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        _cognitoClientMock.Setup(x => x.ForgotPasswordAsync(It.IsAny<Amazon.CognitoIdentityProvider.Model.ForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ForgotPasswordResponse
            {
                CodeDeliveryDetails = new CodeDeliveryDetailsType
                {
                    Destination = "t***@e***.com",
                    DeliveryMedium = DeliveryMediumType.EMAIL,
                    AttributeName = "email"
                }
            });

        // Act
        var result = await _authService.ForgotPasswordAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Password reset code sent to your email", result.Message);
        Assert.NotNull(result.Data);
        Assert.Equal("t***@e***.com", result.Data.CodeDeliveryDestination);

        // Verify Cognito client was called with correct parameters
        _cognitoClientMock.Verify(x => x.ForgotPasswordAsync(
            It.Is<Amazon.CognitoIdentityProvider.Model.ForgotPasswordRequest>(r => 
                r.Username == request.Email && 
                r.ClientId == _cognitoSettingsMock.Object.Value.AppClientId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ValidInput_ReturnsSuccess()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            ConfirmationCode = "123456",
            NewPassword = "NewPassword123!"
        };

        _cognitoClientMock.Setup(x => x.ConfirmForgotPasswordAsync(It.IsAny<ConfirmForgotPasswordRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConfirmForgotPasswordResponse
            {
                HttpStatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await _authService.ResetPasswordAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Password reset successfully", result.Message);

        // Verify Cognito client was called with correct parameters
        _cognitoClientMock.Verify(x => x.ConfirmForgotPasswordAsync(
            It.Is<ConfirmForgotPasswordRequest>(r => 
                r.Username == request.Email && 
                r.ConfirmationCode == request.ConfirmationCode && 
                r.Password == request.NewPassword && 
                r.ClientId == _cognitoSettingsMock.Object.Value.AppClientId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }
}

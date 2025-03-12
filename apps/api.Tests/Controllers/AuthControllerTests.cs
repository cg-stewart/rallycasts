using api.Controllers;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using System.Security.Claims;
using Xunit;

namespace api.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<AuthService> _authServiceMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _authServiceMock = new Mock<AuthService>();
        _loggerMock = new Mock<ILogger<AuthController>>();
        _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);

        // Setup controller context
        var user = new ClaimsPrincipal(new ClaimsIdentity(new Claim[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@example.com")
        }));

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };
    }

    [Fact]
    public async Task Register_ValidInput_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        var serviceResponse = new ServiceResponse<RegisterResponse>
        {
            IsSuccess = true,
            Message = "User registered successfully. Please check your email for verification code.",
            Data = new RegisterResponse
            {
                UserId = "test-user-id",
                IsConfirmed = false
            }
        };

        _authServiceMock.Setup(x => x.RegisterUserAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<RegisterResponse>>(okResult.Value);
        Assert.True(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
        Assert.Equal(serviceResponse.Data.UserId, returnValue.Data.UserId);
    }

    [Fact]
    public async Task Register_InvalidInput_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            FirstName = "",
            LastName = ""
        };

        _controller.ModelState.AddModelError("Email", "Invalid email format");
        _controller.ModelState.AddModelError("Password", "Password too weak");

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);
    }

    [Fact]
    public async Task Register_ServiceError_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "existing@example.com",
            Password = "Password123!",
            FirstName = "Existing",
            LastName = "User"
        };

        var serviceResponse = new ServiceResponse<RegisterResponse>
        {
            IsSuccess = false,
            Message = "User with this email already exists",
            Data = null
        };

        _authServiceMock.Setup(x => x.RegisterUserAsync(It.IsAny<RegisterRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<RegisterResponse>>(badRequestResult.Value);
        Assert.False(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
    }

    [Fact]
    public async Task ConfirmRegistration_ValidInput_ReturnsOk()
    {
        // Arrange
        var request = new ConfirmRegistrationRequest
        {
            Email = "test@example.com",
            ConfirmationCode = "123456"
        };

        var serviceResponse = new ServiceResponse<object>
        {
            IsSuccess = true,
            Message = "User registration confirmed successfully",
            Data = null
        };

        _authServiceMock.Setup(x => x.ConfirmRegistrationAsync(It.IsAny<ConfirmRegistrationRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.ConfirmRegistration(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<object>>(okResult.Value);
        Assert.True(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "Password123!"
        };

        var serviceResponse = new ServiceResponse<LoginResponse>
        {
            IsSuccess = true,
            Message = "Login successful",
            Data = new LoginResponse
            {
                IdToken = "test-id-token",
                AccessToken = "test-access-token",
                RefreshToken = "test-refresh-token",
                ExpiresIn = 3600
            }
        };

        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<LoginResponse>>(okResult.Value);
        Assert.True(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
        Assert.Equal(serviceResponse.Data.IdToken, returnValue.Data.IdToken);
        Assert.Equal(serviceResponse.Data.AccessToken, returnValue.Data.AccessToken);
        Assert.Equal(serviceResponse.Data.RefreshToken, returnValue.Data.RefreshToken);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsUnauthorized()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        };

        var serviceResponse = new ServiceResponse<LoginResponse>
        {
            IsSuccess = false,
            Message = "Invalid email or password",
            Data = null
        };

        _authServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.Login(request);

        // Assert
        var unauthorizedResult = Assert.IsType<UnauthorizedObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<LoginResponse>>(unauthorizedResult.Value);
        Assert.False(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
    }

    [Fact]
    public async Task RefreshToken_ValidToken_ReturnsOk()
    {
        // Arrange
        var request = new RefreshTokenRequest
        {
            RefreshToken = "valid-refresh-token"
        };

        var serviceResponse = new ServiceResponse<RefreshTokenResponse>
        {
            IsSuccess = true,
            Message = "Token refreshed successfully",
            Data = new RefreshTokenResponse
            {
                IdToken = "new-id-token",
                AccessToken = "new-access-token",
                ExpiresIn = 3600
            }
        };

        _authServiceMock.Setup(x => x.RefreshTokenAsync(It.IsAny<RefreshTokenRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.RefreshToken(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<RefreshTokenResponse>>(okResult.Value);
        Assert.True(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
        Assert.Equal(serviceResponse.Data.IdToken, returnValue.Data.IdToken);
        Assert.Equal(serviceResponse.Data.AccessToken, returnValue.Data.AccessToken);
    }

    [Fact]
    public async Task ForgotPassword_ValidEmail_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequest
        {
            Email = "test@example.com"
        };

        var serviceResponse = new ServiceResponse<ForgotPasswordResponse>
        {
            IsSuccess = true,
            Message = "Password reset code sent to your email",
            Data = new ForgotPasswordResponse
            {
                CodeDeliveryDestination = "t***@e***.com"
            }
        };

        _authServiceMock.Setup(x => x.ForgotPasswordAsync(It.IsAny<ForgotPasswordRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<ForgotPasswordResponse>>(okResult.Value);
        Assert.True(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
        Assert.Equal(serviceResponse.Data.CodeDeliveryDestination, returnValue.Data.CodeDeliveryDestination);
    }

    [Fact]
    public async Task ResetPassword_ValidInput_ReturnsOk()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            ConfirmationCode = "123456",
            NewPassword = "NewPassword123!"
        };

        var serviceResponse = new ServiceResponse<object>
        {
            IsSuccess = true,
            Message = "Password reset successfully",
            Data = null
        };

        _authServiceMock.Setup(x => x.ResetPasswordAsync(It.IsAny<ResetPasswordRequest>()))
            .ReturnsAsync(serviceResponse);

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnValue = Assert.IsType<ServiceResponse<object>>(okResult.Value);
        Assert.True(returnValue.IsSuccess);
        Assert.Equal(serviceResponse.Message, returnValue.Message);
    }
}

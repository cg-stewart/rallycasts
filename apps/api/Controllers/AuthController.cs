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
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AuthService authService,
        ApplicationDbContext dbContext,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _dbContext = dbContext;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            // Check if user already exists
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { Message = "User with this email already exists" });
            }

            // Register user in Cognito
            var signUpResponse = await _authService.RegisterUserAsync(
                request.Email,
                request.Password,
                request.FirstName,
                request.LastName);

            // Create user in our database
            var user = new User
            {
                FirstName = request.FirstName,
                LastName = request.LastName,
                Email = request.Email,
                CognitoSub = signUpResponse.UserSub,
                Age = request.Age,
                Rank = request.Rank ?? string.Empty,
                City = request.City ?? string.Empty,
                State = request.State ?? string.Empty,
                Zip = request.Zip ?? string.Empty,
                Phone = request.Phone ?? string.Empty,
                Bio = request.Bio ?? string.Empty
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            return Ok(new
            {
                Message = "User registered successfully. Please check your email for confirmation code.",
                UserId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering user");
            return StatusCode(500, new { Message = "An error occurred while registering the user" });
        }
    }

    [HttpPost("confirm")]
    public async Task<IActionResult> ConfirmRegistration([FromBody] ConfirmRegistrationRequest request)
    {
        try
        {
            // Confirm user registration in Cognito
            await _authService.ConfirmSignUpAsync(request.Email, request.ConfirmationCode);

            return Ok(new { Message = "User registration confirmed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error confirming user registration");
            return StatusCode(500, new { Message = "An error occurred while confirming the registration" });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            // Authenticate user in Cognito
            var authResult = await _authService.LoginAsync(request.Email, request.Password);

            // Get user from our database
            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            return Ok(new
            {
                AccessToken = authResult.AccessToken,
                RefreshToken = authResult.RefreshToken,
                ExpiresIn = authResult.ExpiresIn,
                UserId = user.Id
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging in user");
            return StatusCode(500, new { Message = "An error occurred while logging in" });
        }
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            // Refresh token in Cognito
            var authResult = await _authService.RefreshTokenAsync(request.RefreshToken);

            return Ok(new
            {
                AccessToken = authResult.AccessToken,
                ExpiresIn = authResult.ExpiresIn
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { Message = "An error occurred while refreshing the token" });
        }
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        try
        {
            // Get access token from Authorization header
            var accessToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Sign out user from Cognito
            await _authService.SignOutAsync(accessToken);

            return Ok(new { Message = "User logged out successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging out user");
            return StatusCode(500, new { Message = "An error occurred while logging out" });
        }
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        try
        {
            // Initiate forgot password flow in Cognito
            await _authService.ForgotPasswordAsync(request.Email);

            return Ok(new { Message = "Password reset code sent to your email" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating forgot password flow");
            return StatusCode(500, new { Message = "An error occurred while initiating the password reset" });
        }
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        try
        {
            // Confirm forgot password in Cognito
            await _authService.ConfirmForgotPasswordAsync(request.Email, request.ConfirmationCode, request.NewPassword);

            return Ok(new { Message = "Password reset successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting password");
            return StatusCode(500, new { Message = "An error occurred while resetting the password" });
        }
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            // Get access token from Authorization header
            var accessToken = HttpContext.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

            // Change password in Cognito
            await _authService.ChangePasswordAsync(accessToken, request.OldPassword, request.NewPassword);

            return Ok(new { Message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error changing password");
            return StatusCode(500, new { Message = "An error occurred while changing the password" });
        }
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        try
        {
            // Get user ID from claims
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null)
            {
                return Unauthorized(new { Message = "User not authenticated" });
            }

            int userId = int.Parse(userIdClaim.Value);

            // Get user from database
            var user = await _dbContext.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found" });
            }

            return Ok(new
            {
                user.Id,
                user.FirstName,
                user.LastName,
                user.Email,
                user.Age,
                user.Rank,
                user.City,
                user.State,
                user.Zip,
                user.Phone,
                user.Bio,
                user.FacebookLink,
                user.TwitterLink,
                user.InstagramLink,
                user.YoutubeLink,
                user.TikTokLink,
                user.WebsiteLink,
                user.ProfilePictureUrl,
                user.CreatedAt,
                user.UpdatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting current user");
            return StatusCode(500, new { Message = "An error occurred while getting the user" });
        }
    }
}

// Request models
public class RegisterRequest
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Age { get; set; }
    public string? Rank { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Zip { get; set; }
    public string? Phone { get; set; }
    public string? Bio { get; set; }
}

public class ConfirmRegistrationRequest
{
    public string Email { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
}

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

public class ForgotPasswordRequest
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordRequest
{
    public string Email { get; set; } = string.Empty;
    public string ConfirmationCode { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

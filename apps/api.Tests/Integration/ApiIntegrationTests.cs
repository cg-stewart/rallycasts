using api.Data;
using api.DTOs;
using api.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Xunit;

namespace api.Tests.Integration;

public class ApiIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly CustomWebApplicationFactory _factory;

    public ApiIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetUsers_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/users");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_ExistingUser_ReturnsUser()
    {
        // Act
        var response = await _client.GetAsync("/api/users/1");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var user = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(user);
        Assert.Equal(1, user.Id);
        Assert.Equal("John", user.FirstName);
        Assert.Equal("Doe", user.LastName);
    }

    [Fact]
    public async Task GetUserById_NonExistingUser_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/api/users/999");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_ValidInput_ReturnsCreatedUser()
    {
        // Arrange
        var newUser = new User
        {
            FirstName = "New",
            LastName = "User",
            Email = "new.user@example.com",
            ProfilePictureUrl = "https://example.com/profile.jpg"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(newUser),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/users", content);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var createdUser = await response.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(createdUser);
        Assert.NotEqual(0, createdUser.Id);
        Assert.Equal(newUser.FirstName, createdUser.FirstName);
        Assert.Equal(newUser.LastName, createdUser.LastName);
        Assert.Equal(newUser.Email, createdUser.Email);
    }

    [Fact]
    public async Task UpdateUser_ValidInput_ReturnsNoContent()
    {
        // Arrange
        var updateUser = new User
        {
            Id = 2,
            FirstName = "Jane",
            LastName = "Updated",
            Email = "jane.updated@example.com",
            ProfilePictureUrl = "https://example.com/updated.jpg"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(updateUser),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PutAsync("/api/users/2", content);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the update
        var getResponse = await _client.GetAsync("/api/users/2");
        getResponse.EnsureSuccessStatusCode();

        var updatedUser = await getResponse.Content.ReadFromJsonAsync<User>();
        Assert.NotNull(updatedUser);
        Assert.Equal(updateUser.FirstName, updatedUser.FirstName);
        Assert.Equal(updateUser.LastName, updatedUser.LastName);
        Assert.Equal(updateUser.Email, updatedUser.Email);
    }

    [Fact]
    public async Task DeleteUser_ExistingUser_ReturnsNoContent()
    {
        // Act
        var response = await _client.DeleteAsync("/api/users/3");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify the deletion
        var getResponse = await _client.GetAsync("/api/users/3");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task GetVideos_ReturnsSuccessStatusCode()
    {
        // Act
        var response = await _client.GetAsync("/api/videos");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetVideoById_ExistingVideo_ReturnsVideo()
    {
        // Act
        var response = await _client.GetAsync("/api/videos/1");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var video = await response.Content.ReadFromJsonAsync<Video>();
        Assert.NotNull(video);
        Assert.Equal(1, video.Id);
        Assert.Equal("First Video", video.Title);
    }

    [Fact]
    public async Task GetPhotoById_ExistingPhoto_ReturnsPhoto()
    {
        // Act
        var response = await _client.GetAsync("/api/photos/1");

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var photo = await response.Content.ReadFromJsonAsync<Photo>();
        Assert.NotNull(photo);
        Assert.Equal(1, photo.Id);
        Assert.Equal("First Photo", photo.Title);
    }

    [Fact]
    public async Task RegisterUser_ValidInput_ReturnsCreated()
    {
        // Arrange
        var registerDto = new RegisterDto
        {
            Email = "test.user@example.com",
            Password = "Password123!",
            FirstName = "Test",
            LastName = "User"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registerDto),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/register", content);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var loginDto = new LoginDto
        {
            Email = "john.doe@example.com",
            Password = "Password123!"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(loginDto),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await _client.PostAsync("/api/auth/login", content);

        // Assert
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("token", responseContent.ToLower());
    }

    [Fact]
    public async Task GetUserVideos_ExistingUser_ReturnsVideos()
    {
        // Act
        var response = await _client.GetAsync("/api/users/1/videos");

        // Assert
        response.EnsureSuccessStatusCode();
        var videos = await response.Content.ReadFromJsonAsync<List<Video>>();
        Assert.NotNull(videos);
        Assert.NotEmpty(videos);
    }

    [Fact]
    public async Task GetUserPhotos_ExistingUser_ReturnsPhotos()
    {
        // Act
        var response = await _client.GetAsync("/api/users/1/photos");

        // Assert
        response.EnsureSuccessStatusCode();
        var photos = await response.Content.ReadFromJsonAsync<List<Photo>>();
        Assert.NotNull(photos);
        Assert.NotEmpty(photos);
    }

    [Fact]
    public async Task SearchUsers_WithValidQuery_ReturnsMatchingUsers()
    {
        // Act
        var response = await _client.GetAsync("/api/users/search?q=John");

        // Assert
        response.EnsureSuccessStatusCode();
        var users = await response.Content.ReadFromJsonAsync<List<User>>();
        Assert.NotNull(users);
        Assert.NotEmpty(users);
        Assert.Contains(users, u => u.FirstName.Contains("John") || u.LastName.Contains("John"));
    }

    [Fact]
    public async Task SearchVideos_WithValidQuery_ReturnsMatchingVideos()
    {
        // Act
        var response = await _client.GetAsync("/api/videos/search?q=First");

        // Assert
        response.EnsureSuccessStatusCode();
        var videos = await response.Content.ReadFromJsonAsync<List<Video>>();
        Assert.NotNull(videos);
        Assert.NotEmpty(videos);
        Assert.Contains(videos, v => v.Title.Contains("First") || v.Description.Contains("First"));
    }

    [Fact]
    public async Task GetUserFollowers_ExistingUser_ReturnsFollowers()
    {
        // Act
        var response = await _client.GetAsync("/api/social/followers/2");

        // Assert
        response.EnsureSuccessStatusCode();
        var followers = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(followers);
    }

    [Fact]
    public async Task GetUserFollowing_ExistingUser_ReturnsFollowing()
    {
        // Act
        var response = await _client.GetAsync("/api/social/following/1");

        // Assert
        response.EnsureSuccessStatusCode();
        var following = await response.Content.ReadFromJsonAsync<List<object>>();
        Assert.NotNull(following);
    }
}

// Custom WebApplicationFactory for integration tests
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove the existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add in-memory database for testing
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            // Ensure the database is created and seeded
            db.Database.EnsureCreated();
            SeedTestDatabase(db);
        });
    }

    private void SeedTestDatabase(ApplicationDbContext dbContext)
    {
        // Add test users
        var users = new List<User>
        {
            new User
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                ProfilePictureUrl = "https://example.com/profile1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-30),
                PasswordHash = "$2a$11$k7R1CUYlUooFrqNiLVgBkuuNVYAYEMUHCn5YZXXTUgvk5kKJfJUOK", // Password123!
                CognitoSub = "test-cognito-sub-1"
            },
            new User
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                ProfilePictureUrl = "https://example.com/profile2.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-25)
            },
            new User
            {
                Id = 3,
                FirstName = "Bob",
                LastName = "Johnson",
                Email = "bob.johnson@example.com",
                ProfilePictureUrl = "https://example.com/profile3.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-20)
            }
        };
        dbContext.Users.AddRange(users);

        // Add test videos
        var videos = new List<Video>
        {
            new Video
            {
                Id = 1,
                UserId = 1,
                Title = "First Video",
                Description = "This is the first test video",
                Url = "https://example.com/video1.mp4",
                ThumbnailUrl = "https://example.com/thumbnail1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-15)
            },
            new Video
            {
                Id = 2,
                UserId = 2,
                Title = "Second Video",
                Description = "This is the second test video",
                Url = "https://example.com/video2.mp4",
                ThumbnailUrl = "https://example.com/thumbnail2.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-10)
            }
        };
        dbContext.Videos.AddRange(videos);

        // Add test photos
        var photos = new List<Photo>
        {
            new Photo
            {
                Id = 1,
                UserId = 1,
                Title = "First Photo",
                Description = "This is the first test photo",
                Url = "https://example.com/photo1.jpg",
                ThumbnailUrl = "https://example.com/thumbnail1.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Photo
            {
                Id = 2,
                UserId = 3,
                Title = "Second Photo",
                Description = "This is the second test photo",
                Url = "https://example.com/photo2.jpg",
                ThumbnailUrl = "https://example.com/thumbnail2.jpg",
                CreatedAt = DateTime.UtcNow.AddDays(-2)
            }
        };
        dbContext.Photos.AddRange(photos);

        dbContext.SaveChanges();
    }
}

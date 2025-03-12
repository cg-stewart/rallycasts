using api.Data;
using api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;

namespace api.Tests;

public static class TestHelper
{
    public static ApplicationDbContext CreateInMemoryDbContext()
    {
        var serviceProvider = new ServiceCollection()
            .AddEntityFrameworkInMemoryDatabase()
            .BuildServiceProvider();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .UseInternalServiceProvider(serviceProvider)
            .Options;

        var dbContext = new ApplicationDbContext(options);
        SeedDatabase(dbContext);
        return dbContext;
    }

    private static void SeedDatabase(ApplicationDbContext dbContext)
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
                CreatedAt = DateTime.UtcNow.AddDays(-30)
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

        // Add test follows
        var follows = new List<UserFollow>
        {
            new UserFollow
            {
                Id = 1,
                FollowerId = 1,
                FollowingId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-12)
            },
            new UserFollow
            {
                Id = 2,
                FollowerId = 2,
                FollowingId = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-8)
            },
            new UserFollow
            {
                Id = 3,
                FollowerId = 3,
                FollowingId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-4)
            }
        };
        dbContext.UserFollows.AddRange(follows);

        // Add test likes
        var likes = new List<Like>
        {
            new Like
            {
                Id = 1,
                UserId = 1,
                VideoId = 2,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new Like
            {
                Id = 2,
                UserId = 2,
                PhotoId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-3)
            }
        };
        dbContext.Likes.AddRange(likes);

        // Add test comments
        var comments = new List<Comment>
        {
            new Comment
            {
                Id = 1,
                UserId = 1,
                VideoId = 2,
                Content = "Great video!",
                CreatedAt = DateTime.UtcNow.AddDays(-6)
            },
            new Comment
            {
                Id = 2,
                UserId = 3,
                PhotoId = 1,
                Content = "Nice photo!",
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        dbContext.Comments.AddRange(comments);

        // Add test messages
        var messages = new List<Message>
        {
            new Message
            {
                Id = 1,
                SenderId = 1,
                RecipientId = 2,
                Content = "Hello Jane!",
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Message
            {
                Id = 2,
                SenderId = 2,
                RecipientId = 1,
                Content = "Hi John, how are you?",
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-5),
                CreatedAt = DateTime.UtcNow.AddDays(-5)
            },
            new Message
            {
                Id = 3,
                SenderId = 1,
                RecipientId = 2,
                Content = "I'm good, thanks!",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        dbContext.Messages.AddRange(messages);

        // Add test notifications
        var notifications = new List<Notification>
        {
            new Notification
            {
                Id = 1,
                UserId = 2,
                Type = "like",
                Title = "New Like",
                Content = "John Doe liked your video",
                SenderId = 1,
                RedirectUrl = "/videos/2",
                IsRead = false,
                CreatedAt = DateTime.UtcNow.AddDays(-7)
            },
            new Notification
            {
                Id = 2,
                UserId = 1,
                Type = "comment",
                Title = "New Comment",
                Content = "Bob Johnson commented on your photo",
                SenderId = 3,
                RedirectUrl = "/photos/1",
                IsRead = true,
                ReadAt = DateTime.UtcNow.AddDays(-1),
                CreatedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        dbContext.Notifications.AddRange(notifications);

        dbContext.SaveChanges();
    }
}

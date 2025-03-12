using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }
    
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Caster> Casters { get; set; } = null!;
    public DbSet<Video> Videos { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;
    public DbSet<UserFollow> UserFollows { get; set; } = null!;
    public DbSet<Like> Likes { get; set; } = null!;
    public DbSet<Comment> Comments { get; set; } = null!;
    public DbSet<Message> Messages { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // Configure relationships and constraints for existing models
        modelBuilder.Entity<User>()
            .HasMany(u => u.Videos)
            .WithOne(v => v.User)
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<User>()
            .HasMany(u => u.Photos)
            .WithOne(p => p.User)
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure relationships for UserFollow
        modelBuilder.Entity<UserFollow>()
            .HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<UserFollow>()
            .HasOne(uf => uf.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure relationships for Like
        modelBuilder.Entity<Like>()
            .HasOne(l => l.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Configure relationships for Comment
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Comment>()
            .HasOne(c => c.ParentComment)
            .WithMany(c => c.Replies)
            .HasForeignKey(c => c.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure relationships for Message
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Sender)
            .WithMany(u => u.SentMessages)
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
            
        modelBuilder.Entity<Message>()
            .HasOne(m => m.Recipient)
            .WithMany(u => u.ReceivedMessages)
            .HasForeignKey(m => m.RecipientId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Configure relationships for Notification
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        modelBuilder.Entity<Notification>()
            .HasOne(n => n.Sender)
            .WithMany()
            .HasForeignKey(n => n.SenderId)
            .OnDelete(DeleteBehavior.Restrict);
        
        // Add indexes for better query performance
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();
            
        modelBuilder.Entity<User>()
            .HasIndex(u => u.CognitoSub)
            .IsUnique();
            
        modelBuilder.Entity<Caster>()
            .HasIndex(c => c.Email)
            .IsUnique();
            
        modelBuilder.Entity<Video>()
            .HasIndex(v => v.UserId);
            
        modelBuilder.Entity<Photo>()
            .HasIndex(p => p.UserId);
            
        modelBuilder.Entity<UserFollow>()
            .HasIndex(uf => new { uf.FollowerId, uf.FollowingId })
            .IsUnique();
            
        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.VideoId })
            .IsUnique();
            
        modelBuilder.Entity<Like>()
            .HasIndex(l => new { l.UserId, l.PhotoId })
            .IsUnique();
            
        modelBuilder.Entity<Message>()
            .HasIndex(m => new { m.SenderId, m.RecipientId, m.CreatedAt });
            
        modelBuilder.Entity<Notification>()
            .HasIndex(n => new { n.UserId, n.CreatedAt });
    }
}

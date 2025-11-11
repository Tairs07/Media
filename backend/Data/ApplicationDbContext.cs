using Microsoft.EntityFrameworkCore;
using backend.Models;

namespace backend.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<MediaFile> MediaFiles { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<ChatSession> ChatSessions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 配置User实体
            modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                
                // 唯一索引
                entity.HasIndex(e => e.Username)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Username");
                
                entity.HasIndex(e => e.Email)
                      .IsUnique()
                      .HasDatabaseName("IX_Users_Email");
                
                // 默认值
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("datetime('now', 'utc')");
                
                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);
            });

            // 配置MediaFile实体
            modelBuilder.Entity<MediaFile>(entity =>
            {
                entity.ToTable("MediaFiles");
                
                // 外键关系
                entity.HasOne(m => m.User)
                      .WithMany(u => u.MediaFiles)
                      .HasForeignKey(m => m.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_MediaFiles_Users");
                
                // 索引
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_MediaFiles_UserId");
                
                entity.HasIndex(e => e.FileType)
                      .HasDatabaseName("IX_MediaFiles_FileType");
                
                entity.HasIndex(e => e.UploadedAt)
                      .HasDatabaseName("IX_MediaFiles_UploadedAt");
                
                entity.HasIndex(e => new { e.UserId, e.IsPublic })
                      .HasDatabaseName("IX_MediaFiles_UserId_IsPublic");
                
                // 默认值
                entity.Property(e => e.UploadedAt)
                      .HasDefaultValueSql("GETUTCDATE()");
                
                entity.Property(e => e.ViewCount)
                      .HasDefaultValue(0);
                
                entity.Property(e => e.LikeCount)
                      .HasDefaultValue(0);
                
                entity.Property(e => e.IsPublic)
                      .HasDefaultValue(true);
            });

            // 配置UserSession实体
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.ToTable("UserSessions");
                
                // 外键关系
                entity.HasOne(s => s.User)
                      .WithMany(u => u.Sessions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_UserSessions_Users");
                
                // 索引
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_UserSessions_UserId");
                
                entity.HasIndex(e => e.Token)
                      .IsUnique()
                      .HasDatabaseName("IX_UserSessions_Token");
                
                entity.HasIndex(e => new { e.UserId, e.IsActive })
                      .HasDatabaseName("IX_UserSessions_UserId_IsActive");
                
                // 默认值
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("datetime('now', 'utc')");
                
                entity.Property(e => e.IsActive)
                      .HasDefaultValue(true);
            });

            // 配置ChatSession实体
            modelBuilder.Entity<ChatSession>(entity =>
            {
                entity.ToTable("ChatSessions");
                
                // 外键关系
                entity.HasOne(c => c.User)
                      .WithMany(u => u.ChatSessions)
                      .HasForeignKey(c => c.UserId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_ChatSessions_Users");
                
                // 索引
                entity.HasIndex(e => e.UserId)
                      .HasDatabaseName("IX_ChatSessions_UserId");
                
                entity.HasIndex(e => e.UpdatedAt)
                      .HasDatabaseName("IX_ChatSessions_UpdatedAt");
                
                entity.HasIndex(e => new { e.UserId, e.IsDeleted })
                      .HasDatabaseName("IX_ChatSessions_UserId_IsDeleted");
                
                // 默认值
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("datetime('now', 'utc')");
                
                entity.Property(e => e.UpdatedAt)
                      .HasDefaultValueSql("datetime('now', 'utc')");
                
                entity.Property(e => e.IsDeleted)
                      .HasDefaultValue(false);
                
                entity.Property(e => e.Title)
                      .HasDefaultValue("新对话");
                
                entity.Property(e => e.Model)
                      .HasDefaultValue("qwen-plus");
            });

            // 配置ChatMessage实体
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.ToTable("ChatMessages");
                
                // 外键关系
                entity.HasOne(m => m.Session)
                      .WithMany(s => s.Messages)
                      .HasForeignKey(m => m.SessionId)
                      .OnDelete(DeleteBehavior.Cascade)
                      .HasConstraintName("FK_ChatMessages_ChatSessions");
                
                // 索引
                entity.HasIndex(e => e.SessionId)
                      .HasDatabaseName("IX_ChatMessages_SessionId");
                
                entity.HasIndex(e => e.CreatedAt)
                      .HasDatabaseName("IX_ChatMessages_CreatedAt");
                
                entity.HasIndex(e => new { e.SessionId, e.CreatedAt })
                      .HasDatabaseName("IX_ChatMessages_SessionId_CreatedAt");
                
                // 默认值
                entity.Property(e => e.CreatedAt)
                      .HasDefaultValueSql("datetime('now', 'utc')");
            });
        }
    }
}

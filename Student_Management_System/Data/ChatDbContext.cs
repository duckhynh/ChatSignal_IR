using Microsoft.EntityFrameworkCore;
using Student_Management_System.Models;

namespace Student_Management_System.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
    }

    public DbSet<ChatMessage> Messages { get; set; }
    public DbSet<ChatFile> Files { get; set; }
    public DbSet<ChatRoom> Rooms { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoomId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Sender).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Content).IsRequired();
            entity.Property(e => e.Type).IsRequired();
            entity.Property(e => e.SentAt).IsRequired();
            
            entity.HasOne(e => e.File)
                .WithMany(f => f.Messages)
                .HasForeignKey(e => e.FileId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.RoomId);
        });

        modelBuilder.Entity<ChatFile>(entity =>
        {
            entity.HasKey(e => e.FileId);
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ContentType).IsRequired().HasMaxLength(200);
            entity.Property(e => e.StoragePath).IsRequired().HasMaxLength(1000);
        });

        modelBuilder.Entity<ChatRoom>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.RoomId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CreatedBy).HasMaxLength(100).HasDefaultValue("");
            entity.HasIndex(e => e.RoomId).IsUnique();
        });
    }
}

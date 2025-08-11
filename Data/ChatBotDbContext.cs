using Microsoft.EntityFrameworkCore;
using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Data;

public class ChatBotDbContext : DbContext
{
    public ChatBotDbContext(DbContextOptions<ChatBotDbContext> options) : base(options)
    {
    }

    public DbSet<ChatSession> ChatSessions { get; set; }
    public DbSet<ChatMessage> ChatMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure ChatSession
        modelBuilder.Entity<ChatSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SessionId)
                .IsRequired()
                .HasMaxLength(50);
            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(200);
            entity.HasIndex(e => e.SessionId).IsUnique();
            entity.HasIndex(e => e.CreatedAt);
        });

        // Configure ChatMessage
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(20);
            entity.Property(e => e.Content)
                .IsRequired();
            entity.HasIndex(e => e.Timestamp);
            
            // Foreign key relationship
            entity.HasOne(e => e.ChatSession)
                .WithMany(s => s.Messages)
                .HasForeignKey(e => e.ChatSessionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
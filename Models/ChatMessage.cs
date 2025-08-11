namespace SemanticKernel.ChatBot.Api.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public int ChatSessionId { get; set; }
    public string Role { get; set; } = string.Empty; // "user", "assistant", "system"
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public int? TokenCount { get; set; }

    // Navigation property
    public virtual ChatSession ChatSession { get; set; } = null!;
}
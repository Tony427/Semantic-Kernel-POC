namespace SemanticKernel.ChatBot.Api.Models;

public class ChatSession
{
    public int Id { get; set; }
    public string SessionId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation property
    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
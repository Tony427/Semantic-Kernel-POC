using System.ComponentModel.DataAnnotations;

namespace SemanticKernel.ChatBot.Api.Models;

public class ChatSession
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public bool IsActive { get; set; } = true;

    public virtual ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}
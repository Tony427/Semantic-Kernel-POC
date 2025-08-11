using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public interface IChatHistoryService
{
    Task<ChatSession> CreateSessionAsync(string? title = null);
    Task<ChatSession?> GetSessionAsync(string sessionId);
    Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(int limit = 20);
    Task<bool> UpdateSessionTitleAsync(string sessionId, string title);
    Task<bool> ArchiveSessionAsync(string sessionId);
    
    Task AddMessageAsync(string sessionId, string role, string content, int? tokenCount = null);
    Task<IEnumerable<ChatMessage>> GetMessagesAsync(string sessionId, int limit = 50);
    Task<int> GetMessageCountAsync(string sessionId);
    Task ClearMessagesAsync(string sessionId);
}
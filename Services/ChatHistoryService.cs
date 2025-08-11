using Microsoft.EntityFrameworkCore;
using SemanticKernel.ChatBot.Api.Data;
using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public class ChatHistoryService : IChatHistoryService
{
    private readonly ChatBotDbContext _context;
    private readonly ILogger<ChatHistoryService> _logger;

    public ChatHistoryService(ChatBotDbContext context, ILogger<ChatHistoryService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ChatSession> CreateSessionAsync(string? title = null)
    {
        var sessionId = GenerateSessionId();
        var session = new ChatSession
        {
            SessionId = sessionId,
            Title = title ?? $"Chat Session {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _context.ChatSessions.Add(session);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created new chat session: {SessionId}", sessionId);
        return session;
    }

    public async Task<ChatSession?> GetSessionAsync(string sessionId)
    {
        return await _context.ChatSessions
            .FirstOrDefaultAsync(s => s.SessionId == sessionId && s.IsActive);
    }

    public async Task<IEnumerable<ChatSession>> GetActiveSessionsAsync(int limit = 20)
    {
        return await _context.ChatSessions
            .Where(s => s.IsActive)
            .OrderByDescending(s => s.UpdatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> UpdateSessionTitleAsync(string sessionId, string title)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return false;
        }

        session.Title = title;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated session title: {SessionId}", sessionId);
        return true;
    }

    public async Task<bool> ArchiveSessionAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return false;
        }

        session.IsActive = false;
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Archived session: {SessionId}", sessionId);
        return true;
    }

    public async Task AddMessageAsync(string sessionId, string role, string content, int? tokenCount = null)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            throw new ArgumentException($"Session not found: {sessionId}");
        }

        var message = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = role,
            Content = content,
            Timestamp = DateTime.UtcNow,
            TokenCount = tokenCount
        };

        _context.ChatMessages.Add(message);

        // Update session timestamp
        session.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Added message to session {SessionId}: {Role}", sessionId, role);
    }

    public async Task<IEnumerable<ChatMessage>> GetMessagesAsync(string sessionId, int limit = 50)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return Enumerable.Empty<ChatMessage>();
        }

        return await _context.ChatMessages
            .Where(m => m.ChatSessionId == session.Id)
            .OrderBy(m => m.Timestamp)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<bool> ClearMessagesAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return false;
        }

        var messages = await _context.ChatMessages
            .Where(m => m.ChatSessionId == session.Id)
            .ToListAsync();

        _context.ChatMessages.RemoveRange(messages);
        session.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Cleared messages for session: {SessionId}", sessionId);
        return true;
    }

    public async Task<int> GetMessageCountAsync(string sessionId)
    {
        var session = await GetSessionAsync(sessionId);
        if (session == null)
        {
            return 0;
        }

        return await _context.ChatMessages
            .CountAsync(m => m.ChatSessionId == session.Id);
    }

    private static string GenerateSessionId()
    {
        return DateTime.UtcNow.Ticks.ToString("x")[^12..];
    }
}
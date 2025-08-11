using Microsoft.AspNetCore.Mvc;
using SemanticKernel.ChatBot.Api.Services;

namespace SemanticKernel.ChatBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatHistoryController : ControllerBase
{
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<ChatHistoryController> _logger;

    public ChatHistoryController(IChatHistoryService chatHistoryService, ILogger<ChatHistoryController> logger)
    {
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateSession([FromBody] CreateSessionRequest? request = null)
    {
        try
        {
            var session = await _chatHistoryService.CreateSessionAsync(request?.Title);
            return Ok(new
            {
                sessionId = session.SessionId,
                title = session.Title,
                createdAt = session.CreatedAt,
                message = "Chat session created successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create chat session");
            return StatusCode(500, new { error = "Failed to create chat session" });
        }
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetSessions([FromQuery] int limit = 20)
    {
        try
        {
            var sessions = await _chatHistoryService.GetActiveSessionsAsync(limit);
            return Ok(new
            {
                sessions = sessions.Select(s => new
                {
                    sessionId = s.SessionId,
                    title = s.Title,
                    createdAt = s.CreatedAt,
                    updatedAt = s.UpdatedAt
                }),
                count = sessions.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get chat sessions");
            return StatusCode(500, new { error = "Failed to get chat sessions" });
        }
    }

    [HttpGet("sessions/{sessionId}")]
    public async Task<IActionResult> GetSession(string sessionId)
    {
        try
        {
            var session = await _chatHistoryService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { error = $"Chat session not found: {sessionId}" });
            }

            return Ok(new
            {
                sessionId = session.SessionId,
                title = session.Title,
                createdAt = session.CreatedAt,
                updatedAt = session.UpdatedAt,
                isActive = session.IsActive
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to get session" });
        }
    }

    [HttpPut("sessions/{sessionId}/title")]
    public async Task<IActionResult> UpdateSessionTitle(string sessionId, [FromBody] UpdateTitleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
        {
            return BadRequest(new { error = "Title is required" });
        }

        try
        {
            var updated = await _chatHistoryService.UpdateSessionTitleAsync(sessionId, request.Title);
            if (!updated)
            {
                return NotFound(new { error = $"Chat session not found: {sessionId}" });
            }

            return Ok(new { message = "Session title updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update session title: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to update session title" });
        }
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> ArchiveSession(string sessionId)
    {
        try
        {
            var archived = await _chatHistoryService.ArchiveSessionAsync(sessionId);
            if (!archived)
            {
                return NotFound(new { error = $"Chat session not found: {sessionId}" });
            }

            return Ok(new { message = $"Chat session {sessionId} archived successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to archive session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to archive session" });
        }
    }

    [HttpGet("sessions/{sessionId}/messages")]
    public async Task<IActionResult> GetMessages(string sessionId, [FromQuery] int limit = 50)
    {
        try
        {
            var messages = await _chatHistoryService.GetMessagesAsync(sessionId, limit);
            var session = await _chatHistoryService.GetSessionAsync(sessionId);

            if (session == null)
            {
                return NotFound(new { error = $"Chat session not found: {sessionId}" });
            }

            return Ok(new
            {
                sessionId = sessionId,
                title = session.Title,
                messages = messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content,
                    timestamp = m.Timestamp,
                    tokenCount = m.TokenCount
                }).ToList(),
                messageCount = messages.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get messages for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to get messages" });
        }
    }

    [HttpPost("sessions/{sessionId}/messages")]
    public async Task<IActionResult> AddMessage(string sessionId, [FromBody] AddMessageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Role) || string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest(new { error = "Role and content are required" });
        }

        try
        {
            await _chatHistoryService.AddMessageAsync(sessionId, request.Role, request.Content, request.TokenCount);
            return Ok(new { message = "Message added successfully" });
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add message to session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to add message" });
        }
    }

    [HttpDelete("sessions/{sessionId}/messages")]
    public async Task<IActionResult> ClearMessages(string sessionId)
    {
        try
        {
            var cleared = await _chatHistoryService.ClearMessagesAsync(sessionId);
            if (!cleared)
            {
                return NotFound(new { error = $"Chat session not found: {sessionId}" });
            }

            return Ok(new { message = $"Messages cleared for session {sessionId}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear messages for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to clear messages" });
        }
    }

    [HttpGet("sessions/{sessionId}/messages/count")]
    public async Task<IActionResult> GetMessageCount(string sessionId)
    {
        try
        {
            var count = await _chatHistoryService.GetMessageCountAsync(sessionId);
            return Ok(new { sessionId, messageCount = count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get message count for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to get message count" });
        }
    }
}

public record CreateSessionRequest(string? Title = null);
public record UpdateTitleRequest(string Title);
public record AddMessageRequest(string Role, string Content, int? TokenCount = null);
using Microsoft.AspNetCore.Mvc;
using SemanticKernel.ChatBot.Api.Services;

namespace SemanticKernel.ChatBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly IChatHistoryService _chatHistoryService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatService chatService,
        IChatHistoryService chatHistoryService,
        ILogger<ChatController> logger)
    {
        _chatService = chatService;
        _chatHistoryService = chatHistoryService;
        _logger = logger;
    }

    [HttpPost("sessions/{sessionId}/ask")]
    public async Task<IActionResult> AskQuestion(int sessionId, [FromBody] ChatRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Message))
            {
                return BadRequest("Message cannot be empty");
            }

            _logger.LogInformation("Received chat request for session {SessionId}", sessionId);

            // Verify session exists
            var session = await _chatHistoryService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound($"Chat session {sessionId} not found");
            }

            // Get AI response
            var aiResponse = await _chatService.GetAIResponseAsync(sessionId, request.Message);

            var response = new ChatResponse
            {
                Message = aiResponse,
                SessionId = sessionId,
                Timestamp = DateTime.UtcNow
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request for session {SessionId}", sessionId);
            return StatusCode(500, "An error occurred while processing your request");
        }
    }

    [HttpPost("sessions/{sessionId}/chat")]
    public async Task<IActionResult> Chat(int sessionId, [FromBody] ChatRequest request)
    {
        // Alias for AskQuestion to match the documented API endpoint
        return await AskQuestion(sessionId, request);
    }
}

public class ChatRequest
{
    public string Message { get; set; } = string.Empty;
}

public class ChatResponse
{
    public string Message { get; set; } = string.Empty;
    public int SessionId { get; set; }
    public DateTime Timestamp { get; set; }
}
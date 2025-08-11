using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.ChatBot.Api.Models;
using SemanticKernel.ChatBot.Api.Services;

namespace SemanticKernel.ChatBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IKernelMemoryService _kernelMemoryService;
    private readonly IFileReaderService _fileReaderService;
    private readonly OpenAIConfiguration _openAIConfig;
    private readonly ILogger<ChatController> _logger;
    private readonly Kernel _kernel;

    public ChatController(
        IChatHistoryService chatHistoryService,
        IKernelMemoryService kernelMemoryService,
        IFileReaderService fileReaderService,
        IOptions<OpenAIConfiguration> openAIConfig,
        ILogger<ChatController> logger)
    {
        _chatHistoryService = chatHistoryService;
        _kernelMemoryService = kernelMemoryService;
        _fileReaderService = fileReaderService;
        _openAIConfig = openAIConfig.Value;
        _logger = logger;

        var kernelBuilder = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                modelId: _openAIConfig.Model,
                apiKey: _openAIConfig.ApiKey);
        
        _kernel = kernelBuilder.Build();
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> CreateChatSession([FromBody] CreateChatSessionRequest? request = null)
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
    public async Task<IActionResult> GetChatSessions([FromQuery] int limit = 20)
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

    [HttpPost("sessions/{sessionId}/chat")]
    public async Task<IActionResult> Chat(string sessionId, [FromBody] ChatRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Message))
        {
            return BadRequest(new { error = "Message is required" });
        }

        try
        {
            var session = await _chatHistoryService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { error = $"Chat session not found: {sessionId}" });
            }

            var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();

            var existingMessages = await _chatHistoryService.GetMessagesAsync(sessionId, 50);
            foreach (var msg in existingMessages)
            {
                if (msg.Role.Equals("user", StringComparison.OrdinalIgnoreCase))
                {
                    chatHistory.AddUserMessage(msg.Content);
                }
                else if (msg.Role.Equals("assistant", StringComparison.OrdinalIgnoreCase))
                {
                    chatHistory.AddAssistantMessage(msg.Content);
                }
                else if (msg.Role.Equals("system", StringComparison.OrdinalIgnoreCase))
                {
                    chatHistory.AddSystemMessage(msg.Content);
                }
            }

            var searchResults = await _kernelMemoryService.SearchDocumentsAsync(request.Message, 3, 0.7);
            var contextInfo = "";
            if (searchResults.Any())
            {
                contextInfo = "Based on the following documents:\n\n" + 
                    string.Join("\n---\n", searchResults.Select(r => $"From {r.SourceFile}:\n{r.Content}"));
                chatHistory.AddSystemMessage($"Context information: {contextInfo}");
            }

            chatHistory.AddUserMessage(request.Message);

            var chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();
            var response = await chatCompletionService.GetChatMessageContentAsync(
                chatHistory, 
                new OpenAIPromptExecutionSettings()
                {
                    MaxTokens = request.MaxTokens ?? 1000,
                    Temperature = request.Temperature ?? 0.7
                });

            await _chatHistoryService.AddMessageAsync(sessionId, "user", request.Message);
            await _chatHistoryService.AddMessageAsync(sessionId, "assistant", response.Content ?? "");

            return Ok(new
            {
                sessionId = sessionId,
                response = response.Content,
                contextUsed = searchResults.Any(),
                contextSources = searchResults.Select(r => r.SourceFile).Distinct().ToList(),
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process chat message for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to process chat message" });
        }
    }

    [HttpGet("sessions/{sessionId}/conversation")]
    public async Task<IActionResult> GetConversation(string sessionId, [FromQuery] int limit = 50)
    {
        try
        {
            var session = await _chatHistoryService.GetSessionAsync(sessionId);
            if (session == null)
            {
                return NotFound(new { error = $"Chat session not found: {sessionId}" });
            }

            var messages = await _chatHistoryService.GetMessagesAsync(sessionId, limit);
            
            return Ok(new
            {
                sessionId = sessionId,
                title = session.Title,
                messages = messages.Select(m => new
                {
                    role = m.Role,
                    content = m.Content,
                    timestamp = m.Timestamp
                }).ToList(),
                messageCount = messages.Count()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get conversation for session: {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to get conversation" });
        }
    }

    [HttpDelete("sessions/{sessionId}")]
    public async Task<IActionResult> ArchiveChatSession(string sessionId)
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

    [HttpPut("sessions/{sessionId}/title")]
    public async Task<IActionResult> UpdateSessionTitle(string sessionId, [FromBody] UpdateSessionTitleRequest request)
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

    [HttpGet("documents/status")]
    public async Task<IActionResult> GetDocumentsStatus()
    {
        try
        {
            var documents = await _fileReaderService.GetDocumentsAsync();
            var memoryStatus = await _kernelMemoryService.GetMemoryStatusAsync();
            
            return Ok(new
            {
                documentCount = documents.Count(),
                documentsIndexed = memoryStatus.DocumentsIndexed,
                lastUpdated = documents.Any() ? documents.Max(d => d.LastModified) : (DateTime?)null,
                availableDocuments = documents.Select(d => new
                {
                    fileName = d.FileName,
                    filePath = d.FilePath,
                    lastModified = d.LastModified,
                    size = d.Content.Length
                }).ToList()
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get documents status");
            return StatusCode(500, new { error = "Failed to get documents status" });
        }
    }
}

public record CreateChatSessionRequest(string? Title = null);
public record ChatRequest(
    string Message,
    int? MaxTokens = null,
    double? Temperature = null
);
public record UpdateSessionTitleRequest(string Title);
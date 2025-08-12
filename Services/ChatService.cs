using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using SemanticKernel.ChatBot.Api.Models;
using SemanticKernel.ChatBot.Api.Services;

namespace SemanticKernel.ChatBot.Api.Services;

public class ChatService : IChatService
{
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IKernelMemoryService _memoryService;
    private readonly ILogger<ChatService> _logger;
    private readonly Kernel _kernel;
    private readonly IChatCompletionService _chatCompletionService;
    private readonly OpenAIConfiguration _openAIConfig;

    public ChatService(
        IChatHistoryService chatHistoryService,
        IKernelMemoryService memoryService,
        ILogger<ChatService> logger,
        IOptions<OpenAIConfiguration> openAIConfig)
    {
        _chatHistoryService = chatHistoryService;
        _memoryService = memoryService;
        _logger = logger;
        _openAIConfig = openAIConfig.Value;

        // Initialize Semantic Kernel
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddOpenAIChatCompletion(
            modelId: _openAIConfig.Model,
            apiKey: _openAIConfig.ApiKey);

        _kernel = kernelBuilder.Build();
        _chatCompletionService = _kernel.GetRequiredService<IChatCompletionService>();

        _logger.LogInformation("ChatService initialized with model: {Model}", _openAIConfig.Model);
    }

    public async Task<string> GetAIResponseAsync(string sessionId, string userMessage, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Processing AI response for session {SessionId}", sessionId);

            // Search for relevant context from documents
            var relevantContext = await SearchRelevantContextAsync(userMessage, cancellationToken);

            // Build prompt with context
            var chatHistory = new ChatHistory();
            
            // Add system message with context if available
            if (!string.IsNullOrEmpty(relevantContext))
            {
                chatHistory.AddSystemMessage($"You are a helpful AI assistant. Use the following context to inform your responses when relevant:\n\n{relevantContext}");
            }
            else
            {
                chatHistory.AddSystemMessage("You are a helpful AI assistant.");
            }

            // Add recent conversation history for context (excluding current message)
            var recentMessages = await _chatHistoryService.GetMessagesAsync(sessionId, 10);
            foreach (var message in recentMessages.OrderBy(m => m.Timestamp))
            {
                if (message.Role == "user")
                {
                    chatHistory.AddUserMessage(message.Content);
                }
                else if (message.Role == "assistant")
                {
                    chatHistory.AddAssistantMessage(message.Content);
                }
            }

            // Add current user message
            chatHistory.AddUserMessage(userMessage);

            // Get AI response
            var openAISettings = new OpenAIPromptExecutionSettings
            {
                MaxTokens = _openAIConfig.MaxTokens,
                Temperature = 0.7
            };

            var response = await _chatCompletionService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings: openAISettings,
                cancellationToken: cancellationToken);

            var aiResponse = response.Content ?? "I apologize, but I couldn't generate a response at this time.";

            // Store user message and AI response after successful generation
            await _chatHistoryService.AddMessageAsync(sessionId, "user", userMessage);
            await _chatHistoryService.AddMessageAsync(sessionId, "assistant", aiResponse);

            _logger.LogInformation("AI response generated successfully for session {SessionId}", sessionId);
            return aiResponse;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating AI response for session {SessionId}", sessionId);
            throw;
        }
    }

    private async Task<string> SearchRelevantContextAsync(string userMessage, CancellationToken cancellationToken)
    {
        try
        {
            var searchResult = await _memoryService.SearchAsync(userMessage, 3);
            
            if (!string.IsNullOrEmpty(searchResult))
            {
                return $"Relevant information from documents:\n{searchResult}";
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error searching for relevant context, proceeding without context");
        }

        return string.Empty;
    }
}
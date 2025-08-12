using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public interface IChatService
{
    Task<string> GetAIResponseAsync(int sessionId, string userMessage, CancellationToken cancellationToken = default);
}
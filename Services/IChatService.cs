using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public interface IChatService
{
    Task<string> GetAIResponseAsync(string sessionId, string userMessage, CancellationToken cancellationToken = default);
}
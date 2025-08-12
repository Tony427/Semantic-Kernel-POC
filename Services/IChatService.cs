using SemanticKernelPOC.Models;

namespace SemanticKernelPOC.Services;

public interface IChatService
{
    Task<string> GetAIResponseAsync(int sessionId, string userMessage, CancellationToken cancellationToken = default);
}
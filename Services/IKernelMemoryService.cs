using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public interface IKernelMemoryService
{
    Task InitializeAsync();
    Task LoadDocumentsAsync();
    Task<string> SearchAsync(string query, int limit = 3);
    Task<bool> IsInitializedAsync();
    Task<int> GetDocumentCountAsync();
    Task<DocumentLoadResult> ReloadDocumentsAsync();
    Task<DocumentStatistics> GetDocumentStatisticsAsync();
}
using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public interface IKernelMemoryService
{
    Task InitializeAsync();
    Task LoadDocumentsAsync();
    Task<string> SearchAsync(string query, int limit = 3);
    Task<IEnumerable<SearchResult>> SearchDocumentsAsync(string query, int limit = 3, double minRelevance = 0.7);
    Task<bool> IsInitializedAsync();
    Task<int> GetDocumentCountAsync();
    Task<MemoryStatus> GetMemoryStatusAsync();
}

public record SearchResult(string SourceFile, string Content, double Relevance);
public record MemoryStatus(bool IsInitialized, int DocumentsIndexed, DateTime? LastUpdated);
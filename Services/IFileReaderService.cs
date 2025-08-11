using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public interface IFileReaderService
{
    Task<IEnumerable<DocumentModel>> GetAllDocumentsAsync();
    Task<IEnumerable<DocumentModel>> GetDocumentsAsync();
    Task<DocumentModel?> GetDocumentByFileNameAsync(string fileName);
    Task<bool> RefreshDocumentsAsync();
    Task<int> GetDocumentCountAsync();
}
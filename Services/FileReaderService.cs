using Microsoft.Extensions.Options;
using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public class FileReaderService : IFileReaderService
{
    private readonly SemanticKernelConfiguration _config;
    private readonly ILogger<FileReaderService> _logger;
    private readonly List<DocumentModel> _cachedDocuments = new();
    private DateTime _lastRefresh = DateTime.MinValue;
    private readonly TimeSpan _cacheExpiry = TimeSpan.FromMinutes(5);

    public FileReaderService(
        IOptions<SemanticKernelConfiguration> config,
        ILogger<FileReaderService> logger)
    {
        _config = config.Value;
        _logger = logger;
    }

    public async Task<IEnumerable<DocumentModel>> GetAllDocumentsAsync()
    {
        await RefreshIfNeededAsync();
        return _cachedDocuments.AsReadOnly();
    }

    public async Task<IEnumerable<DocumentModel>> GetDocumentsAsync()
    {
        return await GetAllDocumentsAsync();
    }

    public async Task<DocumentModel?> GetDocumentByFileNameAsync(string fileName)
    {
        await RefreshIfNeededAsync();
        return _cachedDocuments.FirstOrDefault(d => 
            string.Equals(d.FileName, fileName, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<bool> RefreshDocumentsAsync()
    {
        try
        {
            _cachedDocuments.Clear();

            if (!Directory.Exists(_config.DocumentsPath))
            {
                _logger.LogWarning("Documents directory does not exist: {DocumentsPath}", _config.DocumentsPath);
                Directory.CreateDirectory(_config.DocumentsPath);
                return true;
            }

            var txtFiles = Directory.GetFiles(_config.DocumentsPath, "*.txt", SearchOption.TopDirectoryOnly);
            _logger.LogInformation("Found {FileCount} txt files in {DocumentsPath}", txtFiles.Length, _config.DocumentsPath);

            foreach (var filePath in txtFiles)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);
                    var content = await File.ReadAllTextAsync(filePath);

                    var document = new DocumentModel
                    {
                        FileName = fileInfo.Name,
                        FilePath = filePath,
                        Content = content,
                        LastModified = fileInfo.LastWriteTime,
                        FileSizeBytes = fileInfo.Length
                    };

                    _cachedDocuments.Add(document);
                    _logger.LogDebug("Loaded document: {FileName} ({FileSize} bytes)", document.FileName, document.FileSizeBytes);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading file: {FilePath}", filePath);
                }
            }

            _lastRefresh = DateTime.UtcNow;
            _logger.LogInformation("Successfully loaded {DocumentCount} documents", _cachedDocuments.Count);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing documents from: {DocumentsPath}", _config.DocumentsPath);
            return false;
        }
    }

    public async Task<int> GetDocumentCountAsync()
    {
        await RefreshIfNeededAsync();
        return _cachedDocuments.Count;
    }

    private async Task RefreshIfNeededAsync()
    {
        if (DateTime.UtcNow - _lastRefresh > _cacheExpiry)
        {
            await RefreshDocumentsAsync();
        }
    }
}
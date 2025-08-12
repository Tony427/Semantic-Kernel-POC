using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public class KernelMemoryService : IKernelMemoryService
{
    private readonly IKernelMemory _memory;
    private readonly OpenAIConfiguration _openAIConfig;
    private readonly SemanticKernelConfiguration _skConfig;
    private readonly ILogger<KernelMemoryService> _logger;
    private bool _isInitialized = false;
    private readonly object _initLock = new();

    public KernelMemoryService(
        IOptions<OpenAIConfiguration> openAIConfig,
        IOptions<SemanticKernelConfiguration> skConfig,
        ILogger<KernelMemoryService> logger)
    {
        _openAIConfig = openAIConfig.Value;
        _skConfig = skConfig.Value;
        _logger = logger;

        // Initialize Kernel Memory with OpenAI and configurable vector database
        var memoryConfig = new Microsoft.KernelMemory.OpenAIConfig
        {
            APIKey = _openAIConfig.ApiKey,
            TextModel = _openAIConfig.Model,
            EmbeddingModel = "text-embedding-3-small"
        };

        _memory = BuildKernelMemory(memoryConfig);
    }

    private IKernelMemory BuildKernelMemory(Microsoft.KernelMemory.OpenAIConfig openAIConfig)
    {
        var builder = new KernelMemoryBuilder()
            .WithOpenAI(openAIConfig);

        // Configure vector database based on settings
        // Note: Currently only SimpleVectorDb is available with current packages
        // Additional vector databases require specific NuGet packages
        switch (_skConfig.VectorDb.Type)
        {
            case VectorDbType.SimpleVectorDb:
                _logger.LogInformation("Using SimpleVectorDb for vector storage (local, file-based)");
                builder.WithSimpleVectorDb();
                break;

            case VectorDbType.AzureCognitiveSearch:
                _logger.LogWarning("Azure Cognitive Search requires Microsoft.KernelMemory.AI.AzureCognitiveSearch package. Falling back to SimpleVectorDb");
                builder.WithSimpleVectorDb();
                break;

            case VectorDbType.Pinecone:
                _logger.LogWarning("Pinecone requires Microsoft.KernelMemory.AI.Pinecone package. Falling back to SimpleVectorDb");
                builder.WithSimpleVectorDb();
                break;

            case VectorDbType.Qdrant:
                _logger.LogWarning("Qdrant requires Microsoft.KernelMemory.AI.Qdrant package. Falling back to SimpleVectorDb");
                builder.WithSimpleVectorDb();
                break;

            case VectorDbType.Redis:
                _logger.LogWarning("Redis requires Microsoft.KernelMemory.AI.Redis package. Falling back to SimpleVectorDb");
                builder.WithSimpleVectorDb();
                break;

            default:
                _logger.LogWarning("Unknown vector database type: {VectorDbType}, using SimpleVectorDb", _skConfig.VectorDb.Type);
                builder.WithSimpleVectorDb();
                break;
        }

        return builder.Build<MemoryServerless>();
    }

    public async Task InitializeAsync()
    {
        if (_isInitialized) return;

        lock (_initLock)
        {
            if (_isInitialized) return;

            try
            {
                _logger.LogInformation("Initializing Kernel Memory service...");
                
                // Kernel Memory is ready to use after builder
                _isInitialized = true;
                _logger.LogInformation("Kernel Memory service initialized successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Kernel Memory service");
                throw;
            }
        }
    }

    public async Task LoadDocumentsAsync()
    {
        await InitializeAsync();

        try
        {
            _logger.LogInformation("Loading documents into Kernel Memory...");
            
            // Check if documents directory exists
            if (!Directory.Exists(_skConfig.DocumentsPath))
            {
                _logger.LogWarning("Documents directory does not exist: {DocumentsPath}", _skConfig.DocumentsPath);
                return;
            }

            // Supported file extensions for Kernel Memory
            var supportedExtensions = new[] { ".txt", ".docx", ".pdf", ".md", ".html", ".htm" };
            var loadedCount = 0;

            foreach (var extension in supportedExtensions)
            {
                var files = Directory.GetFiles(_skConfig.DocumentsPath, $"*{extension}", SearchOption.AllDirectories);
                
                foreach (var filePath in files)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var documentId = Path.GetFileNameWithoutExtension(fileName);
                        
                        _logger.LogDebug("Loading document: {FileName} (Type: {Extension})", fileName, extension);

                        // Use Kernel Memory's native document import
                        await _memory.ImportDocumentAsync(
                            filePath: filePath,
                            documentId: documentId,
                            tags: new TagCollection 
                            { 
                                { "filename", fileName },
                                { "extension", extension },
                                { "lastModified", File.GetLastWriteTime(filePath).ToString("O") },
                                { "fileSize", new System.IO.FileInfo(filePath).Length.ToString() }
                            });

                        loadedCount++;
                        _logger.LogDebug("Successfully loaded document: {DocumentId} ({FileName})", documentId, fileName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to load document: {FilePath}", filePath);
                    }
                }
            }

            _logger.LogInformation("Successfully loaded {LoadedCount} documents into Kernel Memory from {DocumentsPath}", 
                loadedCount, _skConfig.DocumentsPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load documents into Kernel Memory");
            throw;
        }
    }

    public async Task<string> SearchAsync(string query, int limit = 3)
    {
        await InitializeAsync();

        try
        {
            _logger.LogDebug("Searching Kernel Memory for: {Query}", query);

            var searchResult = await _memory.SearchAsync(
                query: query,
                limit: limit,
                minRelevance: 0.3);

            _logger.LogInformation("Search completed for query: {Query}. Found {ResultCount} results", query, searchResult.Results.Count());

            if (!searchResult.Results.Any())
            {
                _logger.LogWarning("No relevant documents found for query: {Query} with minRelevance: 0.3", query);
                return "No relevant information found in the knowledge base.";
            }

            // Log each result with relevance score
            foreach (var result in searchResult.Results)
            {
                var relevanceScore = result.Partitions.FirstOrDefault()?.Relevance ?? 0;
                _logger.LogInformation("Found result from source: {SourceName}, Relevance: {Relevance:F3}", 
                    result.SourceName ?? "Unknown", relevanceScore);
            }

            var context = string.Join("\n\n", searchResult.Results.Select(result => 
                $"[Source: {result.SourceName ?? "Unknown"}]\n{result.Partitions.FirstOrDefault()?.Text ?? "No content"}"));

            _logger.LogInformation("Successfully found {ResultCount} relevant documents for query: {Query}", searchResult.Results.Count(), query);
            
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Kernel Memory for query: {Query}", query);
            return "Error occurred while searching the knowledge base.";
        }
    }

    public Task<bool> IsInitializedAsync()
    {
        return Task.FromResult(_isInitialized);
    }

    public async Task<int> GetDocumentCountAsync()
    {
        await InitializeAsync();
        
        try
        {
            // Count files directly from Documents directory
            if (!Directory.Exists(_skConfig.DocumentsPath))
            {
                return 0;
            }

            var supportedExtensions = new[] { ".txt", ".docx", ".pdf", ".md", ".html", ".htm" };
            var totalCount = 0;

            foreach (var extension in supportedExtensions)
            {
                var files = Directory.GetFiles(_skConfig.DocumentsPath, $"*{extension}", SearchOption.AllDirectories);
                totalCount += files.Length;
            }

            return totalCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document count from Documents directory");
            return 0;
        }
    }

    public async Task<DocumentLoadResult> ReloadDocumentsAsync()
    {
        var result = new DocumentLoadResult();
        
        try
        {
            await InitializeAsync();

            _logger.LogInformation("Starting document reload...");
            
            // Check if documents directory exists
            if (!Directory.Exists(_skConfig.DocumentsPath))
            {
                result.Success = false;
                result.Message = $"Documents directory does not exist: {_skConfig.DocumentsPath}";
                result.Errors.Add(result.Message);
                return result;
            }

            // Supported file extensions for Kernel Memory
            var supportedExtensions = new[] { ".txt", ".docx", ".pdf", ".md", ".html", ".htm" };
            
            // Get statistics first
            result.Statistics = await GetDocumentStatisticsAsync();
            result.TotalDocuments = result.Statistics.TotalFiles;

            var loadedCount = 0;
            var failedCount = 0;

            foreach (var extension in supportedExtensions)
            {
                var files = Directory.GetFiles(_skConfig.DocumentsPath, $"*{extension}", SearchOption.AllDirectories);
                
                foreach (var filePath in files)
                {
                    try
                    {
                        var fileName = Path.GetFileName(filePath);
                        var documentId = Path.GetFileNameWithoutExtension(fileName);
                        
                        _logger.LogDebug("Reloading document: {FileName} (Type: {Extension})", fileName, extension);

                        // Use Kernel Memory's native document import
                        await _memory.ImportDocumentAsync(
                            filePath: filePath,
                            documentId: documentId,
                            tags: new TagCollection 
                            { 
                                { "filename", fileName },
                                { "extension", extension },
                                { "lastModified", File.GetLastWriteTime(filePath).ToString("O") },
                                { "fileSize", new System.IO.FileInfo(filePath).Length.ToString() },
                                { "reloadedAt", DateTime.UtcNow.ToString("O") }
                            });

                        loadedCount++;
                        _logger.LogDebug("Successfully reloaded document: {DocumentId} ({FileName})", documentId, fileName);
                    }
                    catch (Exception ex)
                    {
                        failedCount++;
                        var error = $"Failed to reload document: {filePath} - {ex.Message}";
                        result.Errors.Add(error);
                        _logger.LogError(ex, "Failed to reload document: {FilePath}", filePath);
                    }
                }
            }

            result.LoadedDocuments = loadedCount;
            result.FailedDocuments = failedCount;
            result.Success = failedCount == 0;
            result.Message = result.Success 
                ? $"Successfully reloaded all {loadedCount} documents" 
                : $"Reloaded {loadedCount} documents with {failedCount} failures";

            _logger.LogInformation("Document reload completed: {LoadedCount} loaded, {FailedCount} failed", 
                loadedCount, failedCount);
                
            return result;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Message = "Failed to reload documents";
            result.Errors.Add(ex.Message);
            _logger.LogError(ex, "Failed to reload documents");
            return result;
        }
    }

    public async Task<DocumentStatistics> GetDocumentStatisticsAsync()
    {
        await InitializeAsync();
        
        var statistics = new DocumentStatistics();

        try
        {
            if (!Directory.Exists(_skConfig.DocumentsPath))
            {
                return statistics;
            }

            var supportedExtensions = new[] { ".txt", ".docx", ".pdf", ".md", ".html", ".htm" };
            
            foreach (var extension in supportedExtensions)
            {
                var files = Directory.GetFiles(_skConfig.DocumentsPath, $"*{extension}", SearchOption.AllDirectories);
                
                if (files.Length > 0)
                {
                    statistics.FilesByExtension[extension] = files.Length;
                    statistics.TotalFiles += files.Length;

                    foreach (var filePath in files)
                    {
                        var fileInfo = new System.IO.FileInfo(filePath);
                        statistics.TotalSizeBytes += fileInfo.Length;
                        
                        if (fileInfo.LastWriteTime > statistics.LastModified)
                        {
                            statistics.LastModified = fileInfo.LastWriteTime;
                        }
                    }
                }
            }

            return statistics;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document statistics");
            return statistics;
        }
    }
}
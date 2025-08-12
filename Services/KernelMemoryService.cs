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
        switch (_skConfig.VectorDb.Type)
        {
            case VectorDbType.SimpleVectorDb:
                _logger.LogInformation("Using SimpleVectorDb for vector storage");
                builder.WithSimpleVectorDb();
                break;

            case VectorDbType.AzureCognitiveSearch:
                if (_skConfig.VectorDb.AzureCognitiveSearch != null)
                {
                    _logger.LogInformation("Using Azure Cognitive Search for vector storage");
                    builder.WithAzureCognitiveSearch(
                        _skConfig.VectorDb.AzureCognitiveSearch.Endpoint ?? throw new InvalidOperationException("Azure Cognitive Search endpoint is required"),
                        _skConfig.VectorDb.AzureCognitiveSearch.ApiKey ?? throw new InvalidOperationException("Azure Cognitive Search API key is required"));
                }
                else
                {
                    _logger.LogWarning("Azure Cognitive Search configuration is missing, falling back to SimpleVectorDb");
                    builder.WithSimpleVectorDb();
                }
                break;

            case VectorDbType.Pinecone:
                if (_skConfig.VectorDb.Pinecone != null)
                {
                    _logger.LogInformation("Using Pinecone for vector storage");
                    builder.WithPinecone(
                        _skConfig.VectorDb.Pinecone.ApiKey ?? throw new InvalidOperationException("Pinecone API key is required"),
                        _skConfig.VectorDb.Pinecone.Environment ?? throw new InvalidOperationException("Pinecone environment is required"));
                }
                else
                {
                    _logger.LogWarning("Pinecone configuration is missing, falling back to SimpleVectorDb");
                    builder.WithSimpleVectorDb();
                }
                break;

            case VectorDbType.Qdrant:
                if (!string.IsNullOrEmpty(_skConfig.VectorDb.ConnectionString))
                {
                    _logger.LogInformation("Using Qdrant for vector storage");
                    builder.WithQdrant(_skConfig.VectorDb.ConnectionString);
                }
                else
                {
                    _logger.LogWarning("Qdrant connection string is missing, falling back to SimpleVectorDb");
                    builder.WithSimpleVectorDb();
                }
                break;

            case VectorDbType.Redis:
                if (!string.IsNullOrEmpty(_skConfig.VectorDb.ConnectionString))
                {
                    _logger.LogInformation("Using Redis for vector storage");
                    builder.WithRedis(_skConfig.VectorDb.ConnectionString);
                }
                else
                {
                    _logger.LogWarning("Redis connection string is missing, falling back to SimpleVectorDb");
                    builder.WithSimpleVectorDb();
                }
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
                                { "fileSize", new FileInfo(filePath).Length.ToString() }
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
                minRelevance: 0.7);

            if (!searchResult.Results.Any())
            {
                _logger.LogDebug("No relevant documents found for query: {Query}", query);
                return "No relevant information found in the knowledge base.";
            }

            var context = string.Join("\n\n", searchResult.Results.Select(result => 
                $"[Source: {result.SourceName ?? "Unknown"}]\n{result.Partitions.FirstOrDefault()?.Text ?? "No content"}"));

            _logger.LogDebug("Found {ResultCount} relevant documents for query: {Query}", searchResult.Results.Count(), query);
            
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
}
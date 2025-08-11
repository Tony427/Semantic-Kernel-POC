using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI.OpenAI;
using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Services;

public class KernelMemoryService : IKernelMemoryService
{
    private readonly IKernelMemory _memory;
    private readonly IFileReaderService _fileReaderService;
    private readonly OpenAIConfiguration _openAIConfig;
    private readonly SemanticKernelConfiguration _skConfig;
    private readonly ILogger<KernelMemoryService> _logger;
    private bool _isInitialized = false;
    private readonly object _initLock = new();

    public KernelMemoryService(
        IFileReaderService fileReaderService,
        IOptions<OpenAIConfiguration> openAIConfig,
        IOptions<SemanticKernelConfiguration> skConfig,
        ILogger<KernelMemoryService> logger)
    {
        _fileReaderService = fileReaderService;
        _openAIConfig = openAIConfig.Value;
        _skConfig = skConfig.Value;
        _logger = logger;

        // Initialize Kernel Memory with OpenAI
        var memoryConfig = new Microsoft.KernelMemory.OpenAIConfig
        {
            APIKey = _openAIConfig.ApiKey,
            TextModel = _openAIConfig.Model,
            EmbeddingModel = "text-embedding-3-small"
        };

        _memory = new KernelMemoryBuilder()
            .WithOpenAI(memoryConfig)
            .WithSimpleVectorDb()
            .Build<MemoryServerless>();
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
            
            var documents = await _fileReaderService.GetAllDocumentsAsync();
            var loadedCount = 0;

            foreach (var document in documents)
            {
                try
                {
                    // Use filename without extension as document ID
                    var documentId = Path.GetFileNameWithoutExtension(document.FileName);
                    
                    // Import document into memory
                    await _memory.ImportTextAsync(
                        text: document.Content,
                        documentId: documentId,
                        tags: new TagCollection 
                        { 
                            { "filename", document.FileName },
                            { "lastModified", document.LastModified.ToString("O") },
                            { "fileSize", document.FileSizeBytes.ToString() }
                        });

                    loadedCount++;
                    _logger.LogDebug("Loaded document: {DocumentId} ({FileName})", documentId, document.FileName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load document: {FileName}", document.FileName);
                }
            }

            _logger.LogInformation("Successfully loaded {LoadedCount} documents into Kernel Memory", loadedCount);
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
            // Since we're using SimpleVectorDb, we can't easily get document count
            // Return the count from file reader service as approximation
            return await _fileReaderService.GetDocumentCountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get document count from Kernel Memory");
            return 0;
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using SemanticKernel.ChatBot.Api.Services;

namespace SemanticKernel.ChatBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MemoryController : ControllerBase
{
    private readonly IKernelMemoryService _memoryService;
    private readonly ILogger<MemoryController> _logger;

    public MemoryController(
        IKernelMemoryService memoryService,
        ILogger<MemoryController> logger)
    {
        _memoryService = memoryService;
        _logger = logger;
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> Initialize()
    {
        try
        {
            await _memoryService.InitializeAsync();
            return Ok(new { message = "Kernel Memory initialized successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize Kernel Memory");
            return StatusCode(500, new { error = "Failed to initialize Kernel Memory" });
        }
    }

    [HttpPost("load-documents")]
    public async Task<IActionResult> LoadDocuments()
    {
        try
        {
            await _memoryService.LoadDocumentsAsync();
            var count = await _memoryService.GetDocumentCountAsync();
            
            return Ok(new { 
                message = "Documents loaded successfully into Kernel Memory",
                documentCount = count
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load documents into Kernel Memory");
            return StatusCode(500, new { error = "Failed to load documents into Kernel Memory" });
        }
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int limit = 3)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return BadRequest(new { error = "Query parameter is required" });
        }

        try
        {
            var context = await _memoryService.SearchAsync(query, limit);
            
            return Ok(new { 
                query,
                limit,
                context,
                message = "Search completed successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to search Kernel Memory for query: {Query}", query);
            return StatusCode(500, new { error = "Failed to search Kernel Memory" });
        }
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        try
        {
            var isInitialized = await _memoryService.IsInitializedAsync();
            var documentCount = await _memoryService.GetDocumentCountAsync();

            return Ok(new {
                isInitialized,
                documentCount,
                message = isInitialized ? "Kernel Memory is ready" : "Kernel Memory not initialized"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Kernel Memory status");
            return StatusCode(500, new { error = "Failed to get Kernel Memory status" });
        }
    }
}
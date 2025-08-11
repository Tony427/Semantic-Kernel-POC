using Microsoft.AspNetCore.Mvc;
using SemanticKernel.ChatBot.Api.Services;

namespace SemanticKernel.ChatBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TestController : ControllerBase
{
    private readonly IChatHistoryService _chatHistoryService;
    private readonly IKernelMemoryService _kernelMemoryService;
    private readonly IFileReaderService _fileReaderService;
    private readonly ILogger<TestController> _logger;

    public TestController(
        IChatHistoryService chatHistoryService,
        IKernelMemoryService kernelMemoryService,
        IFileReaderService fileReaderService,
        ILogger<TestController> logger)
    {
        _chatHistoryService = chatHistoryService;
        _kernelMemoryService = kernelMemoryService;
        _fileReaderService = fileReaderService;
        _logger = logger;
    }

    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new 
        { 
            status = "healthy",
            timestamp = DateTime.UtcNow,
            service = "Semantic Kernel ChatBot API"
        });
    }

    [HttpGet("services")]
    public async Task<IActionResult> ServiceStatus()
    {
        try
        {
            var documentCount = await _fileReaderService.GetDocumentCountAsync();
            var memoryInitialized = await _kernelMemoryService.IsInitializedAsync();
            
            return Ok(new
            {
                fileReaderService = new
                {
                    status = "ready",
                    documentCount = documentCount
                },
                kernelMemoryService = new
                {
                    status = memoryInitialized ? "initialized" : "not_initialized"
                },
                chatHistoryService = new
                {
                    status = "ready"
                },
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking service status");
            return StatusCode(500, new { error = "Failed to check service status" });
        }
    }

    [HttpPost("initialize")]
    public async Task<IActionResult> InitializeServices()
    {
        try
        {
            await _kernelMemoryService.InitializeAsync();
            await _kernelMemoryService.LoadDocumentsAsync();

            var status = await _kernelMemoryService.GetMemoryStatusAsync();
            
            return Ok(new
            {
                message = "Services initialized successfully",
                memoryStatus = status,
                timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing services");
            return StatusCode(500, new { error = "Failed to initialize services" });
        }
    }
}
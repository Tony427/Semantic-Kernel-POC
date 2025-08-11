using Microsoft.AspNetCore.Mvc;
using SemanticKernel.ChatBot.Api.Services;

namespace SemanticKernel.ChatBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IFileReaderService _fileReaderService;
    private readonly ILogger<DocumentsController> _logger;

    public DocumentsController(
        IFileReaderService fileReaderService,
        ILogger<DocumentsController> logger)
    {
        _fileReaderService = fileReaderService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllDocuments()
    {
        try
        {
            var documents = await _fileReaderService.GetAllDocumentsAsync();
            return Ok(documents.Select(d => new
            {
                d.FileName,
                d.LastModified,
                d.FileSizeBytes,
                ContentPreview = d.Content.Length > 200 ? d.Content[..200] + "..." : d.Content
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all documents");
            return StatusCode(500, new { error = "Failed to retrieve documents" });
        }
    }

    [HttpGet("{fileName}")]
    public async Task<IActionResult> GetDocumentByName(string fileName)
    {
        try
        {
            var document = await _fileReaderService.GetDocumentByFileNameAsync(fileName);
            if (document == null)
            {
                return NotFound(new { error = $"Document '{fileName}' not found" });
            }

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document: {FileName}", fileName);
            return StatusCode(500, new { error = "Failed to retrieve document" });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshDocuments()
    {
        try
        {
            var success = await _fileReaderService.RefreshDocumentsAsync();
            var count = await _fileReaderService.GetDocumentCountAsync();
            
            return Ok(new { 
                success, 
                documentCount = count,
                message = success ? "Documents refreshed successfully" : "Failed to refresh documents"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing documents");
            return StatusCode(500, new { error = "Failed to refresh documents" });
        }
    }

    [HttpGet("count")]
    public async Task<IActionResult> GetDocumentCount()
    {
        try
        {
            var count = await _fileReaderService.GetDocumentCountAsync();
            return Ok(new { count });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document count");
            return StatusCode(500, new { error = "Failed to get document count" });
        }
    }
}
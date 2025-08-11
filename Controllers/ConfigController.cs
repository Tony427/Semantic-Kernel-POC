using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SemanticKernel.ChatBot.Api.Models;

namespace SemanticKernel.ChatBot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConfigController : ControllerBase
{
    private readonly OpenAIConfiguration _openAIConfig;
    private readonly SemanticKernelConfiguration _skConfig;
    private readonly DatabaseConfiguration _dbConfig;

    public ConfigController(
        IOptions<OpenAIConfiguration> openAIConfig,
        IOptions<SemanticKernelConfiguration> skConfig,
        IOptions<DatabaseConfiguration> dbConfig)
    {
        _openAIConfig = openAIConfig.Value;
        _skConfig = skConfig.Value;
        _dbConfig = dbConfig.Value;
    }

    [HttpGet("status")]
    public IActionResult GetConfigurationStatus()
    {
        return Ok(new
        {
            OpenAI = new
            {
                HasApiKey = !string.IsNullOrEmpty(_openAIConfig.ApiKey) && _openAIConfig.ApiKey != "YOUR_OPENAI_API_KEY_HERE",
                Model = _openAIConfig.Model,
                MaxTokens = _openAIConfig.MaxTokens
            },
            SemanticKernel = new
            {
                DocumentsPath = _skConfig.DocumentsPath,
                MaxMemoryTokens = _skConfig.MaxMemoryTokens,
                DocumentsPathExists = Directory.Exists(_skConfig.DocumentsPath)
            },
            Database = new
            {
                ConnectionString = _dbConfig.ConnectionString
            }
        });
    }
}
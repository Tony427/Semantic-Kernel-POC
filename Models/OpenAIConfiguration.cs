namespace SemanticKernel.ChatBot.Api.Models;

public class OpenAIConfiguration
{
    public const string SectionName = "OpenAI";
    
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxTokens { get; set; } = 2000;
}
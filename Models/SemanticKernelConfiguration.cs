namespace SemanticKernel.ChatBot.Api.Models;

public class SemanticKernelConfiguration
{
    public const string SectionName = "SemanticKernel";
    
    public string DocumentsPath { get; set; } = "./Documents";
    public int MaxMemoryTokens { get; set; } = 4000;
}
namespace SemanticKernel.ChatBot.Api.Models;

public class DocumentModel
{
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public long FileSizeBytes { get; set; }
}
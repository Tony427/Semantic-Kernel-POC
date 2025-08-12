namespace SemanticKernel.ChatBot.Api.Models;

public class DocumentLoadResult
{
    public bool Success { get; set; }
    public int TotalDocuments { get; set; }
    public int LoadedDocuments { get; set; }
    public int FailedDocuments { get; set; }
    public DocumentStatistics Statistics { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public string Message { get; set; } = string.Empty;
    public DateTime LoadedAt { get; set; } = DateTime.UtcNow;
}

public class DocumentStatistics
{
    public int TotalFiles { get; set; }
    public Dictionary<string, int> FilesByExtension { get; set; } = new();
    public long TotalSizeBytes { get; set; }
    public DateTime LastModified { get; set; }
}

public class DocumentFileInfo
{
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime LastModified { get; set; }
    public bool LoadedSuccessfully { get; set; }
    public string? ErrorMessage { get; set; }
}
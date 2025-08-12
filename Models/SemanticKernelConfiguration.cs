namespace SemanticKernel.ChatBot.Api.Models;

public class SemanticKernelConfiguration
{
    public const string SectionName = "SemanticKernel";
    
    public string DocumentsPath { get; set; } = "./Documents";
    public int MaxMemoryTokens { get; set; } = 4000;
    
    // Vector Database Configuration
    public VectorDbConfiguration VectorDb { get; set; } = new();
}

public class VectorDbConfiguration
{
    public VectorDbType Type { get; set; } = VectorDbType.SimpleVectorDb;
    public string? ConnectionString { get; set; }
    public AzureCognitiveSearchConfig? AzureCognitiveSearch { get; set; }
    public PineconeConfig? Pinecone { get; set; }
}

public class AzureCognitiveSearchConfig
{
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}

public class PineconeConfig
{
    public string? ApiKey { get; set; }
    public string? Environment { get; set; }
}

public enum VectorDbType
{
    SimpleVectorDb,
    AzureCognitiveSearch,
    Pinecone,
    Qdrant,
    Redis
}
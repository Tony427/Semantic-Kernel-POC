namespace SemanticKernel.ChatBot.Api.Models;

public class DatabaseConfiguration
{
    public const string SectionName = "Database";

    public string ConnectionString { get; set; } = "Data Source=chatbot.db";
}
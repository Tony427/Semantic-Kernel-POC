using Microsoft.EntityFrameworkCore;
using SemanticKernel.ChatBot.Api.Data;
using SemanticKernel.ChatBot.Api.Models;
using SemanticKernel.ChatBot.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Services.Configure<OpenAIConfiguration>(
    builder.Configuration.GetSection(OpenAIConfiguration.SectionName));
builder.Services.Configure<SemanticKernelConfiguration>(
    builder.Configuration.GetSection(SemanticKernelConfiguration.SectionName));
builder.Services.Configure<DatabaseConfiguration>(
    builder.Configuration.GetSection(DatabaseConfiguration.SectionName));

// Add Entity Framework
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? builder.Configuration.GetSection("Database:ConnectionString").Value
    ?? "Data Source=chatbot.db";

builder.Services.AddDbContext<ChatBotDbContext>(options =>
    options.UseSqlite(connectionString));

// Add application services
builder.Services.AddScoped<IFileReaderService, FileReaderService>();
builder.Services.AddScoped<IChatHistoryService, ChatHistoryService>();
builder.Services.AddSingleton<IKernelMemoryService, KernelMemoryService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ChatBotDbContext>();
    context.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

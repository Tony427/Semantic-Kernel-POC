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

// Add application services
builder.Services.AddScoped<IFileReaderService, FileReaderService>();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

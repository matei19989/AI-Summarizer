// AISummarizerAPI/Program.cs - Clean, focused bootstrap
using AISummarizerAPI.Extensions.ServiceCollection;
using AISummarizerAPI.Extensions.WebApp;
using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// Service Registration - Clean and Organized
// ===================================================================
builder.Services.AddConfiguration(builder.Configuration);
builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices();
builder.Services.AddHttpClientServices(builder.Configuration);
builder.Services.AddSecurityServices(builder.Configuration, builder.Environment);
builder.Services.AddFrameworkServices();

var app = builder.Build();

// ===================================================================
// Pipeline Configuration - Environment Aware
// ===================================================================
app.ConfigurePipeline(builder.Environment);
app.ConfigureEndpoints();
app.ConfigureHealthChecks();

// ===================================================================
// Application Startup
// ===================================================================
await app.ValidateStartupAsync();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("🚀 AI Content Summarizer v2.1.0 - Clean Architecture Edition");

app.Run();
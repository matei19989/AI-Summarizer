// AISummarizerAPI/Program.cs
using AISummarizerAPI.Configuration;

// Core interfaces - these define our business capabilities
using AISummarizerAPI.Core.Interfaces;

// Application layer - orchestration and use cases
using AISummarizerAPI.Application.Interfaces;
using AISummarizerAPI.Application.Services;

// Infrastructure layer - concrete implementations
using AISummarizerAPI.Infrastructure.Services;

// Legacy interfaces that we're keeping for the HuggingFace integration
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// Configuration Setup (unchanged - this part was already good)
// ===================================================================

builder.Services.Configure<HuggingFaceOptions>(
    builder.Configuration.GetSection(HuggingFaceOptions.SectionName));

builder.Services.AddOptions<HuggingFaceOptions>()
    .Bind(builder.Configuration.GetSection(HuggingFaceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ===================================================================
// Core Services Registration - The New Architecture
// ===================================================================

// Register our domain services following the new architecture
// Notice how we're programming to interfaces, not implementations
// This makes our system incredibly flexible and testable

// Core business services - each with a single, focused responsibility
builder.Services.AddScoped<IContentValidator, ContentValidationService>();
builder.Services.AddScoped<IResponseFormatter, ResponseFormatterService>();

// Infrastructure services - these handle external dependencies
builder.Services.AddScoped<IContentSummarizer, HuggingFaceContentSummarizer>();
builder.Services.AddScoped<IContentExtractor, SmartReaderContentExtractor>();

// Application layer - this is our use case orchestrator
// This is the heart of our new architecture - it coordinates everything
builder.Services.AddScoped<ISummarizationOrchestrator, SummarizationOrchestrator>();

// Legacy services that our new infrastructure services depend on
// We keep these because they contain well-tested, working logic
// Eventually, we might refactor these further, but for now they serve us well
builder.Services.AddScoped<IHuggingFaceApiClient, HuggingFaceApiClient>();

// ===================================================================
// HTTP Client Configuration (unchanged - this part was already excellent)
// ===================================================================

// HTTP client for general HTTP operations (used by content validator and extractor)
builder.Services.AddHttpClient<IContentValidator, ContentValidationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.0 (Content Validator)");
});

// HTTP client for URL content extraction
builder.Services.AddHttpClient<IContentExtractor, SmartReaderContentExtractor>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.0 (Content Extractor)");
});

// HTTP client for Hugging Face API communication
builder.Services.AddHttpClient<IHuggingFaceApiClient, HuggingFaceApiClient>((serviceProvider, client) =>
{
    var huggingFaceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    client.Timeout = TimeSpan.FromSeconds(huggingFaceOptions.RateLimit.TimeoutSeconds);
    client.BaseAddress = new Uri(huggingFaceOptions.BaseUrl);
    
    if (!string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", huggingFaceOptions.ApiToken);
    }
    
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AISummarizer/2.0 (Hugging Face Integration)");
});

// ===================================================================
// Framework Services (unchanged)
// ===================================================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ===================================================================
// CORS Configuration (unchanged)
// ===================================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", corsBuilder =>
    {
        if (builder.Environment.IsDevelopment())
        {
            corsBuilder
                .WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "http://localhost:4173",
                    "http://frontend:80",
                    "http://localhost:80"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            corsBuilder
                .WithOrigins(
                    "http://frontend:80",
                    "http://localhost:80",
                    "https://localhost:443",
                    "https://yourdomain.com"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Length", "Content-Type");
        }
    });
});

// ===================================================================
// Application Pipeline Configuration
// ===================================================================

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application starting in Development mode with new Clean Architecture");
    
    var huggingFaceOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    logger.LogInformation("Hugging Face API configured: BaseUrl={BaseUrl}, HasToken={HasToken}, Model={Model}", 
        huggingFaceOptions.BaseUrl,
        !string.IsNullOrEmpty(huggingFaceOptions.ApiToken),
        huggingFaceOptions.Models.SummarizationModel);
}

app.UseCors("ReactPolicy");
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ===================================================================
// Enhanced Root Endpoint - Shows Our New Architecture
// ===================================================================

app.MapGet("/", (IServiceProvider serviceProvider) =>
{
    var huggingFaceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    return new
    {
        Application = "AI Content Summarizer API",
        Version = "2.0.0", // Updated to reflect our architectural improvements
        Architecture = "Clean Architecture with Domain-Driven Design",
        Status = "Running",
        Timestamp = DateTime.UtcNow,
        
        Features = new[] 
        { 
            "AI-Powered Text Summarization", 
            "Intelligent URL Content Extraction", 
            "Comprehensive Input Validation",
            "Extensible Service Architecture",
            "Real-time Processing with Cancellation Support",
            "Robust Error Handling and User Feedback"
        },
        
        Architecture_Benefits = new[]
        {
            "Single Responsibility Principle - Each service has one clear job",
            "Interface Segregation - Clean, focused interfaces", 
            "Dependency Inversion - Program to abstractions, not concretions",
            "Separation of Concerns - Business logic isolated from infrastructure",
            "Testability - Each component can be easily unit tested",
            "Extensibility - Easy to add new AI providers or content sources"
        },
        
        AI = new
        {
            Provider = "Hugging Face",
            SummarizationModel = huggingFaceOptions.Models.SummarizationModel,
            IsConfigured = !string.IsNullOrEmpty(huggingFaceOptions.ApiToken),
            RateLimit = $"{huggingFaceOptions.RateLimit.RequestsPerMinute} requests/minute"
        },
        
        Endpoints = new
        {
            Summarize = "/api/summarization/summarize",
            Health = "/api/summarization/health",
            Info = "/api/summarization/info"
        }
    };
});

// ===================================================================
// Startup Validation with Enhanced Architecture Awareness
// ===================================================================

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Test our new orchestrator to make sure everything is wired up correctly
    try
    {
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISummarizationOrchestrator>();
        var isHealthy = await orchestrator.IsHealthyAsync();
        
        if (isHealthy)
        {
            logger.LogInformation("✅ New Clean Architecture successfully initialized - all services are healthy");
        }
        else
        {
            logger.LogWarning("⚠️ Some services are not available - check AI provider configuration");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error testing new architecture initialization");
    }
    
    // Validate configuration as before
    var huggingFaceOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    if (string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        logger.LogWarning("Hugging Face API token is not configured. Set 'HuggingFace:ApiToken' in user secrets or environment variables.");
    }
    else
    {
        logger.LogInformation("Hugging Face API token is configured. Real AI summarization is enabled.");
    }
}

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 AI Content Summarizer API v2.0 is starting with Clean Architecture and enhanced capabilities...");

app.Run();
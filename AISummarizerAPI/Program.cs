// AISummarizerAPI/Program.cs
using AISummarizerAPI.Configuration;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// Configuration Setup
// ===================================================================

// Configure Hugging Face API options from appsettings.json and user secrets
builder.Services.Configure<HuggingFaceOptions>(
    builder.Configuration.GetSection(HuggingFaceOptions.SectionName));

// Validate configuration at startup to catch issues early
builder.Services.AddOptions<HuggingFaceOptions>()
    .Bind(builder.Configuration.GetSection(HuggingFaceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ===================================================================
// Core Services
// ===================================================================

// Add controllers and OpenAPI support
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ===================================================================
// HTTP Client Configuration
// ===================================================================

// HTTP client for SummarizationService (existing functionality)
// Used for URL content extraction and general HTTP operations
builder.Services.AddHttpClient<ISummarizationService, SummarizationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/1.0");
});

// HTTP client for URL content extraction (existing functionality)
// Separate client with different timeout settings for URL validation
builder.Services.AddHttpClient<IUrlContentExtractor, UrlContentExtractor>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/1.0 (Content Extractor)");
});

// NEW: HTTP client for Hugging Face API communication
// Configured specifically for AI API calls with longer timeouts
builder.Services.AddHttpClient<IHuggingFaceApiClient, HuggingFaceApiClient>((serviceProvider, client) =>
{
    var huggingFaceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    // Configure timeout from options
    client.Timeout = TimeSpan.FromSeconds(huggingFaceOptions.RateLimit.TimeoutSeconds);
    
    // Set base address for Hugging Face API
    client.BaseAddress = new Uri(huggingFaceOptions.BaseUrl);
    
    // Add authentication header if token is configured
    if (!string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", huggingFaceOptions.ApiToken);
    }
    
    // Set user agent for API identification
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AISummarizer/1.0 (Hugging Face Integration)");
});

// ===================================================================
// Service Registration
// ===================================================================

// Register existing services (URL content extraction)
builder.Services.AddScoped<IUrlContentExtractor, UrlContentExtractor>();

// NEW: Register Hugging Face API client
// This service handles all communication with Hugging Face APIs
builder.Services.AddScoped<IHuggingFaceApiClient, HuggingFaceApiClient>();

// Register main summarization service (now enhanced with AI)
// This orchestrates between URL extraction and AI summarization
builder.Services.AddScoped<ISummarizationService, SummarizationService>();

// ===================================================================
// CORS Configuration
// ===================================================================

// Enhanced CORS configuration for both development and production

// Updated Program.cs CORS configuration for Docker environment
main
builder.Services.AddCors(options =>
{
    options.AddPolicy("ReactPolicy", corsBuilder =>
    {
        if (builder.Environment.IsDevelopment())
        {
            // Development: Allow common dev server origins
            corsBuilder
                .WithOrigins(
                    "http://localhost:3000",    // React dev server
                    "http://localhost:5173",    // Vite dev server
                    "http://localhost:4173",    // Vite preview
                    "http://frontend:80",       // Docker development
                    "http://localhost:80"       // Docker host access
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            // Production: More restrictive
            corsBuilder
                .WithOrigins(
                    "http://frontend:80",       // Docker internal communication
                    "http://localhost:80",      // Docker host access
                    "https://localhost:443",    // If SSL enabled
                    "https://yourdomain.com"    // Replace with your actual domain
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

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    
    // In development, log the configuration for debugging
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application starting in Development mode");
    
    // Log Hugging Face configuration status (without exposing sensitive data)
    var huggingFaceOptions = app.Services.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    logger.LogInformation("Hugging Face API configured: BaseUrl={BaseUrl}, HasToken={HasToken}, Model={Model}", 
        huggingFaceOptions.BaseUrl,
        !string.IsNullOrEmpty(huggingFaceOptions.ApiToken),
        huggingFaceOptions.Models.SummarizationModel);
}

// Apply CORS policy before other middleware
app.UseCors("ReactPolicy");

// Security and routing
app.UseHttpsRedirection();
app.UseAuthorization();

// Map controller routes
app.MapControllers();

// ===================================================================
// Root Endpoint with Enhanced Information
// ===================================================================

// Enhanced root endpoint showing new AI capabilities
app.MapGet("/", (IServiceProvider serviceProvider) =>
{
    var huggingFaceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    return new
    {
        Application = "AI Content Summarizer API",
        Version = "2.0.0", // Incremented version to reflect AI integration
        Status = "Running",
        Timestamp = DateTime.UtcNow,
        Features = new[] 
        { 
            "Text Summarization with AI", 
            "URL Content Extraction", 
            "Hugging Face Integration",
            "Rate Limited API Calls",
            "Real-time AI Processing"
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
// Startup Validation and Health Checks
// ===================================================================

// Validate critical configuration at startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    var huggingFaceOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    // Warn if Hugging Face token is not configured
    if (string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        logger.LogWarning("Hugging Face API token is not configured. Set 'HuggingFace:ApiToken' in user secrets or environment variables.");
        logger.LogWarning("Without an API token, the application will use mock responses for development.");
    }
    else
    {
        logger.LogInformation("Hugging Face API token is configured. Real AI summarization is enabled.");
    }
    
    // Test Hugging Face API connection if token is available
    if (!string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        try
        {
            var huggingFaceClient = scope.ServiceProvider.GetRequiredService<IHuggingFaceApiClient>();
            var connectionTest = await huggingFaceClient.TestConnectionAsync();
            
            if (connectionTest)
            {
                logger.LogInformation("✅ Hugging Face API connection test successful");
            }
            else
            {
                logger.LogWarning("⚠️ Hugging Face API connection test failed - check your token and network connectivity");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Error testing Hugging Face API connection");
        }
    }
}

// Get logger after app is built and log startup message
var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 AI Content Summarizer API is starting with enhanced AI capabilities...");

app.Run();
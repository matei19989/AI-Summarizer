// AISummarizerAPI/Program.cs - Complete Enhanced Version for Azure Deployment
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

// System and networking
using System.Net;

// Polly resilience libraries
using Polly;
using Polly.Extensions.Http;
using Microsoft.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// ===================================================================
// Configuration Setup
// ===================================================================

builder.Services.Configure<HuggingFaceOptions>(
    builder.Configuration.GetSection(HuggingFaceOptions.SectionName));

builder.Services.AddOptions<HuggingFaceOptions>()
    .Bind(builder.Configuration.GetSection(HuggingFaceOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ===================================================================
// Core Services Registration
// ===================================================================

builder.Services.AddScoped<IContentValidator, ContentValidationService>();
builder.Services.AddScoped<IResponseFormatter, ResponseFormatterService>();
builder.Services.AddScoped<IContentSummarizer, HuggingFaceContentSummarizer>();
builder.Services.AddScoped<IContentExtractor, SmartReaderContentExtractor>();
builder.Services.AddScoped<ISummarizationOrchestrator, SummarizationOrchestrator>();
builder.Services.AddScoped<IHuggingFaceApiClient, HuggingFaceApiClient>();

// ===================================================================
// Enhanced HTTP Client Configuration for Azure Cloud Environment
// ===================================================================

/*
 * Why we need special HTTP client configuration for Azure:
 * 
 * 1. Connection Pooling: Azure containers benefit from persistent connections
 * 2. Timeout Management: Cloud-to-cloud calls need longer, more strategic timeouts
 * 3. Retry Policies: Network hiccups are more common in cloud environments
 * 4. Connection Limits: We need to manage how many concurrent connections we use
 */

// Configure HTTP client for general operations (content validation and extraction)
builder.Services.AddHttpClient<IContentValidator, ContentValidationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30); // Reasonable timeout for validation
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.0 (Content Validator)");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    // Allow more connections per endpoint for better performance
    MaxConnectionsPerServer = 10,
    // Use connection pooling efficiently
    PooledConnectionLifetime = TimeSpan.FromMinutes(15)
});

builder.Services.AddHttpClient<IContentExtractor, SmartReaderContentExtractor>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45); // Content extraction can take longer
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.0 (Content Extractor)");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 5
});

// Special configuration for Hugging Face API client - this is the critical one
builder.Services.AddHttpClient<IHuggingFaceApiClient, HuggingFaceApiClient>((serviceProvider, client) =>
{
    var huggingFaceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    /*
     * Timeout Strategy for AI APIs:
     * - We use a longer timeout because AI processing inherently takes time
     * - We set this at the HTTP client level, and implement additional retry logic at the service level
     * - This gives us maximum flexibility to handle different types of delays
     */
    client.Timeout = TimeSpan.FromSeconds(120); // Increased from 45 to 120 seconds for AI processing
    client.BaseAddress = new Uri(huggingFaceOptions.BaseUrl);
    
    if (!string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        client.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", huggingFaceOptions.ApiToken);
    }
    
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AISummarizer/2.0 (Hugging Face Integration)");
    
    /*
     * Connection optimization headers:
     * - Keep-Alive helps maintain persistent connections
     * - Connection pooling reduces overhead for subsequent requests
     */
    client.DefaultRequestHeaders.ConnectionClose = false;
    client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=60");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    /*
     * Advanced connection management for external API calls:
     * 
     * MaxConnectionsPerServer: Limits concurrent connections to Hugging Face
     * This prevents overwhelming their servers and reduces connection competition
     */
    MaxConnectionsPerServer = 3,

    /*
     * PooledConnectionLifetime: How long to keep connections alive
     * Shorter lifetime ensures we don't hit connection limits, but too short hurts performance
     * 20 minutes is a good balance for API calls
     */
    PooledConnectionLifetime = TimeSpan.FromMinutes(20),

    /*
     * UseCookies: Disable cookie container for API calls (not needed, saves memory)
     */
    UseCookies = false,

    /*
     * AutomaticDecompression: Enable compression to reduce bandwidth
     * This can significantly speed up large responses from AI APIs
     */
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
})
/*
 * Polly integration for resilience:
 * This adds automatic retry and circuit breaker patterns
 * We'll configure this to handle transient network errors gracefully
 */
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// ===================================================================
// Framework Services
// ===================================================================

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ===================================================================
// CORS Configuration - Azure Production Ready
// ===================================================================

builder.Services.AddCors(options =>
{
    options.AddPolicy("ProductionPolicy", corsBuilder =>
    {
        var allowedOrigins = builder.Configuration.GetSection("ASPNETCORE_ALLOWEDORIGINS").Get<string>();
        
        if (!string.IsNullOrEmpty(allowedOrigins))
        {
            var origins = allowedOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                     .Select(o => o.Trim())
                                     .ToArray();
            
            corsBuilder
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            corsBuilder
                .WithOrigins(
                    "http://localhost:3000",
                    "http://localhost:5173",
                    "http://localhost:4173",
                    "https://ai-summarizer-au3d83i5e-matei19989s-projects.vercel.app"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            corsBuilder
                .WithOrigins(
                    "https://ai-summarizer-au3d83i5e-matei19989s-projects.vercel.app",
                    "https://aisummarizer2026-bsech4f0cyh3akdw.northeurope-01.azurewebsites.net"
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
}

// IMPORTANT: CORS must come before other middleware
app.UseCors("ProductionPolicy");

// Security headers for production
if (app.Environment.IsProduction())
{
    app.Use(async (context, next) =>
    {
        context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
        context.Response.Headers.Append("X-Frame-Options", "DENY");
        context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
        context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
        await next();
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ===================================================================
// Enhanced Root Endpoint for Azure Monitoring
// ===================================================================

app.MapGet("/", (IServiceProvider serviceProvider) =>
{
    var huggingFaceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    return new
    {
        Application = "AI Content Summarizer API",
        Version = "2.0.1-Azure-Enhanced",
        Environment = app.Environment.EnvironmentName,
        Status = "Running with Enhanced Network Resilience",
        Timestamp = DateTime.UtcNow,
        
        NetworkOptimizations = new
        {
            HttpClientTimeout = "120 seconds",
            ConnectionPooling = "Enabled",
            RetryPolicy = "Enabled with exponential backoff",
            CircuitBreaker = "Enabled (5 failures trigger 30s break)",
            ConnectionManagement = "Optimized for cloud-to-cloud communication"
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
// Startup Validation for Azure Deployment
// ===================================================================

using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var orchestrator = scope.ServiceProvider.GetRequiredService<ISummarizationOrchestrator>();
        var isHealthy = await orchestrator.IsHealthyAsync();
        
        if (isHealthy)
        {
            logger.LogInformation("✅ Azure deployment with enhanced networking is healthy");
        }
        else
        {
            logger.LogWarning("⚠️ Some services are not available - will retry with resilience policies");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error during startup - resilience policies will handle runtime issues");
    }
    
    var huggingFaceOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    
    if (string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        logger.LogWarning("⚠️ Hugging Face API token is not configured in Azure App Settings");
    }
    else
    {
        logger.LogInformation("✅ Hugging Face API token is configured in Azure");
    }
}

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 AI Content Summarizer API v2.0.1 is running on Azure with enhanced resilience");

app.Run();

// ===================================================================
// Resilience Policy Definitions
// ===================================================================

/*
 * These policies define how our application handles network failures:
 * 
 * Retry Policy: Automatically retries failed requests with exponential backoff
 * Circuit Breaker: Temporarily stops making requests if too many fail consecutively
 */

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // 2, 4, 8 seconds
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                // Log retry attempts for debugging and monitoring
                Console.WriteLine($"🔄 Retry attempt {retryCount} for Hugging Face API after {timespan.TotalSeconds}s delay");
                
                // Additional logging for Azure diagnostics
                if (outcome.Exception != null)
                {
                    Console.WriteLine($"   Reason: {outcome.Exception.GetType().Name} - {outcome.Exception.Message}");
                }
                else if (outcome.Result != null)
                {
                    Console.WriteLine($"   HTTP Status: {outcome.Result.StatusCode}");
                }
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (result, timespan) =>
            {
                Console.WriteLine($"🚫 Circuit breaker OPENED for Hugging Face API. Will retry after {timespan.TotalSeconds} seconds.");
                Console.WriteLine("   This protects against cascading failures and gives the external service time to recover.");
            },
            onReset: () =>
            {
                Console.WriteLine($"✅ Circuit breaker CLOSED for Hugging Face API. Normal operation resumed.");
            });
}
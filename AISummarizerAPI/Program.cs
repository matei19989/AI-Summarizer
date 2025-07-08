// AISummarizerAPI/Program.cs - Container Strategy with Fixed CORS
using AISummarizerAPI.Configuration;
using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Application.Interfaces;
using AISummarizerAPI.Application.Services;
using AISummarizerAPI.Infrastructure.Services;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;
using System.Net;
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
// Core & Infrastructure Service Registration
// ===================================================================
builder.Services.AddScoped<IContentValidator, ContentValidationService>();
builder.Services.AddScoped<IResponseFormatter, ResponseFormatterService>();
builder.Services.AddScoped<IContentSummarizer, HuggingFaceContentSummarizer>();
builder.Services.AddScoped<IContentExtractor, SmartReaderContentExtractor>();
builder.Services.AddScoped<ISummarizationOrchestrator, SummarizationOrchestrator>();
builder.Services.AddScoped<IHuggingFaceApiClient, HuggingFaceApiClient>();

// ===================================================================
// HTTP Clients with Container Optimized Settings
// ===================================================================
builder.Services.AddHttpClient<IContentValidator, ContentValidationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(30);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.0 (Content Validator)");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    MaxConnectionsPerServer = 10,
    PooledConnectionLifetime = TimeSpan.FromMinutes(15)
});

builder.Services.AddHttpClient<IContentExtractor, SmartReaderContentExtractor>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(45);
    client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.0 (Content Extractor)");
})
.ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
{
    MaxConnectionsPerServer = 5
});

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
    client.DefaultRequestHeaders.ConnectionClose = false;
    client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=60");
})
.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    MaxConnectionsPerServer = 3,
    PooledConnectionLifetime = TimeSpan.FromMinutes(20),
    UseCookies = false,
    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
})
.AddPolicyHandler(GetRetryPolicy())
.AddPolicyHandler(GetCircuitBreakerPolicy());

// ===================================================================
// Framework Services
// ===================================================================
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// ===================================================================
// CORS Configuration - FIXED for Container Strategy
// ===================================================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("ContainerPolicy", corsBuilder =>
    {
        // Get allowed origins from environment
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
            // Development origins - includes Docker networking
            corsBuilder
                .WithOrigins(
                    "http://localhost:3000",           // Local dev
                    "http://localhost:5173",           // Vite dev server
                    "http://localhost:4173",           // Vite preview
                    "http://frontend:80",              // Docker container
                    "https://ai-summarizer-au3d83i5e-matei19989s-projects.vercel.app"  // Current Vercel
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
        else
        {
            // Production origins - FIXED for container deployment
            corsBuilder
                .WithOrigins(
                    "https://ai-summarizer-au3d83i5e-matei19989s-projects.vercel.app",  // Current Vercel URL
                    "https://ai-summarizer-theta-ten.vercel.app",                       // Old Vercel URL (backup)
                    "https://aisummarizer2026-bsech4f0cyh3akdw.northeurope-01.azurewebsites.net"  // Azure URL
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Length", "Content-Type");
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogInformation("Application starting in Development mode with Container Strategy");
}

app.UseCors("ContainerPolicy");  // Use container-specific CORS policy
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ===================================================================
// Root Endpoint for Container Monitoring
// ===================================================================
app.MapGet("/", (IServiceProvider serviceProvider) =>
{
    var huggingFaceOptions = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;

    return new
    {
        Application = "AI Content Summarizer API",
        Version = "2.1.0-Container-Strategy",
        Environment = app.Environment.EnvironmentName,
        Status = "Running with Full Container Deployment",
        Timestamp = DateTime.UtcNow,

        ContainerOptimizations = new
        {
            DeploymentStrategy = "Container-based with Azure Web App for Containers",
            Port = "80 (Container Standard)",
            NetworkResilience = "Enhanced with Polly retry and circuit breaker",
            CORS = "Configured for Vercel and Azure origins"
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
// Container Health Validation
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
            logger.LogInformation("✅ Container deployment is healthy and ready");
        }
        else
        {
            logger.LogWarning("⚠️ Some services unavailable - container will retry with resilience policies");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Container startup error - resilience policies will handle runtime issues");
    }

    var huggingFaceOptions = scope.ServiceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<HuggingFaceOptions>>().Value;
    if (string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
    {
        logger.LogWarning("⚠️ Hugging Face API token not configured - check container environment variables");
    }
    else
    {
        logger.LogInformation("✅ Hugging Face API token configured in container");
    }
}

var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
startupLogger.LogInformation("🚀 AI Content Summarizer v2.1.0 running with Full Container Strategy");

app.Run();

// ===================================================================
// Polly Resilience Policies
// ===================================================================
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return Policy
        .HandleResult<HttpResponseMessage>(r => !r.IsSuccessStatusCode)
        .Or<HttpRequestException>()
        .Or<TaskCanceledException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Console.WriteLine($"🔄 Container retry attempt {retryCount} after {timespan.TotalSeconds}s");
                if (outcome.Exception != null)
                    Console.WriteLine($"   Reason: {outcome.Exception.GetType().Name} - {outcome.Exception.Message}");
                else if (outcome.Result != null)
                    Console.WriteLine($"   HTTP Status: {outcome.Result.StatusCode}");
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
                Console.WriteLine($"🚫 Container circuit breaker OPENED. Will retry after {timespan.TotalSeconds}s.");
            },
            onReset: () =>
            {
                Console.WriteLine("✅ Container circuit breaker CLOSED. Normal operation resumed.");
            });
}
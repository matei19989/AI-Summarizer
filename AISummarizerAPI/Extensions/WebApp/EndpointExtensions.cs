// AISummarizerAPI/Extensions/WebApp/EndpointExtensions.cs
using AISummarizerAPI.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder;

namespace AISummarizerAPI.Extensions.WebApp;

/// <summary>
/// Extension methods for configuring application endpoints
/// Separates endpoint configuration from pipeline middleware
/// </summary>
public static class EndpointExtensions
{
    /// <summary>
    /// Configures all application endpoints
    /// </summary>
    public static Microsoft.AspNetCore.Builder.WebApplication ConfigureEndpoints(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.MapRootEndpoint();
        
        // Future: Add other endpoint groups
        // app.MapHealthCheckEndpoints();
        // app.MapMetricsEndpoints();
        
        return app;
    }

    /// <summary>
    /// Maps the root endpoint with application information
    /// Useful for container monitoring and service discovery
    /// </summary>
    private static Microsoft.AspNetCore.Builder.WebApplication MapRootEndpoint(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.MapGet("/", (IServiceProvider serviceProvider, IWebHostEnvironment environment) =>
        {
            var huggingFaceOptions = serviceProvider.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;

            return new
            {
                Application = "AI Content Summarizer API",
                Version = "2.1.0-Clean-Architecture",
                Environment = environment.EnvironmentName,
                Status = "Running with Clean Architecture",
                Timestamp = DateTime.UtcNow,

                Architecture = new
                {
                    Pattern = "Clean Architecture with SOLID Principles",
                    DeploymentStrategy = "Container-based with Azure Web App",
                    NetworkResilience = "Enhanced with Polly retry and circuit breaker",
                    CORS = "Environment-aware configuration"
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
        })
        .WithName("GetApplicationInfo")
        .WithTags("System")
        .WithOpenApi();

        return app;
    }
}
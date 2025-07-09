// AISummarizerAPI/Extensions/WebApp/HealthCheckExtensions.cs
using AISummarizerAPI.Application.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Builder;
using AISummarizerAPI.Extensions.ServiceCollection;

namespace AISummarizerAPI.Extensions.WebApp;

/// <summary>
/// Extension methods for configuring health checks and startup validation
/// Provides comprehensive application health monitoring
/// </summary>
public static class HealthCheckExtensions
{
    /// <summary>
    /// Configures health check endpoints and startup validation
    /// </summary>
    public static Microsoft.AspNetCore.Builder.WebApplication ConfigureHealthChecks(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        // Map health check endpoints
        app.MapHealthChecks("/health");

        return app;
    }

    /// <summary>
    /// Validates application health during startup
    /// Performs dependency checks and configuration validation
    /// </summary>
    public static async Task<Microsoft.AspNetCore.Builder.WebApplication> ValidateStartupAsync(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // Validate configuration
            scope.ServiceProvider.ValidateConfiguration();

            // Test critical dependencies
            await ValidateDependenciesAsync(scope.ServiceProvider, logger);

            logger.LogInformation("✅ Startup validation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Startup validation failed");
            // In production, you might want to throw here to prevent startup
            // throw;
        }

        return app;
    }

    /// <summary>
    /// Validates critical application dependencies
    /// </summary>
    private static async Task ValidateDependenciesAsync(IServiceProvider serviceProvider, ILogger logger)
    {
        try
        {
            // Test the orchestrator health (which tests AI service)
            var orchestrator = serviceProvider.GetRequiredService<ISummarizationOrchestrator>();
            var isHealthy = await orchestrator.IsHealthyAsync();

            if (isHealthy)
            {
                logger.LogInformation("✅ AI summarization service is healthy");
            }
            else
            {
                logger.LogWarning("⚠️ AI summarization service unavailable - will retry with resilience policies");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Dependency validation error - resilience policies will handle runtime issues");
        }
    }
}
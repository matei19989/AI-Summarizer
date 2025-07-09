// AISummarizerAPI/Extensions/WebApp/PipelineExtensions.cs
using Microsoft.AspNetCore.Builder;

namespace AISummarizerAPI.Extensions.WebApp;

/// <summary>
/// Extension methods for configuring the request pipeline
/// Organizes middleware configuration by environment and concern
/// </summary>
public static class PipelineExtensions
{
    /// <summary>
    /// Configures the complete request pipeline based on environment
    /// </summary>
    public static Microsoft.AspNetCore.Builder.WebApplication ConfigurePipeline(this Microsoft.AspNetCore.Builder.WebApplication app, IWebHostEnvironment environment)
    {
        if (environment.IsDevelopment())
        {
            app.ConfigureDevelopmentPipeline();
        }
        else
        {
            app.ConfigureProductionPipeline();
        }

        // Common pipeline configuration
        app.ConfigureCommonPipeline();

        return app;
    }

    /// <summary>
    /// Configures development-specific middleware
    /// More permissive and verbose for debugging
    /// </summary>
    private static Microsoft.AspNetCore.Builder.WebApplication ConfigureDevelopmentPipeline(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.UseDeveloperExceptionPage();
        app.MapOpenApi();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🛠️ Development pipeline configured with OpenAPI");

        return app;
    }

    /// <summary>
    /// Configures production-specific middleware
    /// Security-focused and performance-optimized
    /// </summary>
    private static Microsoft.AspNetCore.Builder.WebApplication ConfigureProductionPipeline(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();

        // Future: Add production middleware
        // app.UseResponseCompression();
        // app.UseResponseCaching();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("🔒 Production pipeline configured with security headers");

        return app;
    }

    /// <summary>
    /// Configures middleware common to all environments
    /// Order matters - this follows ASP.NET Core middleware order recommendations
    /// </summary>
    private static Microsoft.AspNetCore.Builder.WebApplication ConfigureCommonPipeline(this Microsoft.AspNetCore.Builder.WebApplication app)
    {
        // Security headers and CORS
        app.UseCors("ContainerPolicy");

        // HTTPS redirection
        app.UseHttpsRedirection();

        // Authentication and authorization (when implemented)
        // app.UseAuthentication();
        // app.UseAuthorization();

        // Controller routing
        app.MapControllers();

        return app;
    }
}
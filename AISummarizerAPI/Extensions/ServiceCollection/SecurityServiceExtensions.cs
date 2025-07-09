// AISummarizerAPI/Extensions/ServiceCollection/SecurityServiceExtensions.cs
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace AISummarizerAPI.Extensions.ServiceCollection;

/// <summary>
/// Extension methods for configuring security-related services
/// Handles CORS, authentication, and other security concerns
/// </summary>
public static class SecurityServiceExtensions
{
    /// <summary>
    /// Configures all security services including CORS
    /// </summary>
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddCorsConfiguration(configuration, environment);

        // Future: Add authentication, authorization, rate limiting, etc.
        // services.AddAuthentication();
        // services.AddRateLimiting();

        return services;
    }

    /// <summary>
    /// Configures CORS policies based on environment
    /// Secure by default, permissive only in development
    /// </summary>
    private static IServiceCollection AddCorsConfiguration(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("ContainerPolicy", corsBuilder =>
            {
                if (environment.IsDevelopment())
                {
                    ConfigureDevelopmentCors(corsBuilder);
                }
                else
                {
                    ConfigureProductionCors(corsBuilder, configuration);
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Configures permissive CORS for development environment
    /// Includes Docker networking and common development ports
    /// </summary>
    private static void ConfigureDevelopmentCors(CorsPolicyBuilder corsBuilder)
    {
        corsBuilder
            .WithOrigins(
                "http://localhost:3000",           // Local React dev
                "http://localhost:5173",           // Vite dev server
                "http://localhost:4173",           // Vite preview
                "http://frontend:80",              // Docker container
                "https://ai-summarizer-au3d83i5e-matei19989s-projects.vercel.app"  // Current Vercel
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowedToAllowWildcardSubdomains();
    }

    /// <summary>
    /// Configures strict CORS for production environment
    /// Only allows specific, known origins
    /// </summary>
    private static void ConfigureProductionCors(CorsPolicyBuilder corsBuilder, IConfiguration configuration)
    {
        // Get allowed origins from environment configuration
        var allowedOrigins = configuration.GetSection("ASPNETCORE_ALLOWEDORIGINS").Get<string>();

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
        else
        {
            // Fallback to known production origins
            corsBuilder
                .WithOrigins(
                    "https://ai-summarizer-au3d83i5e-matei19989s-projects.vercel.app",  // Current Vercel URL
                    "https://ai-summarizer-theta-ten.vercel.app",                       // Old Vercel URL (backup)
                    "https://aisummarizer2026-bsech4f0cyh3akdw.northeurope-01.azurewebsites.net",  // Azure URL
                    "https://ai-summarizer-ge8m4p474-matei19989s-projects.vercel.app",  // ✅ add this
                    "https://ai-summarizer-k313u6jh6-matei19989s-projects.vercel.app"
                )
                .AllowAnyMethod()
                .AllowAnyHeader()
                .WithExposedHeaders("Content-Length", "Content-Type");
        }
    }
}

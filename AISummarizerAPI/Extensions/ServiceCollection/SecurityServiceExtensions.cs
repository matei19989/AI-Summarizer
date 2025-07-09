// Extensions/ServiceCollection/SecurityServiceExtensions.cs - FIXED VERSION
using Microsoft.AspNetCore.Cors.Infrastructure;

namespace AISummarizerAPI.Extensions.ServiceCollection;

public static class SecurityServiceExtensions
{
    public static IServiceCollection AddSecurityServices(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        services.AddCorsConfiguration(configuration, environment);
        return services;
    }

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

    private static void ConfigureDevelopmentCors(CorsPolicyBuilder corsBuilder)
    {
        corsBuilder
            .WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://localhost:4173",
                "http://frontend:80"
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials()
            .SetIsOriginAllowedToAllowWildcardSubdomains();
    }

    private static void ConfigureProductionCors(CorsPolicyBuilder corsBuilder, IConfiguration configuration)
    {
        // Check for explicit allowed origins first
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
            // FIXED: More flexible Vercel URL matching
            corsBuilder
                .SetIsOriginAllowed(origin =>
                {
                    if (string.IsNullOrEmpty(origin))
                        return false;

                    // Allow all Vercel deployments for this project
                    return origin.Contains("matei19989s-projects.vercel.app") ||
                           origin.Contains("ai-summarizer-theta-ten.vercel.app") ||
                           origin.Contains("aisummarizer2026-bsech4f0cyh3akdw.northeurope-01.azurewebsites.net");
                })
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }
    }
}
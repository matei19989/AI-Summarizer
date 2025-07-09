// AISummarizerAPI/Extensions/ServiceCollection/ConfigurationExtensions.cs
using AISummarizerAPI.Configuration;
using Microsoft.Extensions.Options;

namespace AISummarizerAPI.Extensions.ServiceCollection;

/// <summary>
/// Extension methods for configuration setup and validation
/// Follows Single Responsibility Principle - only handles configuration concerns
/// </summary>
public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures and validates all application configuration sections
    /// </summary>
    public static IServiceCollection AddConfiguration(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure HuggingFace options with validation
        services.Configure<HuggingFaceOptions>(configuration.GetSection(HuggingFaceOptions.SectionName));

        services.AddOptions<HuggingFaceOptions>()
            .Bind(configuration.GetSection(HuggingFaceOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(options => !string.IsNullOrEmpty(options.ApiToken),
                     "HuggingFace API token is required")
            .Validate(options => options.RateLimit.RequestsPerMinute > 0,
                     "Rate limit must be greater than 0")
            .ValidateOnStart();

        return services;
    }

    /// <summary>
    /// Validates critical configuration at startup
    /// </summary>
    public static void ValidateConfiguration(this IServiceProvider services)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();

        try
        {
            var huggingFaceOptions = services.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;

            if (string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
            {
                logger.LogWarning("⚠️ HuggingFace API token not configured - check environment variables");
            }
            else
            {
                logger.LogInformation("✅ HuggingFace API token configured");
            }

            logger.LogInformation("✅ Configuration validation completed");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ Configuration validation failed");
            throw;
        }
    }
}
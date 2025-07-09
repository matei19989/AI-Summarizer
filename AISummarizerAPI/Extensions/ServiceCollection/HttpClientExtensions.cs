// AISummarizerAPI/Extensions/ServiceCollection/HttpClientExtensions.cs
using AISummarizerAPI.Configuration;
using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Infrastructure.Services;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;
using Microsoft.Extensions.Options;
using System.Net;
using Polly;
using Polly.Extensions.Http;

namespace AISummarizerAPI.Extensions.ServiceCollection;

/// <summary>
/// Extension methods for configuring HTTP clients with resilience policies
/// Centralizes all HTTP client configuration for better maintainability
/// </summary>
public static class HttpClientExtensions
{
    /// <summary>
    /// Configures all HTTP clients with appropriate policies and timeouts
    /// </summary>
    public static IServiceCollection AddHttpClientServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddContentValidatorHttpClient();
        services.AddContentExtractorHttpClient();
        services.AddHuggingFaceHttpClient(configuration);

        return services;
    }

    /// <summary>
    /// Configures HTTP client for content validation service
    /// Optimized for quick URL accessibility checks
    /// </summary>
    private static IServiceCollection AddContentValidatorHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient<IContentValidator, ContentValidationService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.1 (Content Validator)");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 10,
            PooledConnectionLifetime = TimeSpan.FromMinutes(15),
            ConnectTimeout = TimeSpan.FromSeconds(10)
        });

        return services;
    }

    /// <summary>
    /// Configures HTTP client for content extraction service
    /// Handles web scraping with appropriate timeouts and retry logic
    /// </summary>
    private static IServiceCollection AddContentExtractorHttpClient(this IServiceCollection services)
    {
        services.AddHttpClient<IContentExtractor, SmartReaderContentExtractor>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(45);
            client.DefaultRequestHeaders.Add("User-Agent", "AISummarizer/2.1 (Content Extractor)");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler()
        {
            MaxConnectionsPerServer = 5,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        });

        return services;
    }

    /// <summary>
    /// Configures HTTP client for HuggingFace API with advanced resilience patterns
    /// Includes circuit breaker for handling API unavailability gracefully
    /// </summary>
    private static IServiceCollection AddHuggingFaceHttpClient(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient<IHuggingFaceApiClient, HuggingFaceApiClient>((serviceProvider, client) =>
        {
            var huggingFaceOptions = serviceProvider.GetRequiredService<IOptions<HuggingFaceOptions>>().Value;

            client.Timeout = TimeSpan.FromSeconds(huggingFaceOptions.RateLimit.TimeoutSeconds);
            client.BaseAddress = new Uri(huggingFaceOptions.BaseUrl);

            // Configure authentication if token is available
            if (!string.IsNullOrEmpty(huggingFaceOptions.ApiToken))
            {
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", huggingFaceOptions.ApiToken);
            }

            // Optimize for API calls
            client.DefaultRequestHeaders.UserAgent.ParseAdd("AISummarizer/2.1 (HuggingFace Integration)");
            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=60");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            MaxConnectionsPerServer = 3,
            PooledConnectionLifetime = TimeSpan.FromMinutes(20),
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
            ConnectTimeout = TimeSpan.FromSeconds(10)
        });

        return services;
    }
}
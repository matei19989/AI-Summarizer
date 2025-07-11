// AISummarizerAPI/Extensions/ServiceCollection/InfrastructureServiceExtensions.cs
using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Infrastructure.Services;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;
using AISummarizerAPI.Infrastructure.Interfaces;
using Microsoft.Extensions.Options;
using AISummarizerAPI.Configuration;

namespace AISummarizerAPI.Extensions.ServiceCollection;

/// <summary>
/// Extension methods for registering Infrastructure Layer services
/// These services handle external concerns like AI APIs, content extraction, etc.
/// </summary>
public static class InfrastructureServiceExtensions
{
    /// <summary>
    /// Registers all infrastructure services with proper scoping
    /// </summary>
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services)
    {
        // Core domain services
        services.AddScoped<IContentValidator, ContentValidationService>();
        services.AddScoped<IContentExtractor, SmartReaderContentExtractor>();
        services.AddScoped<IContentSummarizer, HuggingFaceContentSummarizer>();
        services.AddScoped<IResponseFormatter, ResponseFormatterService>();

        // HuggingFace infrastructure services
        services.AddScoped<IRateLimitService>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<HuggingFaceOptions>>();
            return new RateLimitService(options.Value);
        });

        services.AddScoped<IRetryPolicyService, RetryPolicyService>();
        services.AddScoped<IHuggingFaceRequestBuilder, HuggingFaceRequestBuilder>();
        services.AddScoped<IHuggingFaceResponseProcessor, HuggingFaceResponseProcessor>();

        // Main HuggingFace client
        services.AddScoped<IHuggingFaceApiClient, HuggingFaceApiClient>();

        return services;
    }

}
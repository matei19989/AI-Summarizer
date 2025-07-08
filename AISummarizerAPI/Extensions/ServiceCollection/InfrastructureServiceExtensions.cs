// AISummarizerAPI/Extensions/ServiceCollection/InfrastructureServiceExtensions.cs
using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Infrastructure.Services;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Services.Implementations;

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
        // Core domain services - these implement our business capabilities
        services.AddScoped<IContentValidator, ContentValidationService>();
        services.AddScoped<IContentExtractor, SmartReaderContentExtractor>();
        services.AddScoped<IContentSummarizer, HuggingFaceContentSummarizer>();
        services.AddScoped<IResponseFormatter, ResponseFormatterService>();
        
        // External API clients
        services.AddScoped<IHuggingFaceApiClient, HuggingFaceApiClient>();

        return services;
    }
}
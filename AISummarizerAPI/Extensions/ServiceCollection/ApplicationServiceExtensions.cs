// AISummarizerAPI/Extensions/ServiceCollection/ApplicationServiceExtensions.cs
using AISummarizerAPI.Application.Interfaces;
using AISummarizerAPI.Application.Services;

namespace AISummarizerAPI.Extensions.ServiceCollection;

/// <summary>
/// Extension methods for registering Application Layer services
/// Contains the core business logic orchestration services
/// </summary>
public static class ApplicationServiceExtensions
{
    /// <summary>
    /// Registers all application layer services
    /// These are the services that contain business logic and orchestration
    /// </summary>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register the main orchestrator - this is our primary use case coordinator
        services.AddScoped<ISummarizationOrchestrator, SummarizationOrchestrator>();

        return services;
    }
}
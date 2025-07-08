// AISummarizerAPI/Extensions/ServiceCollection/FrameworkServiceExtensions.cs
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AISummarizerAPI.Extensions.ServiceCollection;

/// <summary>
/// Extension methods for registering ASP.NET Core framework services
/// Keeps framework concerns separate from business logic registration
/// </summary>
public static class FrameworkServiceExtensions
{
    /// <summary>
    /// Registers all ASP.NET Core framework services
    /// </summary>
    public static IServiceCollection AddFrameworkServices(this IServiceCollection services)
    {
        // MVC and API services
        services.AddControllers();
        
        // OpenAPI/Swagger for development
        services.AddOpenApi();
        
        // Health checks for monitoring
        services.AddHealthChecks();
        
        // Future framework services can be added here:
        // services.AddProblemDetails();
        // services.AddApiVersioning();
        // services.AddResponseCompression();
        
        return services;
    }
}
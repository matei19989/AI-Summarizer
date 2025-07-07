namespace AISummarizerAPI.Core.Interfaces;

using AISummarizerAPI.Core.Models;

/// <summary>
/// Interface for formatting responses for different consumers
/// Separated because formatting logic is different from business logic
/// Allows us to easily add new output formats without changing core services
/// </summary>
public interface IResponseFormatter
{
    /// <summary>
    /// Formats a summarization result for API consumption
    /// Handles different success/failure scenarios consistently
    /// </summary>
    T FormatResponse<T>(SummarizationResult result) where T : class;
    
    /// <summary>
    /// Formats validation errors in a user-friendly way
    /// Consistent error formatting across all endpoints
    /// </summary>
    T FormatValidationError<T>(ValidationResult validationResult) where T : class;
    
    /// <summary>
    /// Formats system errors (exceptions, infrastructure failures)
    /// Ensures we never leak internal details to the API consumer
    /// </summary>
    T FormatSystemError<T>(string userFriendlyMessage) where T : class;
}
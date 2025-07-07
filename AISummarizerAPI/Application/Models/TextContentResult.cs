namespace AISummarizerAPI.Application.Models;

using AISummarizerAPI.Core.Models;

/// <summary>
/// Internal application model for text content retrieval results
/// This is a private concern of the application layer - external layers don't need to know about it
/// </summary>
internal class TextContentResult
{
    public string Content { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    
    public bool IsFailure => !Success;
    
    public static TextContentResult Success(string content)
    {
        return new TextContentResult
        {
            Content = content,
            Success = true
        };
    }
    
    public static TextContentResult Failure(string errorMessage)
    {
        return new TextContentResult
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
    
    /// <summary>
    /// Converts to domain model for external consumption
    /// This encapsulates the mapping logic within the application layer
    /// </summary>
    public SummarizationResult ToSummarizationResult(string sourceType)
    {
        return Success 
            ? SummarizationResult.Success(Content, sourceType, TimeSpan.Zero)
            : SummarizationResult.Failure(ErrorMessage ?? "Unknown error", sourceType);
    }
}
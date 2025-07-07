namespace AISummarizerAPI.Infrastructure.Services;

using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Core.Models;
using AISummarizerAPI.Models.DTOs;

public class ResponseFormatterService : IResponseFormatter
{
    public T FormatResponse<T>(SummarizationResult result) where T : class
    {
        if (typeof(T) == typeof(SummarizationResponse))
        {
            var response = new SummarizationResponse
            {
                Summary = result.Summary,
                Success = result.Success,
                ErrorMessage = result.ErrorMessage,
                ProcessedContentType = result.SourceType,
                GeneratedAt = result.GeneratedAt,
                HasAudio = true
            };

            return response as T ?? throw new InvalidOperationException("Failed to cast response");
        }

        throw new NotSupportedException($"Response type {typeof(T).Name} is not supported");
    }

    public T FormatValidationError<T>(ValidationResult validationResult) where T : class
    {
        if (typeof(T) == typeof(SummarizationResponse))
        {
            var response = new SummarizationResponse
            {
                Success = false,
                ErrorMessage = validationResult.ErrorMessage,
                ProcessedContentType = "validation_error"
            };

            return response as T ?? throw new InvalidOperationException("Failed to cast validation error response");
        }

        throw new NotSupportedException($"Response type {typeof(T).Name} is not supported for validation errors");
    }

    public T FormatSystemError<T>(string userFriendlyMessage) where T : class
    {
        if (typeof(T) == typeof(SummarizationResponse))
        {
            var response = new SummarizationResponse
            {
                Success = false,
                ErrorMessage = userFriendlyMessage,
                ProcessedContentType = "system_error"
            };

            return response as T ?? throw new InvalidOperationException("Failed to cast system error response");
        }

        throw new NotSupportedException($"Response type {typeof(T).Name} is not supported for system errors");
    }
}
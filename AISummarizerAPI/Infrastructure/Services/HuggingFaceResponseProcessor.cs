using System.Text.Json;
using System.Text.Json.Serialization;
using AISummarizerAPI.Infrastructure.Interfaces;
using AISummarizerAPI.Models.HuggingFace;

namespace AISummarizerAPI.Infrastructure.Services;

public class HuggingFaceResponseProcessor : IHuggingFaceResponseProcessor
{
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<HuggingFaceResponseProcessor> _logger;

    public HuggingFaceResponseProcessor(ILogger<HuggingFaceResponseProcessor> logger)
    {
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<HuggingFaceSummarizationResponse> ProcessSummarizationResponseAsync(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        _logger.LogDebug("Received response with status: {StatusCode}, content length: {ContentLength}",
            response.StatusCode, responseContent.Length);

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var rawResponses = JsonSerializer.Deserialize<HuggingFaceRawResponse[]>(responseContent, _jsonOptions);

                if (rawResponses?.Length > 0 && !string.IsNullOrEmpty(rawResponses[0].SummaryText))
                {
                    _logger.LogInformation("Successfully received summary of length: {Length}", rawResponses[0].SummaryText.Length);

                    return new HuggingFaceSummarizationResponse
                    {
                        SummaryText = rawResponses[0].SummaryText.Trim(),
                        Success = true,
                        StatusCode = statusCode,
                        ProcessingTime = TimeSpan.Zero
                    };
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse successful response from Hugging Face API");
            }
        }

        return HandleErrorResponse(response, responseContent);
    }

    public async Task<HuggingFaceApiStatus> ProcessApiStatusResponseAsync(HttpResponseMessage response)
    {
        var responseContent = await response.Content.ReadAsStringAsync();
        var statusCode = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            return new HuggingFaceApiStatus
            {
                IsAvailable = true,
                IsLoading = false,
                StatusCode = statusCode,
                StatusMessage = "API is available and ready"
            };
        }

        if (statusCode == 503)
        {
            try
            {
                var errorResponse = JsonSerializer.Deserialize<HuggingFaceErrorResponse>(responseContent, _jsonOptions);
                if (errorResponse?.EstimatedTime.HasValue == true)
                {
                    return new HuggingFaceApiStatus
                    {
                        IsAvailable = false,
                        IsLoading = true,
                        EstimatedLoadTime = TimeSpan.FromSeconds(errorResponse.EstimatedTime.Value),
                        StatusCode = statusCode,
                        StatusMessage = "Model is loading"
                    };
                }
            }
            catch (JsonException) { }
        }

        return new HuggingFaceApiStatus
        {
            IsAvailable = false,
            IsLoading = false,
            StatusCode = statusCode,
            StatusMessage = $"API unavailable (Status: {statusCode})"
        };
    }

    private HuggingFaceSummarizationResponse HandleErrorResponse(HttpResponseMessage response, string responseContent)
    {
        var statusCode = (int)response.StatusCode;
        _logger.LogWarning("Received error response from Hugging Face API: {StatusCode}", statusCode);

        try
        {
            var errorResponse = JsonSerializer.Deserialize<HuggingFaceErrorResponse>(responseContent, _jsonOptions);
            if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.Error))
            {
                var errorMessage = GetFriendlyErrorMessage(errorResponse.Error, statusCode);
                _logger.LogError("Hugging Face API error: {Error}", errorResponse.Error);

                return new HuggingFaceSummarizationResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    StatusCode = statusCode
                };
            }
        }
        catch (JsonException)
        {
            _logger.LogWarning("Could not parse error response from Hugging Face API");
        }

        var fallbackMessage = statusCode switch
        {
            401 => "Invalid API token. Please check your Hugging Face authentication.",
            403 => "Access forbidden. Please verify your API token permissions.",
            429 => "Rate limit exceeded. Please wait before making another request.",
            503 => "Model is currently loading. Please try again in a few moments.",
            _ => $"The summarization service is temporarily unavailable (Status: {statusCode})"
        };

        return new HuggingFaceSummarizationResponse
        {
            Success = false,
            ErrorMessage = fallbackMessage,
            StatusCode = statusCode
        };
    }

    private static string GetFriendlyErrorMessage(string apiError, int statusCode)
    {
        if (apiError.Contains("loading", StringComparison.OrdinalIgnoreCase))
            return "The AI model is warming up. Please try again in a few moments.";

        if (apiError.Contains("rate limit", StringComparison.OrdinalIgnoreCase) || statusCode == 429)
            return "Too many requests. Please wait a moment before trying again.";

        if (apiError.Contains("token", StringComparison.OrdinalIgnoreCase) || statusCode == 401)
            return "Authentication failed. Please check the API configuration.";

        if (apiError.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return "The request took too long to process. Please try with shorter content.";

        return "The summarization service encountered an error. Please try again.";
    }
}
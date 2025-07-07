// AISummarizerAPI/Models/HuggingFace/HuggingFaceModels.cs
using System.Text.Json.Serialization;

namespace AISummarizerAPI.Models.HuggingFace;

/// <summary>
/// Request model for Hugging Face summarization API
/// Maps to the expected JSON structure for the inference API
/// </summary>
public class HuggingFaceSummarizationRequest
{
    /// <summary>
    /// The input text to be summarized
    /// </summary>
    [JsonPropertyName("inputs")]
    public string Inputs { get; set; } = string.Empty;

    /// <summary>
    /// Optional parameters for fine-tuning the summarization
    /// </summary>
    [JsonPropertyName("parameters")]
    public SummarizationParameters? Parameters { get; set; }

    /// <summary>
    /// Options for how the API should behave
    /// </summary>
    [JsonPropertyName("options")]
    public ApiOptions? Options { get; set; }
}

/// <summary>
/// Parameters for controlling summarization behavior
/// These affect the quality and characteristics of the generated summary
/// </summary>
public class SummarizationParameters
{
    /// <summary>
    /// Maximum length of the generated summary
    /// Helps control output size for consistent UI display
    /// </summary>
    [JsonPropertyName("max_length")]
    public int? MaxLength { get; set; }

    /// <summary>
    /// Minimum length of the generated summary
    /// Ensures summaries have sufficient detail
    /// </summary>
    [JsonPropertyName("min_length")]
    public int? MinLength { get; set; }

    /// <summary>
    /// Whether to do sampling during generation
    /// False typically gives more consistent, deterministic results
    /// </summary>
    [JsonPropertyName("do_sample")]
    public bool? DoSample { get; set; }

    /// <summary>
    /// Controls randomness in generation (0.0 = deterministic, 1.0 = very random)
    /// Lower values give more focused, consistent summaries
    /// </summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }
}

/// <summary>
/// Options for API behavior and caching
/// </summary>
public class ApiOptions
{
    /// <summary>
    /// Whether to wait for the model to load if it's not ready
    /// True ensures we get results but may increase response time
    /// </summary>
    [JsonPropertyName("wait_for_model")]
    public bool WaitForModel { get; set; } = true;

    /// <summary>
    /// Whether to use cached results
    /// False ensures fresh results for each request
    /// </summary>
    [JsonPropertyName("use_cache")]
    public bool UseCache { get; set; } = true;
}

/// <summary>
/// Response model from Hugging Face summarization API
/// The API returns an array of results, we typically use the first one
/// </summary>
public class HuggingFaceSummarizationResponse
{
    /// <summary>
    /// Array of summarization results
    /// Usually contains one result, but API can return multiple options
    /// </summary>
    [JsonPropertyName("summary_text")]
    public string SummaryText { get; set; } = string.Empty;

    /// <summary>
    /// Indicates if the request was successful
    /// </summary>
    public bool Success { get; set; } = true;

    /// <summary>
    /// Error message if the request failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// HTTP status code from the API response
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Time taken for the API to process the request
    /// </summary>
    public TimeSpan ProcessingTime { get; set; }
}

/// <summary>
/// Raw response structure from Hugging Face API
/// Used for deserialization before mapping to our response model
/// </summary>
public class HuggingFaceRawResponse
{
    [JsonPropertyName("summary_text")]
    public string SummaryText { get; set; } = string.Empty;
}

/// <summary>
/// Error response structure from Hugging Face API
/// Used when the API returns error information
/// </summary>
public class HuggingFaceErrorResponse
{
    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    [JsonPropertyName("estimated_time")]
    public double? EstimatedTime { get; set; }

    [JsonPropertyName("warnings")]
    public string[]? Warnings { get; set; }
}

/// <summary>
/// API status information for monitoring and health checks
/// </summary>
public class HuggingFaceApiStatus
{
    /// <summary>
    /// Whether the API is currently available for requests
    /// </summary>
    public bool IsAvailable { get; set; }

    /// <summary>
    /// Whether the model is currently loading (cold start)
    /// </summary>
    public bool IsLoading { get; set; }

    /// <summary>
    /// Estimated time until the model is ready (if loading)
    /// </summary>
    public TimeSpan? EstimatedLoadTime { get; set; }

    /// <summary>
    /// Any status messages from the API
    /// </summary>
    public string? StatusMessage { get; set; }

    /// <summary>
    /// HTTP status code from the status check
    /// </summary>
    public int StatusCode { get; set; }
}
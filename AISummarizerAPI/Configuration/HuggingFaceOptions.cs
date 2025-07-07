// AISummarizerAPI/Configuration/HuggingFaceOptions.cs
namespace AISummarizerAPI.Configuration;

/// <summary>
/// Configuration options for Hugging Face API integration
/// Follows the Options pattern for clean configuration management
/// </summary>
public class HuggingFaceOptions
{
    public const string SectionName = "HuggingFace";

    /// <summary>
    /// API token for Hugging Face authentication
    /// Should be stored in user secrets for development and environment variables for production
    /// </summary>
    public string ApiToken { get; set; } = string.Empty;

    /// <summary>
    /// Base URL for Hugging Face Inference API
    /// </summary>
    public string BaseUrl { get; set; } = "https://api-inference.huggingface.co";

    /// <summary>
    /// Model configuration for different AI tasks
    /// </summary>
    public ModelConfiguration Models { get; set; } = new();

    /// <summary>
    /// Rate limiting and retry configuration
    /// </summary>
    public RateLimitConfiguration RateLimit { get; set; } = new();
}

public class ModelConfiguration
{
    /// <summary>
    /// Model used for text summarization
    /// facebook/bart-large-cnn is excellent for news article summarization
    /// </summary>
    public string SummarizationModel { get; set; } = "facebook/bart-large-cnn";

    /// <summary>
    /// Model for text-to-speech (future implementation)
    /// </summary>
    public string TextToSpeechModel { get; set; } = "microsoft/speecht5_tts";
}

public class RateLimitConfiguration
{
    /// <summary>
    /// Maximum requests per minute to avoid hitting rate limits
    /// </summary>
    public int RequestsPerMinute { get; set; } = 30;

    /// <summary>
    /// Timeout for HTTP requests in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Number of retry attempts for failed requests
    /// </summary>
    public int MaxRetryAttempts { get; set; } = 3;

    /// <summary>
    /// Base delay between retries in milliseconds
    /// Will be exponentially increased for each retry
    /// </summary>
    public int BaseRetryDelayMs { get; set; } = 1000;
}
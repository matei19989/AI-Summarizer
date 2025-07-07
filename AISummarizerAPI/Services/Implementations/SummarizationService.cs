// AISummarizerAPI/Services/Implementations/SummarizationService.cs
using AISummarizerAPI.Models.DTOs;
using AISummarizerAPI.Services.Interfaces;
using AISummarizerAPI.Utils;
using System.Text;
using System.Text.RegularExpressions;

namespace AISummarizerAPI.Services.Implementations;

/// <summary>
/// Enhanced implementation of summarization service with real AI integration
/// This service now orchestrates between URL extraction and Hugging Face AI summarization
/// Maintains the same interface but now provides genuine AI-powered summaries
/// Follows Single Responsibility Principle - coordinates between services without handling HTTP details
/// </summary>
public class SummarizationService : ISummarizationService
{
    private readonly ILogger<SummarizationService> _logger;
    private readonly HttpClient _httpClient;
    private readonly IUrlContentExtractor _urlContentExtractor;
    private readonly IHuggingFaceApiClient _huggingFaceClient;

    /// <summary>
    /// Enhanced constructor now includes the Hugging Face API client
    /// Demonstrates Dependency Injection principle with multiple services
    /// Each service has a specific responsibility in the summarization pipeline
    /// </summary>
    public SummarizationService(
        ILogger<SummarizationService> logger,
        HttpClient httpClient,
        IUrlContentExtractor urlContentExtractor,
        IHuggingFaceApiClient huggingFaceClient)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _urlContentExtractor = urlContentExtractor ?? throw new ArgumentNullException(nameof(urlContentExtractor));
        _huggingFaceClient = huggingFaceClient ?? throw new ArgumentNullException(nameof(huggingFaceClient));
    }

    /// <summary>
    /// Enhanced text summarization using real Hugging Face AI
    /// Now provides genuine AI-powered summaries instead of mock responses
    /// Maintains the same interface contract for seamless frontend integration
    /// </summary>
    public async Task<SummarizationResponse> SummarizeTextAsync(string content, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI-powered text summarization for content length: {ContentLength}", content.Length);

        try
        {
            // Validate input using existing validation logic
            var (isValid, errorMessage) = ValidateContent(content, "text");
            if (!isValid)
            {
                _logger.LogWarning("Text validation failed: {ErrorMessage}", errorMessage);
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    ProcessedContentType = "text"
                };
            }

            // Pre-process content for optimal AI summarization
            var processedContent = PreprocessTextForSummarization(content);
            _logger.LogDebug("Preprocessed content length: {Length}", processedContent.Length);

            // Call Hugging Face API for real AI summarization
            _logger.LogInformation("Calling Hugging Face API for text summarization");
            var huggingFaceResponse = await _huggingFaceClient.SummarizeTextAsync(processedContent, cancellationToken);

            if (!huggingFaceResponse.Success)
            {
                _logger.LogError("Hugging Face API call failed: {ErrorMessage}", huggingFaceResponse.ErrorMessage);
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = GetUserFriendlyErrorMessage(huggingFaceResponse.ErrorMessage, "text"),
                    ProcessedContentType = "text"
                };
            }

            // Post-process the AI-generated summary for better presentation
            var finalSummary = PostprocessSummary(huggingFaceResponse.SummaryText);
            
            _logger.LogInformation("Successfully generated AI summary of length: {SummaryLength} from content length: {ContentLength}", 
                finalSummary.Length, content.Length);

            return new SummarizationResponse
            {
                Summary = finalSummary,
                HasAudio = true, // TTS will be implemented in next phase
                Success = true,
                ProcessedContentType = "text",
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Text summarization was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI text summarization");
            return new SummarizationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while generating your summary. Please try again.",
                ProcessedContentType = "text"
            };
        }
    }

    /// <summary>
    /// Enhanced URL summarization combining content extraction with AI summarization
    /// Demonstrates service orchestration - coordinates between URL extraction and AI processing
    /// Now provides real AI summaries of extracted web content
    /// </summary>
    public async Task<SummarizationResponse> SummarizeUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI-powered URL summarization for: {Url}", LogSanitizer.SanitizeAndTruncate(url, 200));

        try
        {
            // Validate input using existing validation logic
            var (isValid, errorMessage) = ValidateContent(url, "url");
            if (!isValid)
            {
                _logger.LogWarning("URL validation failed: {ErrorMessage}", errorMessage);
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = errorMessage,
                    ProcessedContentType = "url"
                };
            }

            // Step 1: Extract content from URL (existing functionality)
            _logger.LogInformation("Extracting content from URL...");
            var extractionResult = await _urlContentExtractor.ExtractContentAsync(url, cancellationToken);

            if (!extractionResult.Success)
            {
                _logger.LogWarning("URL content extraction failed: {ErrorMessage}", extractionResult.ErrorMessage);
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = extractionResult.ErrorMessage,
                    ProcessedContentType = "url"
                };
            }

            _logger.LogInformation("Successfully extracted {ContentLength} characters from URL", extractionResult.Content.Length);

            // Step 2: Pre-process extracted content for AI summarization
            var processedContent = PreprocessTextForSummarization(extractionResult.Content);
            
            // Validate that extracted content is suitable for summarization
            if (processedContent.Length < 50)
            {
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = "The extracted content is too short for meaningful summarization.",
                    ProcessedContentType = "url"
                };
            }

            // Step 3: Generate AI summary using Hugging Face
            _logger.LogInformation("Generating AI summary for extracted URL content");
            var huggingFaceResponse = await _huggingFaceClient.SummarizeTextAsync(processedContent, cancellationToken);

            if (!huggingFaceResponse.Success)
            {
                _logger.LogError("Hugging Face API call failed for URL content: {ErrorMessage}", huggingFaceResponse.ErrorMessage);
                return new SummarizationResponse
                {
                    Success = false,
                    ErrorMessage = GetUserFriendlyErrorMessage(huggingFaceResponse.ErrorMessage, "url"),
                    ProcessedContentType = "url"
                };
            }

            // Step 4: Create enhanced summary with metadata
            var enhancedSummary = CreateEnhancedUrlSummary(
                huggingFaceResponse.SummaryText, 
                extractionResult,
                huggingFaceResponse.ProcessingTime);

            _logger.LogInformation("Successfully generated AI summary for URL content");

            return new SummarizationResponse
            {
                Summary = enhancedSummary,
                HasAudio = true, // TTS will be implemented in next phase
                Success = true,
                ProcessedContentType = "url",
                GeneratedAt = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("URL summarization was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI URL summarization");
            return new SummarizationResponse
            {
                Success = false,
                ErrorMessage = "An unexpected error occurred while processing the URL. Please try again.",
                ProcessedContentType = "url"
            };
        }
    }

    /// <summary>
    /// Existing validation logic - unchanged to maintain compatibility
    /// Validates content based on type with appropriate rules for each input method
    /// </summary>
    public (bool IsValid, string ErrorMessage) ValidateContent(string content, string contentType)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            return (false, "Content cannot be empty");
        }

        switch (contentType.ToLowerInvariant())
        {
            case "text":
                if (content.Length < 50)
                {
                    return (false, "Text content must be at least 50 characters for meaningful AI summarization");
                }
                if (content.Length > 10000)
                {
                    return (false, "Text content exceeds maximum length of 10,000 characters. Please shorten your text.");
                }
                break;

            case "url":
                var urlPattern = @"^https?://[^\s/$.?#].[^\s]*$";
                if (!Regex.IsMatch(content, urlPattern, RegexOptions.IgnoreCase))
                {
                    return (false, "Please provide a valid URL starting with http:// or https://");
                }
                break;

            default:
                return (false, "Content type must be either 'text' or 'url'");
        }

        return (true, string.Empty);
    }

    /// <summary>
    /// Preprocesses text content to optimize it for AI summarization
    /// Cleans up common formatting issues and ensures optimal input for the AI model
    /// </summary>
    private static string PreprocessTextForSummarization(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        // Remove excessive whitespace and normalize line breaks
        var processed = Regex.Replace(content, @"\s+", " ").Trim();
        
        // Remove common patterns that don't add value to summaries
        processed = Regex.Replace(processed, @"\b(Click here|Read more|Subscribe|Advertisement)\b", "", RegexOptions.IgnoreCase);
        
        // Ensure content doesn't exceed optimal length for the model
        // BART works best with content under 1024 tokens (roughly 4000 characters)
        if (processed.Length > 4000)
        {
            // Intelligently truncate by taking the first portion
            // This preserves the most important content which is typically at the beginning
            processed = processed.Substring(0, 4000);
            
            // Try to end at a sentence boundary for better results
            var lastPeriod = processed.LastIndexOf('.');
            if (lastPeriod > 3000) // Only use sentence boundary if it's reasonably long
            {
                processed = processed.Substring(0, lastPeriod + 1);
            }
        }

        return processed;
    }

    /// <summary>
    /// Post-processes AI-generated summaries to improve readability and presentation
    /// Ensures consistent formatting and quality for the user interface
    /// </summary>
    private static string PostprocessSummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return "Unable to generate summary.";

        // Clean up the summary text
        var processed = summary.Trim();
        
        // Ensure proper capitalization at the start
        if (processed.Length > 0 && char.IsLower(processed[0]))
        {
            processed = char.ToUpper(processed[0]) + processed.Substring(1);
        }
        
        // Ensure summary ends with proper punctuation
        if (!processed.EndsWith('.') && !processed.EndsWith('!') && !processed.EndsWith('?'))
        {
            processed += ".";
        }
        
        // Remove any artifacts from the AI model
        processed = processed.Replace("Summary:", "").Trim();
        
        return processed;
    }

    /// <summary>
    /// Creates an enhanced summary for URL content that includes metadata
    /// Provides context about the source and extraction process for better user experience
    /// </summary>
    private static string CreateEnhancedUrlSummary(
        string aiSummary, 
        UrlExtractionResult extractionResult, 
        TimeSpan processingTime)
    {
        var enhancedSummary = new StringBuilder();
        
        // Add source information for transparency
        if (!string.IsNullOrWhiteSpace(extractionResult.Title))
        {
            enhancedSummary.AppendLine($"📰 {extractionResult.Title}");
            enhancedSummary.AppendLine();
        }
        
        if (!string.IsNullOrWhiteSpace(extractionResult.Author))
        {
            enhancedSummary.AppendLine($"✍️ By {extractionResult.Author}");
            enhancedSummary.AppendLine();
        }
        
        // Add the AI-generated summary (the main content)
        enhancedSummary.AppendLine("🤖 AI Summary:");
        enhancedSummary.AppendLine(PostprocessSummary(aiSummary));
        enhancedSummary.AppendLine();
        
        // Add metadata footer for transparency
        enhancedSummary.AppendLine($"📊 Source: {extractionResult.Content.Length:N0} characters analyzed");
        enhancedSummary.AppendLine($"⏱️ Generated: {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC");
        
        if (processingTime > TimeSpan.Zero)
        {
            enhancedSummary.AppendLine($"⚡ Processing time: {processingTime.TotalSeconds:F1}s");
        }
        
        return enhancedSummary.ToString();
    }

    /// <summary>
    /// Converts technical error messages into user-friendly explanations
    /// Provides helpful guidance based on the type of content being processed
    /// </summary>
    private static string GetUserFriendlyErrorMessage(string? technicalError, string contentType)
    {
        if (string.IsNullOrEmpty(technicalError))
        {
            return contentType == "url" 
                ? "Unable to generate summary from the webpage content." 
                : "Unable to generate summary from the provided text.";
        }

        // Convert common technical errors to user-friendly messages
        if (technicalError.Contains("loading", StringComparison.OrdinalIgnoreCase) ||
            technicalError.Contains("warming up", StringComparison.OrdinalIgnoreCase))
        {
            return "The AI summarization service is starting up. Please try again in a moment.";
        }

        if (technicalError.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            technicalError.Contains("too many", StringComparison.OrdinalIgnoreCase))
        {
            return "The service is currently busy. Please wait a moment and try again.";
        }

        if (technicalError.Contains("timeout", StringComparison.OrdinalIgnoreCase))
        {
            return contentType == "url"
                ? "The webpage took too long to process. Please try a different URL or try again later."
                : "The text took too long to process. Please try with shorter content.";
        }

        if (technicalError.Contains("authentication", StringComparison.OrdinalIgnoreCase) ||
            technicalError.Contains("token", StringComparison.OrdinalIgnoreCase))
        {
            return "The summarization service is temporarily unavailable. Please try again later.";
        }

        // Default user-friendly message
        return contentType == "url"
            ? "Unable to generate summary from this webpage. Please try a different URL."
            : "Unable to generate summary from this text. Please try different content.";
    }
}
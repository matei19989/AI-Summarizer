namespace AISummarizerAPI.Application.Services;

using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Core.Models;
using AISummarizerAPI.Application.Interfaces;
using AISummarizerAPI.Application.Models;
using AISummarizerAPI.Utils;
using Microsoft.Extensions.Logging;

/// <summary>
/// Orchestrates the complete summarization workflow
/// This class embodies the "Single Responsibility Principle" - its job is purely coordination
/// It doesn't know HOW to validate, extract, or summarize - it just knows WHEN to call each service
/// This makes our system incredibly flexible and testable
/// </summary>
public class SummarizationOrchestrator : ISummarizationOrchestrator
{
    private readonly IContentValidator _validator;
    private readonly IContentExtractor _extractor;
    private readonly IContentSummarizer _summarizer;
    private readonly ILogger<SummarizationOrchestrator> _logger;

    /// <summary>
    /// Constructor demonstrates Dependency Injection at its finest
    /// We depend on abstractions (interfaces) not concretions
    /// This means we can swap out any of these services without changing this orchestrator
    /// </summary>
    public SummarizationOrchestrator(
        IContentValidator validator,
        IContentExtractor extractor,
        IContentSummarizer summarizer,
        ILogger<SummarizationOrchestrator> logger)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _extractor = extractor ?? throw new ArgumentNullException(nameof(extractor));
        _summarizer = summarizer ?? throw new ArgumentNullException(nameof(summarizer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The main orchestration method - this is our use case implementation
    /// Notice how clean and readable this is compared to the old monolithic service
    /// Each step is clearly defined and the flow is easy to follow
    /// </summary>
    public async Task<SummarizationResult> ProcessAsync(ContentRequest request, CancellationToken cancellationToken = default)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        _logger.LogInformation("Starting summarization process for content type: {ContentType}", request.ContentType);

        try
        {
            // Step 1: Validate the request
            // We fail fast if the input isn't valid - no point in processing bad data
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Content validation failed: {ErrorMessage}", validationResult.ErrorMessage);
                return SummarizationResult.CreateFailure(validationResult.ErrorMessage, request.ContentType.ToString());
            }

            // Step 2: Get the text content to summarize
            // This might involve extraction for URLs or just using the text directly
            var textContent = await GetTextContentAsync(request, cancellationToken);
            if (textContent.IsFailure)
            {
                return textContent.ToSummarizationResult(request.ContentType.ToString());
            }

            // Step 3: Generate the actual summary
            // The summarizer doesn't need to know where the text came from
            var summaryResult = await _summarizer.SummarizeAsync(textContent.Content, cancellationToken);
            
            // Add processing time for monitoring and optimization
            summaryResult.ProcessingTime = stopwatch.Elapsed;
            
            _logger.LogInformation("Summarization completed successfully in {ProcessingTime}ms", 
                stopwatch.ElapsedMilliseconds);

            return summaryResult;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Summarization process was cancelled");
            throw; // Let cancellation bubble up
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during summarization orchestration");
            return SummarizationResult.CreateFailure(
                "An unexpected error occurred while processing your request. Please try again.", 
                request.ContentType.ToString());
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Private helper method that encapsulates the decision of how to get text content
    /// This is where we decide whether to extract from URL or use text directly
    /// Notice how this method has a single, clear responsibility
    /// </summary>
    private async Task<TextContentResult> GetTextContentAsync(ContentRequest request, CancellationToken cancellationToken)
    {
        switch (request.ContentType)
        {
            case ContentType.Text:
                _logger.LogDebug("Using direct text content, length: {Length}", request.Content.Length);
                return TextContentResult.CreateSuccess(request.Content);

            case ContentType.Url:
                // SECURITY: Sanitize URL before logging to prevent log injection attacks
                var sanitizedUrl = LogSanitizer.SanitizeUrl(request.Content);
                _logger.LogInformation("Extracting content from URL: {Url}", sanitizedUrl);
                
                var extractedContent = await _extractor.ExtractAsync(request.Content, cancellationToken);
                
                if (!extractedContent.Success)
                {
                    return TextContentResult.CreateFailure(extractedContent.ErrorMessage ?? "Failed to extract content from URL");
                }
                
                _logger.LogInformation("Successfully extracted {Length} characters from URL", extractedContent.Content.Length);
                return TextContentResult.CreateSuccess(extractedContent.Content);

            default:
                return TextContentResult.CreateFailure($"Unsupported content type: {request.ContentType}");
        }
    }

    /// <summary>
    /// Checks if all dependent services are available
    /// This is crucial for health checks and circuit breaker patterns
    /// </summary>
    public async Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            // We check our most critical dependency - the AI summarizer
            // If it's down, we can't fulfill our core business function
            return await _summarizer.IsAvailableAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return false;
        }
    }
}
namespace AISummarizerAPI.Presentation.Controllers;

using Microsoft.AspNetCore.Mvc;
using AISummarizerAPI.Models.DTOs;
using AISummarizerAPI.Application.Interfaces;
using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Core.Models;

/// <summary>
/// Updated controller that demonstrates the power of clean architecture
/// Notice how much simpler and more focused this controller has become
/// It's now purely concerned with HTTP concerns - validation, routing, and response formatting
/// All the complex orchestration has moved to where it belongs: the application layer
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SummarizationController : ControllerBase
{
    private readonly ISummarizationOrchestrator _orchestrator;
    private readonly IResponseFormatter _responseFormatter;
    private readonly ILogger<SummarizationController> _logger;

    /// <summary>
    /// Constructor shows the clean dependency pattern
    /// We depend on high-level abstractions, not low-level implementation details
    /// This makes the controller incredibly easy to test and reason about
    /// </summary>
    public SummarizationController(
        ISummarizationOrchestrator orchestrator,
        IResponseFormatter responseFormatter,
        ILogger<SummarizationController> logger)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _responseFormatter = responseFormatter ?? throw new ArgumentNullException(nameof(responseFormatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// The main summarization endpoint - now beautifully simple and focused
    /// Compare this to the original version - it's doing exactly the same thing
    /// but with much clearer separation of concerns and better error handling
    /// 
    /// This method demonstrates the "Tell, Don't Ask" principle perfectly
    /// We tell the orchestrator what we want, and it handles all the complexity
    /// </summary>
    [HttpPost("summarize")]
    public async Task<ActionResult<SummarizationResponse>> SummarizeContent(
        [FromBody] SummarizationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received summarization request for content type: {ContentType}", 
            request?.ContentType);

        try
        {
            // Basic request validation - this is an HTTP concern, so it stays in the controller
            if (request == null)
            {
                _logger.LogWarning("Request body is null");
                return BadRequest(_responseFormatter.FormatSystemError<SummarizationResponse>(
                    "Request body is required"));
            }

            // Model state validation - also an HTTP/presentation concern
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                var firstError = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .FirstOrDefault()?.ErrorMessage ?? "Invalid request format";
                
                return BadRequest(_responseFormatter.FormatSystemError<SummarizationResponse>(firstError));
            }

            // Convert from DTO to domain model - this is the boundary between HTTP and business logic
            var contentRequest = request.ContentType.ToLowerInvariant() switch
            {
                "text" => ContentRequest.FromText(request.Content),
                "url" => ContentRequest.FromUrl(request.Content),
                _ => throw new ArgumentException($"Unsupported content type: {request.ContentType}")
            };

            // Delegate to the orchestrator - this is where the magic happens
            // Notice how we don't need to know anything about validation, extraction, or AI processing
            // The orchestrator handles all of that complexity
            var result = await _orchestrator.ProcessAsync(contentRequest, cancellationToken);

            // Format and return the response
            if (result.Success)
            {
                _logger.LogInformation("Summarization completed successfully");
                return Ok(_responseFormatter.FormatResponse<SummarizationResponse>(result));
            }
            else
            {
                _logger.LogWarning("Summarization failed: {ErrorMessage}", result.ErrorMessage);
                return BadRequest(_responseFormatter.FormatResponse<SummarizationResponse>(result));
            }
        }
        catch (ArgumentException argEx)
        {
            _logger.LogWarning(argEx, "Invalid argument in summarization request");
            return BadRequest(_responseFormatter.FormatSystemError<SummarizationResponse>(argEx.Message));
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Summarization request was cancelled");
            return StatusCode(499, _responseFormatter.FormatSystemError<SummarizationResponse>(
                "Request was cancelled"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during summarization request");
            return StatusCode(500, _responseFormatter.FormatSystemError<SummarizationResponse>(
                "An unexpected error occurred. Please try again later."));
        }
    }

    /// <summary>
    /// Health check endpoint - now uses the orchestrator's health check capability
    /// This gives us a more comprehensive view of system health
    /// instead of just checking if the controller is running
    /// </summary>
    [HttpGet("health")]
    public async Task<ActionResult> GetHealth(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Health check requested");
        
        try
        {
            var isHealthy = await _orchestrator.IsHealthyAsync(cancellationToken);
            
            var healthResponse = new
            {
                Service = "AI Content Summarizer",
                Status = isHealthy ? "Healthy" : "Degraded",
                Timestamp = DateTime.UtcNow,
                Version = "2.0.0",
                Dependencies = new
                {
                    AIService = isHealthy ? "Available" : "Unavailable",
                    ContentExtraction = "Available", // Could add specific checks here
                    Validation = "Available"
                }
            };

            return isHealthy ? Ok(healthResponse) : StatusCode(503, healthResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            
            var errorResponse = new
            {
                Service = "AI Content Summarizer",
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Error = "Health check failed"
            };
            
            return StatusCode(503, errorResponse);
        }
    }

    /// <summary>
    /// API information endpoint - provides documentation about capabilities
    /// This is a presentation concern, so it stays in the controller
    /// </summary>
    [HttpGet("info")]
    public ActionResult GetApiInfo()
    {
        _logger.LogDebug("API information requested");

        var apiInfo = new
        {
            Name = "AI Content Summarizer API",
            Version = "2.0.0",
            Description = "Generates AI-powered summaries from text content or web URLs",
            
            SupportedContentTypes = new[]
            {
                new
                {
                    Type = "text",
                    Description = "Plain text content to be summarized",
                    MinLength = 50,
                    MaxLength = 10000,
                    Example = "Your long article or document text here..."
                },
                new
                {
                    Type = "url", 
                    Description = "URL to extract content from and summarize",
                    Format = "Must start with http:// or https://",
                    Example = "https://example.com/article"
                }
            },
            
            Features = new[]
            {
                "AI-powered summarization using state-of-the-art language models",
                "Intelligent content extraction from web URLs",
                "Comprehensive input validation and error handling",
                "Real-time processing with cancellation support",
                "Extensible architecture supporting multiple AI providers"
            },
            
            Architecture = new
            {
                Pattern = "Clean Architecture with Domain-Driven Design",
                AIProvider = "Hugging Face Transformers",
                ContentExtraction = "Mozilla Readability Algorithm",
                ResponseFormat = "JSON with comprehensive error handling"
            }
        };

        return Ok(apiInfo);
    }
}
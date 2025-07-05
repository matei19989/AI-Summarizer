using Microsoft.AspNetCore.Mvc;
using AISummarizerAPI.Models.DTOs;
using AISummarizerAPI.Services.Interfaces;

namespace AISummarizerAPI.Controllers;

/// <summary>
/// Controller responsible for handling summarization HTTP requests
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class SummarizationController : ControllerBase
{
    private readonly ISummarizationService _summarizationService;
    private readonly ILogger<SummarizationController> _logger;

    public SummarizationController(
        ISummarizationService summarizationService,
        ILogger<SummarizationController> logger)
    {
        _summarizationService = summarizationService ?? throw new ArgumentNullException(nameof(summarizationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    [HttpPost("summarize")]
    public async Task<ActionResult<SummarizationResponse>> SummarizeContent(
        [FromBody] SummarizationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received summarization request for content type: {ContentType}", request?.ContentType ?? "null");

        try
        {
            // Enhanced request validation with detailed logging
            if (request == null)
            {
                _logger.LogWarning("Request body is null");
                return BadRequest(new { error = "Request body is required" });
            }

            _logger.LogInformation("Request content length: {Length}, type: {Type}", 
                request.Content?.Length ?? 0, request.ContentType);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                
                return BadRequest(new { 
                    error = "Validation failed", 
                    details = errors 
                });
            }

            SummarizationResponse response;

            switch (request.ContentType.ToLowerInvariant())
            {
                case "text":
                    response = await _summarizationService.SummarizeTextAsync(request.Content!, cancellationToken);
                    break;
                case "url":
                    response = await _summarizationService.SummarizeUrlAsync(request.Content!, cancellationToken);
                    break;
                default:
                    _logger.LogWarning("Unsupported content type: {ContentType}", request.ContentType);
                    return BadRequest(new { error = $"Unsupported content type '{request.ContentType}'. Use 'text' or 'url'." });
            }

            if (!response.Success)
            {
                _logger.LogWarning("Summarization failed: {ErrorMessage}", response.ErrorMessage);
                return BadRequest(new { error = response.ErrorMessage });
            }

            _logger.LogInformation("Summarization completed successfully for content type: {ContentType}", request.ContentType);
            return Ok(response);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Summarization request was cancelled");
            return StatusCode(499, new { error = "Request was cancelled" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during summarization request");
            return StatusCode(500, new { error = "An unexpected error occurred. Please try again later." });
        }
    }

    [HttpGet("health")]
    public ActionResult GetHealth()
    {
        _logger.LogDebug("Health check requested for summarization controller");
        
        return Ok(new
        {
            Service = "SummarizationController",
            Status = "Healthy",
            Timestamp = DateTime.UtcNow,
            Version = "1.0.0"
        });
    }

    [HttpGet("info")]
    public ActionResult GetSupportedContentTypes()
    {
        _logger.LogDebug("API information requested");

        var supportedContentTypes = new object[]
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
        };

        var features = new string[]
        {
            "AI-powered summarization using Hugging Face models",
            "Text-to-speech conversion for generated summaries", 
            "Support for both direct text and URL content extraction"
        };

        return Ok(new
        {
            SupportedContentTypes = supportedContentTypes,
            Features = features
        });
    }
}
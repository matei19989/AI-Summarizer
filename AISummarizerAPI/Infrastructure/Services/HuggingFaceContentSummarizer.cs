namespace AISummarizerAPI.Infrastructure.Services;

using AISummarizerAPI.Core.Interfaces;
using AISummarizerAPI.Core.Models;
using AISummarizerAPI.Services.Interfaces;
using Microsoft.Extensions.Logging;

public class HuggingFaceContentSummarizer : IContentSummarizer
{
    private readonly IHuggingFaceApiClient _huggingFaceClient;
    private readonly ILogger<HuggingFaceContentSummarizer> _logger;

    public HuggingFaceContentSummarizer(
        IHuggingFaceApiClient huggingFaceClient,
        ILogger<HuggingFaceContentSummarizer> logger)
    {
        _huggingFaceClient = huggingFaceClient ?? throw new ArgumentNullException(nameof(huggingFaceClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<SummarizationResult> SummarizeAsync(string content, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating AI summary for content length: {Length}", content.Length);

            var processedContent = PreprocessContentForAI(content);
            var huggingFaceResponse = await _huggingFaceClient.SummarizeTextAsync(processedContent, cancellationToken);

            if (!huggingFaceResponse.Success)
            {
                _logger.LogWarning("HuggingFace API call failed: {ErrorMessage}", huggingFaceResponse.ErrorMessage);
                return SummarizationResult.CreateFailure(
                    ConvertToUserFriendlyMessage(huggingFaceResponse.ErrorMessage),
                    "AI Service");
            }

            var finalSummary = PostprocessAISummary(huggingFaceResponse.SummaryText);
            _logger.LogInformation("Successfully generated AI summary of length: {Length}", finalSummary.Length);

            return SummarizationResult.CreateSuccess(finalSummary, "AI Service", huggingFaceResponse.ProcessingTime);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("AI summarization was cancelled");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during AI summarization");
            return SummarizationResult.CreateFailure(
                "The AI summarization service encountered an error. Please try again.",
                "AI Service");
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _huggingFaceClient.TestConnectionAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for HuggingFace AI service");
            return false;
        }
    }

    private static string PreprocessContentForAI(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return string.Empty;

        var processed = System.Text.RegularExpressions.Regex.Replace(content.Trim(), @"\s+", " ");
        processed = System.Text.RegularExpressions.Regex.Replace(processed, 
            @"\b(Click here|Read more|Subscribe|Advertisement)\b", "", 
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        
        if (processed.Length > 4000)
        {
            processed = processed.Substring(0, 4000);
            var lastPeriod = processed.LastIndexOf('.');
            if (lastPeriod > 3000)
            {
                processed = processed.Substring(0, lastPeriod + 1);
            }
        }

        return processed;
    }

    private static string PostprocessAISummary(string summary)
    {
        if (string.IsNullOrWhiteSpace(summary))
            return "Unable to generate summary.";

        var processed = summary.Trim();
        
        if (processed.Length > 0 && char.IsLower(processed[0]))
        {
            processed = char.ToUpper(processed[0]) + processed.Substring(1);
        }
        
        if (!processed.EndsWith('.') && !processed.EndsWith('!') && !processed.EndsWith('?'))
        {
            processed += ".";
        }
        
        processed = processed.Replace("Summary:", "").Trim();
        
        return processed;
    }

    private static string ConvertToUserFriendlyMessage(string? technicalError)
    {
        if (string.IsNullOrEmpty(technicalError))
            return "The AI service encountered an error while generating your summary.";

        if (technicalError.Contains("loading", StringComparison.OrdinalIgnoreCase))
            return "The AI service is starting up. Please try again in a moment.";
        
        if (technicalError.Contains("rate limit", StringComparison.OrdinalIgnoreCase))
            return "The service is currently busy. Please wait a moment and try again.";
        
        if (technicalError.Contains("timeout", StringComparison.OrdinalIgnoreCase))
            return "The request took too long to process. Please try with shorter content.";
        
        if (technicalError.Contains("authentication", StringComparison.OrdinalIgnoreCase))
            return "The AI service is temporarily unavailable. Please try again later.";

        return "The AI service encountered an error. Please try again.";
    }
}
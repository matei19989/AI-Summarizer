namespace AISummarizerAPI.Application.Interfaces;

using AISummarizerAPI.Core.Models;

/// <summary>
/// Interface for our main use case orchestrator
/// This represents our primary business capability from the application perspective
/// </summary>
public interface ISummarizationOrchestrator
{
    Task<SummarizationResult> ProcessAsync(ContentRequest request, CancellationToken cancellationToken = default);
    Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);
}
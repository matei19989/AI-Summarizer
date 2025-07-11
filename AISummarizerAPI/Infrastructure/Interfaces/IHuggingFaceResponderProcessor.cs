using AISummarizerAPI.Models.HuggingFace;

namespace AISummarizerAPI.Infrastructure.Interfaces;

public interface IHuggingFaceResponseProcessor
{
    Task<HuggingFaceSummarizationResponse> ProcessSummarizationResponseAsync(HttpResponseMessage response);
    Task<HuggingFaceApiStatus> ProcessApiStatusResponseAsync(HttpResponseMessage response);
}